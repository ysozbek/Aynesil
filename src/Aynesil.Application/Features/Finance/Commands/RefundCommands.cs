using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Commands;

// ── RequestRefundCommand ──────────────────────────────────────────────────────

public record RequestRefundCommand(
    Guid PaymentId,
    decimal Amount,
    string? Reason) : IRequest<RefundDto>;

public class RequestRefundCommandValidator : AbstractValidator<RequestRefundCommand>
{
    public RequestRefundCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason is not null);
    }
}

public sealed class RequestRefundCommandHandler
    : IRequestHandler<RequestRefundCommand, RefundDto>
{
    private readonly IAppDbContext _db;

    public RequestRefundCommandHandler(IAppDbContext db) => _db = db;

    public async Task<RefundDto> Handle(RequestRefundCommand req, CancellationToken ct)
    {
        var payment = await _db.Payments
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == req.PaymentId, ct)
            ?? throw new KeyNotFoundException($"Payment {req.PaymentId} not found.");

        if (payment.Status != "captured")
            throw new InvalidOperationException(
                "Refunds can only be requested against captured payments.");

        var alreadyRefunded = payment.Refunds
            .Where(r => r.Status == "processed")
            .Sum(r => r.Amount);

        if (alreadyRefunded + req.Amount > payment.Amount)
            throw new InvalidOperationException(
                $"Refund amount ({req.Amount:F2}) would exceed the remaining refundable amount " +
                $"({payment.Amount - alreadyRefunded:F2}).");

        var refund = Refund.Request(payment.CorporationId, req.PaymentId, req.Amount, req.Reason);

        _db.Refunds.Add(refund);
        await _db.SaveChangesAsync(ct);

        return new RefundDto(
            refund.Id, refund.CorporationId, refund.PaymentId,
            refund.Amount, refund.Reason, refund.Status,
            refund.ProcessedAt, refund.CreatedAt);
    }
}

// ── ProcessRefundCommand ──────────────────────────────────────────────────────

public record ProcessRefundCommand(Guid Id) : IRequest<RefundDto>;

public sealed class ProcessRefundCommandHandler
    : IRequestHandler<ProcessRefundCommand, RefundDto>
{
    private readonly IAppDbContext _db;

    public ProcessRefundCommandHandler(IAppDbContext db) => _db = db;

    public async Task<RefundDto> Handle(ProcessRefundCommand req, CancellationToken ct)
    {
        var refund = await _db.Refunds
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Refund {req.Id} not found.");

        refund.Process();

        // If the full payment amount has been refunded, update payment status
        var payment = await _db.Payments
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == refund.PaymentId, ct);

        if (payment is not null)
        {
            var totalProcessed = payment.Refunds
                .Where(r => r.Status == "processed" || r.Id == refund.Id)
                .Sum(r => r.Amount);

            if (totalProcessed >= payment.Amount)
                payment.MarkRefunded();
        }

        await _db.SaveChangesAsync(ct);

        return new RefundDto(
            refund.Id, refund.CorporationId, refund.PaymentId,
            refund.Amount, refund.Reason, refund.Status,
            refund.ProcessedAt, refund.CreatedAt);
    }
}

// ── FailRefundCommand ─────────────────────────────────────────────────────────

public record FailRefundCommand(Guid Id) : IRequest<RefundDto>;

public sealed class FailRefundCommandHandler : IRequestHandler<FailRefundCommand, RefundDto>
{
    private readonly IAppDbContext _db;

    public FailRefundCommandHandler(IAppDbContext db) => _db = db;

    public async Task<RefundDto> Handle(FailRefundCommand req, CancellationToken ct)
    {
        var refund = await _db.Refunds
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Refund {req.Id} not found.");

        refund.Fail();
        await _db.SaveChangesAsync(ct);

        return new RefundDto(
            refund.Id, refund.CorporationId, refund.PaymentId,
            refund.Amount, refund.Reason, refund.Status,
            refund.ProcessedAt, refund.CreatedAt);
    }
}
