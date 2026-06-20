using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Campuses.Dtos;
using Aynesil.Domain.Modules.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Campuses.Commands;

// ── Request ──────────────────────────────────────────────────────────────────
public record CreateCampusCommand(
    Guid CorporationId,
    string Code,
    string Name,
    string? City,
    string? AddressLine,
    string? District,
    string? Phone,
    string? Email,
    string? Timezone,
    decimal? GeoLat,
    decimal? GeoLng) : IRequest<CampusDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class CreateCampusCommandValidator : AbstractValidator<CreateCampusCommand>
{
    public CreateCampusCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[A-Z0-9][A-Z0-9_-]*$")
            .WithMessage("Code must be uppercase alphanumeric (hyphens/underscores allowed).")
            .OverridePropertyName("code");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);
        RuleFor(x => x.AddressLine).MaximumLength(500).When(x => x.AddressLine is not null);
        RuleFor(x => x.District).MaximumLength(100).When(x => x.District is not null);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
        RuleFor(x => x.Email).MaximumLength(254).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.Timezone).MaximumLength(100).When(x => x.Timezone is not null);
        RuleFor(x => x.GeoLat).InclusiveBetween(-90m, 90m).When(x => x.GeoLat.HasValue);
        RuleFor(x => x.GeoLng).InclusiveBetween(-180m, 180m).When(x => x.GeoLng.HasValue);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class CreateCampusCommandHandler : IRequestHandler<CreateCampusCommand, CampusDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCampusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CampusDto> Handle(CreateCampusCommand req, CancellationToken ct)
    {
        var corp = await _db.Corporations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.CorporationId, ct)
            ?? throw new NotFoundException("Corporation", req.CorporationId);

        var codeNormalized = req.Code.ToUpperInvariant();
        var codeExists = await _db.Campuses
            .AnyAsync(c => c.CorporationId == req.CorporationId && c.Code == codeNormalized, ct);
        if (codeExists)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                "Code", $"Campus code '{codeNormalized}' already exists in this corporation.")]);

        var campus = Campus.Create(
            req.CorporationId,
            codeNormalized,
            req.Name,
            req.City,
            req.AddressLine,
            req.District,
            req.Phone,
            req.Email,
            req.Timezone);

        campus.GeoLat = req.GeoLat;
        campus.GeoLng = req.GeoLng;
        campus.CreatedBy = _currentUser.UserId;
        campus.UpdatedBy = _currentUser.UserId;

        _db.Campuses.Add(campus);
        await _db.SaveChangesAsync(ct);

        return campus.ToDto(corp.DisplayName);
    }
}
