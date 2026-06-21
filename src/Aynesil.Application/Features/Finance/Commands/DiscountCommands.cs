using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Commands;

// ── ApplyDiscountCommand ──────────────────────────────────────────────────────

public record ApplyDiscountCommand(
    Guid CorporationId,
    decimal Value,
    bool IsPercentage = true,
    Guid? InvoiceId = null,
    Guid? StudentPackageId = null,
    Guid? DiscountTypeId = null,
    string? Reason = null) : IRequest<DiscountDto>;

public class ApplyDiscountCommandValidator : AbstractValidator<ApplyDiscountCommand>
{
    public ApplyDiscountCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.IsPercentage)
            .WithMessage("Percentage discount cannot exceed 100%.");

        RuleFor(x => x)
            .Must(x => x.InvoiceId.HasValue || x.StudentPackageId.HasValue)
            .WithMessage("Discount must target either an invoice or a student package.");

        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason is not null);
    }
}

public sealed class ApplyDiscountCommandHandler
    : IRequestHandler<ApplyDiscountCommand, DiscountDto>
{
    private readonly IAppDbContext _db;

    public ApplyDiscountCommandHandler(IAppDbContext db) => _db = db;

    public async Task<DiscountDto> Handle(ApplyDiscountCommand req, CancellationToken ct)
    {
        if (req.InvoiceId.HasValue)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Lines)
                .Include(i => i.Discounts)
                .FirstOrDefaultAsync(i => i.Id == req.InvoiceId.Value, ct)
                ?? throw new KeyNotFoundException($"Invoice {req.InvoiceId} not found.");

            if (invoice.Status != "draft")
                throw new InvalidOperationException(
                    "Discounts can only be applied to draft invoices.");

            var discount = Discount.Apply(
                req.CorporationId, req.Value, req.IsPercentage,
                req.InvoiceId, req.StudentPackageId, req.DiscountTypeId, req.Reason);

            _db.Discounts.Add(discount);
            invoice.Discounts.Add(discount);
            invoice.RecalculateTotals();

            await _db.SaveChangesAsync(ct);

            return FinanceProjection.ToDiscountDto(discount);
        }
        else
        {
            var packageExists = await _db.StudentPackages
                .AnyAsync(p => p.Id == req.StudentPackageId!.Value, ct);

            if (!packageExists)
                throw new KeyNotFoundException($"Student package {req.StudentPackageId} not found.");

            var discount = Discount.Apply(
                req.CorporationId, req.Value, req.IsPercentage,
                req.InvoiceId, req.StudentPackageId, req.DiscountTypeId, req.Reason);

            _db.Discounts.Add(discount);
            await _db.SaveChangesAsync(ct);

            return FinanceProjection.ToDiscountDto(discount);
        }
    }
}
