using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Domain.Modules.Assessment.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Commands;

// ── Request ───────────────────────────────────────────────────────────────────

public record CreateAssessmentSessionCommand(
    Guid TemplateId,
    Guid? LeadId,
    Guid? StudentId,
    Guid? CampusId,
    Guid? AssessorId,
    DateTimeOffset? ScheduledAt) : IRequest<AssessmentSessionDto>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class CreateAssessmentSessionCommandValidator
    : AbstractValidator<CreateAssessmentSessionCommand>
{
    public CreateAssessmentSessionCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.LeadId.HasValue || x.StudentId.HasValue)
            .WithMessage("Either LeadId or StudentId must be provided.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CreateAssessmentSessionCommandHandler
    : IRequestHandler<CreateAssessmentSessionCommand, AssessmentSessionDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public CreateAssessmentSessionCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _tenant = tenant;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentSessionDto> Handle(
        CreateAssessmentSessionCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var template = await _db.AssessmentTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == req.TemplateId && t.IsActive
                                   && (t.CorporationId == null || t.CorporationId == corporationId), ct)
            ?? throw new KeyNotFoundException(
                $"Active assessment template {req.TemplateId} not found.");

        var session = AssessmentSession.Create(
            corporationId, req.TemplateId, template.Version,
            req.LeadId, req.StudentId,
            req.CampusId, req.AssessorId, req.ScheduledAt,
            _currentUser.UserId);

        _db.AssessmentSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return (await AssessmentProjection.LoadSessionAsync(_db, session.Id, ct))!;
    }
}
