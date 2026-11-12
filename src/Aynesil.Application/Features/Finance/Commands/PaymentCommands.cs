using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Commands;

// ── RecordPaymentCommand ──────────────────────────────────────────────────────

public record RecordPaymentCommand(
    decimal Amount,
    string Currency = "TRY",
    Guid? InvoiceId = null,
    Guid? StudentId = null,
    Guid? PaymentMethodId = null,
    Guid? GatewayProviderId = null,
    string? GatewayReference = null,
    string? IdempotencyKey = null,
    DateTimeOffset? PaidAt = null) : IRequest<PaymentDto>;

public class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        // Saat farkı/gecikme için 5 dk tolerans; geleceğe tarihli ödeme kaydedilemez.
        RuleFor(x => x.PaidAt)
            .Must(d => d!.Value <= DateTimeOffset.UtcNow.AddMinutes(5))
            .When(x => x.PaidAt.HasValue)
            .WithMessage("Ödeme tarihi gelecekte olamaz.");
    }
}

public sealed class RecordPaymentCommandHandler
    : IRequestHandler<RecordPaymentCommand, PaymentDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public RecordPaymentCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PaymentDto> Handle(RecordPaymentCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        if (req.InvoiceId.HasValue)
        {
            var invoiceExists = await _db.Invoices
                .AnyAsync(i => i.CorporationId == corporationId && i.Id == req.InvoiceId.Value, ct);

            if (!invoiceExists)
                throw new KeyNotFoundException($"Invoice {req.InvoiceId} not found.");
        }

        var payment = Payment.Record(
            corporationId, req.Amount, req.Currency,
            req.InvoiceId, req.StudentId, req.PaymentMethodId,
            req.GatewayProviderId, req.GatewayReference, req.IdempotencyKey, req.PaidAt);

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        return FinanceProjection.ToPaymentDto(payment);
    }
}

// ── CapturePaymentCommand ─────────────────────────────────────────────────────

public record CapturePaymentCommand(
    Guid Id,
    string? GatewayReference,
    DateTimeOffset? PaidAt,
    int RowVersion) : IRequest<PaymentDto>;

public sealed class CapturePaymentCommandHandler
    : IRequestHandler<CapturePaymentCommand, PaymentDto>
{
    private readonly IAppDbContext _db;

    public CapturePaymentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<PaymentDto> Handle(CapturePaymentCommand req, CancellationToken ct)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Payment {req.Id} not found.");

        if (payment.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "Payment was modified by another user. Please refresh and retry.");

        payment.Capture(req.GatewayReference, req.PaidAt);

        // Update invoice status if payment covers full or partial amount
        if (payment.InvoiceId.HasValue)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == payment.InvoiceId.Value, ct);

            if (invoice is not null && invoice.Status is "issued" or "partial" or "overdue")
            {
                var totalCaptured = invoice.Payments
                    .Where(p => p.Status == "captured" || p.Id == payment.Id)
                    .Sum(p => p.Amount);

                if (totalCaptured >= invoice.GrandTotal)
                    invoice.MarkPaid();
                else
                    invoice.MarkPartiallyPaid();
            }
        }

        await _db.SaveChangesAsync(ct);

        return FinanceProjection.ToPaymentDto(payment);
    }
}

// ── FailPaymentCommand ────────────────────────────────────────────────────────

public record FailPaymentCommand(Guid Id, int RowVersion) : IRequest<PaymentDto>;

public sealed class FailPaymentCommandHandler
    : IRequestHandler<FailPaymentCommand, PaymentDto>
{
    private readonly IAppDbContext _db;

    public FailPaymentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<PaymentDto> Handle(FailPaymentCommand req, CancellationToken ct)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Payment {req.Id} not found.");

        if (payment.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "Payment was modified by another user. Please refresh and retry.");

        payment.Fail();
        await _db.SaveChangesAsync(ct);

        return FinanceProjection.ToPaymentDto(payment);
    }
}
