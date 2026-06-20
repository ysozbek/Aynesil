using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leads.Dtos;
using Aynesil.Domain.Modules.Crm.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Commands;

// ── Request ───────────────────────────────────────────────────────────────────
public record ScheduleInterviewCommand(
    Guid LeadId,
    Guid? CampusId,
    DateTimeOffset? ScheduledAt) : IRequest<InterviewDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class ScheduleInterviewCommandValidator : AbstractValidator<ScheduleInterviewCommand>
{
    public ScheduleInterviewCommandValidator()
    {
        RuleFor(x => x.LeadId).NotEmpty();
        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .When(x => x.ScheduledAt.HasValue)
            .WithMessage("Scheduled date must be in the future.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class ScheduleInterviewCommandHandler : IRequestHandler<ScheduleInterviewCommand, InterviewDto>
{
    private readonly IAppDbContext _db;

    public ScheduleInterviewCommandHandler(IAppDbContext db) => _db = db;

    public async Task<InterviewDto> Handle(ScheduleInterviewCommand req, CancellationToken ct)
    {
        var lead = await _db.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == req.LeadId, ct)
            ?? throw new NotFoundException("Lead", req.LeadId);

        var interview = Interview.Schedule(
            lead.CorporationId,
            req.LeadId,
            req.CampusId,
            req.ScheduledAt);

        _db.Interviews.Add(interview);
        await _db.SaveChangesAsync(ct);

        var campusName = interview.CampusId.HasValue
            ? await _db.Campuses
                .Where(c => c.Id == interview.CampusId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        return interview.ToDto(campusName, null);
    }
}
