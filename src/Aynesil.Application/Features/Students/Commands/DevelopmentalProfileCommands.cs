using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── UpsertDevelopmentalProfileCommand ────────────────────────────────────────

/// <summary>
/// Creates a new developmental profile or updates an existing one for the given
/// (student, development_area) pair.
/// </summary>
public record UpsertDevelopmentalProfileCommand(
    Guid StudentId,
    Guid? DevelopmentAreaId,
    string? Summary,
    string? Strengths,
    string? Needs,
    DateOnly? AssessedOn) : IRequest<DevelopmentalProfileDto>;

public class UpsertDevelopmentalProfileCommandValidator
    : AbstractValidator<UpsertDevelopmentalProfileCommand>
{
    public UpsertDevelopmentalProfileCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
    }
}

public sealed class UpsertDevelopmentalProfileCommandHandler
    : IRequestHandler<UpsertDevelopmentalProfileCommand, DevelopmentalProfileDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpsertDevelopmentalProfileCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DevelopmentalProfileDto> Handle(
        UpsertDevelopmentalProfileCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var profile = req.DevelopmentAreaId.HasValue
            ? await _db.DevelopmentalProfiles
                .FirstOrDefaultAsync(p => p.StudentId == req.StudentId
                    && p.DevelopmentAreaId == req.DevelopmentAreaId, ct)
            : null;

        if (profile is null)
        {
            profile = new DevelopmentalProfile
            {
                CorporationId     = student.CorporationId,
                StudentId         = req.StudentId,
                DevelopmentAreaId = req.DevelopmentAreaId,
                Summary           = req.Summary,
                Strengths         = req.Strengths,
                Needs             = req.Needs,
                AssessedOn        = req.AssessedOn,
                CreatedAt         = DateTimeOffset.UtcNow,
                UpdatedAt         = DateTimeOffset.UtcNow,
                CreatedBy         = _currentUser.UserId
            };
            _db.DevelopmentalProfiles.Add(profile);
        }
        else
        {
            profile.Summary    = req.Summary;
            profile.Strengths  = req.Strengths;
            profile.Needs      = req.Needs;
            profile.AssessedOn = req.AssessedOn;
            profile.UpdatedAt  = DateTimeOffset.UtcNow;
            profile.UpdatedBy  = _currentUser.UserId;
        }

        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToDevelopmentalProfileDto(profile);
    }
}
