using Aynesil.Application.Common.Validation;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Corporations.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Corporations.Commands;

// ── Request ──────────────────────────────────────────────────────────────────
public record UpdateCorporationCommand(
    Guid Id,
    string LegalName,
    string DisplayName,
    string DefaultLocale,
    string DefaultCurrency,
    string Timezone,
    string? TaxOffice,
    string? TaxNumber,
    int RowVersion) : IRequest<CorporationDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class UpdateCorporationCommandValidator : AbstractValidator<UpdateCorporationCommand>
{
    public UpdateCorporationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DefaultLocale).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => x.TaxNumber is not null);
        RuleFor(x => x.TaxOffice).MaximumLength(150).When(x => x.TaxOffice is not null);
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class UpdateCorporationCommandHandler : IRequestHandler<UpdateCorporationCommand, CorporationDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public UpdateCorporationCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task<CorporationDto> Handle(UpdateCorporationCommand req, CancellationToken ct)
    {
        // Only the caller's own corporation; any other id is "not found" (Y1). RLS enforces the same.
        var corporationId = _tenant.RequireCorporationId();

        var corp = await _db.Corporations
            .Include(c => c.Campuses)
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.Id == corporationId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Corporation"));

        // Optimistic concurrency guard
        if (corp.RowVersion != req.RowVersion)
            throw new ValidationException([ValidationFailures.For(nameof(req.RowVersion), MsgKeys.Error.Concurrency)]);

        corp.LegalName = req.LegalName;
        corp.DisplayName = req.DisplayName;
        corp.DefaultLocale = req.DefaultLocale;
        corp.DefaultCurrency = req.DefaultCurrency;
        corp.Timezone = req.Timezone;
        corp.TaxOffice = req.TaxOffice;
        corp.TaxNumber = req.TaxNumber;
        corp.UpdatedAt = DateTimeOffset.UtcNow;
        corp.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);

        // Global query filter on Campus (DeletedAt == null) applies to navigated collections.
        return corp.ToDto(corp.Campuses.Count);
    }
}
