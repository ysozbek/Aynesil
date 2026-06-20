using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Corporations.Dtos;
using Aynesil.Domain.Modules.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Corporations.Commands;

// ── Request ──────────────────────────────────────────────────────────────────
public record CreateCorporationCommand(
    string Code,
    string LegalName,
    string DisplayName,
    string DefaultLocale,
    string DefaultCurrency,
    string Timezone,
    string? TaxOffice,
    string? TaxNumber) : IRequest<CorporationDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class CreateCorporationCommandValidator : AbstractValidator<CreateCorporationCommand>
{
    public CreateCorporationCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[a-z0-9][a-z0-9_-]*$")
            .WithMessage("Code must start with a letter or digit and contain only lowercase letters, digits, hyphens, or underscores.");

        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DefaultLocale).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => x.TaxNumber is not null);
        RuleFor(x => x.TaxOffice).MaximumLength(150).When(x => x.TaxOffice is not null);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class CreateCorporationCommandHandler : IRequestHandler<CreateCorporationCommand, CorporationDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCorporationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CorporationDto> Handle(CreateCorporationCommand req, CancellationToken ct)
    {
        var codeNormalized = req.Code.ToLowerInvariant();

        var codeExists = await _db.Corporations
            .AnyAsync(c => c.Code == codeNormalized, ct);
        if (codeExists)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.Code), $"Corporation code '{codeNormalized}' is already taken.")]);

        var corp = Corporation.Create(codeNormalized, req.LegalName, req.DisplayName, req.DefaultLocale);
        corp.DefaultCurrency = req.DefaultCurrency;
        corp.Timezone = req.Timezone;
        corp.TaxOffice = req.TaxOffice;
        corp.TaxNumber = req.TaxNumber;
        corp.CreatedBy = _currentUser.UserId;
        corp.UpdatedBy = _currentUser.UserId;

        _db.Corporations.Add(corp);
        await _db.SaveChangesAsync(ct);

        return corp.ToDto(campusCount: 0);
    }
}
