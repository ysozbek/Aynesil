using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
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
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[A-Z0-9][A-Z0-9_-]*$")
            .WithKey(MsgKeys.Validation.CodeFormatUpper)
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
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public CreateCampusCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _tenant = tenant;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CampusDto> Handle(CreateCampusCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var corp = await _db.Corporations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == corporationId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Corporation"));

        var codeNormalized = req.Code.Trim().ToUpperInvariant();
        // Unique (corporation_id, code) applies to soft-deleted rows too.
        var codeExists = await _db.Campuses
            .IgnoreQueryFilters()
            .AnyAsync(c => c.CorporationId == corporationId && c.Code == codeNormalized, ct);
        if (codeExists)
            throw new ValidationException([ValidationFailures.For("Code", MsgKeys.Rule.CampusCodeExists, codeNormalized)]);

        var campus = Campus.Create(
            corporationId,
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
