using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Commands;

// ── ScheduleSchoolVisitCommand ────────────────────────────────────────────────

public record ScheduleSchoolVisitCommand(
    Guid CorporationId,
    Guid InstitutionId,
    DateOnly VisitDate,
    Guid? ConsultancyPlanId,
    Guid? VisitorId,
    string? Purpose) : IRequest<SchoolVisitDto>;

public class ScheduleSchoolVisitCommandValidator
    : AbstractValidator<ScheduleSchoolVisitCommand>
{
    public ScheduleSchoolVisitCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.InstitutionId).NotEmpty();
        RuleFor(x => x.VisitDate).NotEmpty();
        RuleFor(x => x.Purpose).MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Purpose));
    }
}

public sealed class ScheduleSchoolVisitCommandHandler
    : IRequestHandler<ScheduleSchoolVisitCommand, SchoolVisitDto>
{
    private readonly IAppDbContext _db;

    public ScheduleSchoolVisitCommandHandler(IAppDbContext db) => _db = db;

    public async Task<SchoolVisitDto> Handle(
        ScheduleSchoolVisitCommand req, CancellationToken ct)
    {
        var institutionExists = await _db.Institutions
            .AnyAsync(i => i.Id == req.InstitutionId && i.DeletedAt == null, ct);
        if (!institutionExists)
            throw new KeyNotFoundException($"Institution {req.InstitutionId} not found.");

        if (req.ConsultancyPlanId.HasValue)
        {
            var planExists = await _db.ConsultancyPlans
                .AnyAsync(p => p.Id == req.ConsultancyPlanId.Value
                            && p.InstitutionId == req.InstitutionId
                            && p.Status == "active", ct);
            if (!planExists)
                throw new InvalidOperationException(
                    $"Consultancy plan {req.ConsultancyPlanId} not found or is not active for this institution.");
        }

        var visit = SchoolVisit.Schedule(
            req.CorporationId, req.InstitutionId, req.VisitDate,
            req.ConsultancyPlanId, req.VisitorId, req.Purpose);

        _db.SchoolVisits.Add(visit);
        await _db.SaveChangesAsync(ct);

        var institutionName = await _db.Institutions.AsNoTracking()
            .Where(i => i.Id == req.InstitutionId)
            .Select(i => i.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var planName = req.ConsultancyPlanId.HasValue
            ? await _db.ConsultancyPlans.AsNoTracking()
                .Where(p => p.Id == req.ConsultancyPlanId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        return new SchoolVisitDto(
            visit.Id, visit.CorporationId,
            visit.ConsultancyPlanId, planName,
            visit.InstitutionId, institutionName,
            visit.VisitDate, visit.VisitorId, visit.Purpose, visit.Status,
            visit.CreatedAt, []);
    }
}

// ── UpdateSchoolVisitCommand ──────────────────────────────────────────────────

public record UpdateSchoolVisitCommand(
    Guid Id,
    DateOnly VisitDate,
    Guid? VisitorId,
    string? Purpose,
    Guid? ConsultancyPlanId) : IRequest;

public class UpdateSchoolVisitCommandValidator : AbstractValidator<UpdateSchoolVisitCommand>
{
    public UpdateSchoolVisitCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VisitDate).NotEmpty();
        RuleFor(x => x.Purpose).MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Purpose));
    }
}

public sealed class UpdateSchoolVisitCommandHandler : IRequestHandler<UpdateSchoolVisitCommand>
{
    private readonly IAppDbContext _db;

    public UpdateSchoolVisitCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateSchoolVisitCommand req, CancellationToken ct)
    {
        var visit = await _db.SchoolVisits
            .FirstOrDefaultAsync(v => v.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"School visit {req.Id} not found.");

        visit.Update(req.VisitDate, req.VisitorId, req.Purpose, req.ConsultancyPlanId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CompleteSchoolVisitCommand ────────────────────────────────────────────────

public record CompleteSchoolVisitCommand(Guid Id) : IRequest;

public sealed class CompleteSchoolVisitCommandHandler
    : IRequestHandler<CompleteSchoolVisitCommand>
{
    private readonly IAppDbContext _db;

    public CompleteSchoolVisitCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CompleteSchoolVisitCommand req, CancellationToken ct)
    {
        var visit = await _db.SchoolVisits
            .FirstOrDefaultAsync(v => v.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"School visit {req.Id} not found.");

        visit.Complete();
        await _db.SaveChangesAsync(ct);
    }
}

// ── CancelSchoolVisitCommand ──────────────────────────────────────────────────

public record CancelSchoolVisitCommand(Guid Id) : IRequest;

public sealed class CancelSchoolVisitCommandHandler : IRequestHandler<CancelSchoolVisitCommand>
{
    private readonly IAppDbContext _db;

    public CancelSchoolVisitCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CancelSchoolVisitCommand req, CancellationToken ct)
    {
        var visit = await _db.SchoolVisits
            .FirstOrDefaultAsync(v => v.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"School visit {req.Id} not found.");

        visit.Cancel();
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteSchoolVisitCommand ──────────────────────────────────────────────────

public record DeleteSchoolVisitCommand(Guid Id) : IRequest;

public sealed class DeleteSchoolVisitCommandHandler : IRequestHandler<DeleteSchoolVisitCommand>
{
    private readonly IAppDbContext _db;

    public DeleteSchoolVisitCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteSchoolVisitCommand req, CancellationToken ct)
    {
        var visit = await _db.SchoolVisits
            .FirstOrDefaultAsync(v => v.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"School visit {req.Id} not found.");

        if (visit.Status != "planned")
            throw new InvalidOperationException(
                "Only planned visits can be deleted. Use Cancel to archive the visit.");

        var hasObservations = await _db.ObservationRecords
            .AnyAsync(o => o.SchoolVisitId == req.Id, ct);

        if (hasObservations)
            throw new InvalidOperationException(
                "Cannot delete a visit that has recorded observations.");

        _db.SchoolVisits.Remove(visit);
        await _db.SaveChangesAsync(ct);
    }
}
