using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Campuses.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Campuses.Commands;

// ── Request ──────────────────────────────────────────────────────────────────
public record UpdateCampusCommand(
    Guid Id,
    string Name,
    string? City,
    string? AddressLine,
    string? District,
    string? Phone,
    string? Email,
    string? Timezone,
    decimal? GeoLat,
    decimal? GeoLng,
    int RowVersion) : IRequest<CampusDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class UpdateCampusCommandValidator : AbstractValidator<UpdateCampusCommand>
{
    public UpdateCampusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);
        RuleFor(x => x.AddressLine).MaximumLength(500).When(x => x.AddressLine is not null);
        RuleFor(x => x.District).MaximumLength(100).When(x => x.District is not null);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
        RuleFor(x => x.Email).MaximumLength(254).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.Timezone).MaximumLength(100).When(x => x.Timezone is not null);
        RuleFor(x => x.GeoLat).InclusiveBetween(-90m, 90m).When(x => x.GeoLat.HasValue);
        RuleFor(x => x.GeoLng).InclusiveBetween(-180m, 180m).When(x => x.GeoLng.HasValue);
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class UpdateCampusCommandHandler : IRequestHandler<UpdateCampusCommand, CampusDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCampusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CampusDto> Handle(UpdateCampusCommand req, CancellationToken ct)
    {
        var campus = await _db.Campuses
            .Include(c => c.Corporation)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Campus", req.Id);

        if (campus.RowVersion != req.RowVersion)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(req.RowVersion), "The campus was modified by another user. Please refresh and retry.")]);

        campus.Name = req.Name;
        campus.City = req.City;
        campus.AddressLine = req.AddressLine;
        campus.District = req.District;
        campus.Phone = req.Phone;
        campus.Email = req.Email;
        campus.Timezone = req.Timezone;
        campus.GeoLat = req.GeoLat;
        campus.GeoLng = req.GeoLng;
        campus.UpdatedAt = DateTimeOffset.UtcNow;
        campus.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);

        return campus.ToDto(campus.Corporation?.DisplayName ?? string.Empty);
    }
}
