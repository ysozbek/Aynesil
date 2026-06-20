using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Domain.Modules.Assessment.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Commands;

// ── Create recommendation ─────────────────────────────────────────────────────

public record CreateProgramRecommendationCommand(
    Guid CorporationId,
    Guid? AssessmentSessionId,
    Guid? LeadId,
    Guid? StudentId,
    Guid? RecommendedProgramId,
    string? RecommendedIntensity,
    string? Rationale,
    Guid? RecommendedBy) : IRequest<ProgramRecommendationDto>;

public class CreateProgramRecommendationCommandValidator
    : AbstractValidator<CreateProgramRecommendationCommand>
{
    public CreateProgramRecommendationCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.RecommendedIntensity).MaximumLength(200)
            .When(x => x.RecommendedIntensity is not null);
        RuleFor(x => x.Rationale).MaximumLength(2000)
            .When(x => x.Rationale is not null);
    }
}

public sealed class CreateProgramRecommendationCommandHandler
    : IRequestHandler<CreateProgramRecommendationCommand, ProgramRecommendationDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateProgramRecommendationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProgramRecommendationDto> Handle(
        CreateProgramRecommendationCommand req, CancellationToken ct)
    {
        var recommendation = ProgramRecommendation.Create(
            req.CorporationId,
            req.AssessmentSessionId,
            req.LeadId,
            req.StudentId,
            req.RecommendedProgramId,
            req.RecommendedIntensity,
            req.Rationale,
            req.RecommendedBy,
            _currentUser.UserId);

        _db.ProgramRecommendations.Add(recommendation);
        await _db.SaveChangesAsync(ct);

        return AssessmentProjection.ToRecommendationDto(recommendation);
    }
}

// ── Update recommendation ─────────────────────────────────────────────────────

public record UpdateProgramRecommendationCommand(
    Guid Id,
    Guid? RecommendedProgramId,
    string? RecommendedIntensity,
    string? Rationale,
    Guid? RecommendedBy,
    int RowVersion) : IRequest<ProgramRecommendationDto>;

public class UpdateProgramRecommendationCommandValidator
    : AbstractValidator<UpdateProgramRecommendationCommand>
{
    public UpdateProgramRecommendationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
        RuleFor(x => x.RecommendedIntensity).MaximumLength(200)
            .When(x => x.RecommendedIntensity is not null);
        RuleFor(x => x.Rationale).MaximumLength(2000)
            .When(x => x.Rationale is not null);
    }
}

public sealed class UpdateProgramRecommendationCommandHandler
    : IRequestHandler<UpdateProgramRecommendationCommand, ProgramRecommendationDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateProgramRecommendationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProgramRecommendationDto> Handle(
        UpdateProgramRecommendationCommand req, CancellationToken ct)
    {
        var rec = await _db.ProgramRecommendations
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Program recommendation {req.Id} not found.");

        rec.Update(req.RecommendedProgramId, req.RecommendedIntensity,
                   req.Rationale, req.RecommendedBy, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        return AssessmentProjection.ToRecommendationDto(rec);
    }
}
