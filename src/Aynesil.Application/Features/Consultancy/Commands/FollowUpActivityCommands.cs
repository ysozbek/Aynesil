using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Commands;

// ── CreateFollowUpActivityCommand ─────────────────────────────────────────────

public record CreateFollowUpActivityCommand(
    Guid CorporationId,
    string Title,
    Guid? ConsultancyPlanId,
    Guid? SchoolVisitId,
    Guid? ObservationRecordId,
    string? Description,
    DateOnly? DueDate,
    Guid? AssignedTo,
    Guid? CreatedBy = null) : IRequest<FollowUpActivityDto>;

public class CreateFollowUpActivityCommandValidator
    : AbstractValidator<CreateFollowUpActivityCommand>
{
    public CreateFollowUpActivityCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x)
            .Must(x => x.ConsultancyPlanId.HasValue || x.SchoolVisitId.HasValue)
            .WithMessage("A follow-up activity must be linked to a consultancy plan or a school visit.");
    }
}

public sealed class CreateFollowUpActivityCommandHandler
    : IRequestHandler<CreateFollowUpActivityCommand, FollowUpActivityDto>
{
    private readonly IAppDbContext _db;

    public CreateFollowUpActivityCommandHandler(IAppDbContext db) => _db = db;

    public async Task<FollowUpActivityDto> Handle(
        CreateFollowUpActivityCommand req, CancellationToken ct)
    {
        if (req.ConsultancyPlanId.HasValue)
        {
            var planExists = await _db.ConsultancyPlans
                .AnyAsync(p => p.Id == req.ConsultancyPlanId.Value, ct);
            if (!planExists)
                throw new KeyNotFoundException($"Consultancy plan {req.ConsultancyPlanId} not found.");
        }

        if (req.SchoolVisitId.HasValue)
        {
            var visitExists = await _db.SchoolVisits
                .AnyAsync(v => v.Id == req.SchoolVisitId.Value, ct);
            if (!visitExists)
                throw new KeyNotFoundException($"School visit {req.SchoolVisitId} not found.");
        }

        var activity = FollowUpActivity.Create(
            req.CorporationId, req.Title,
            req.ConsultancyPlanId, req.SchoolVisitId, req.ObservationRecordId,
            req.Description, req.DueDate, req.AssignedTo, req.CreatedBy);

        _db.FollowUpActivities.Add(activity);
        await _db.SaveChangesAsync(ct);

        return await BuildDto(activity.Id, ct);
    }

    internal async Task<FollowUpActivityDto> BuildDto(Guid id, CancellationToken ct)
        => await ProjectFollowUpDto(_db, id, ct)
           ?? throw new KeyNotFoundException($"Follow-up activity {id} not found after save.");

    internal static async Task<FollowUpActivityDto?> ProjectFollowUpDto(
        IAppDbContext db, Guid id, CancellationToken ct)
        => await (
            from a in db.FollowUpActivities.AsNoTracking()
            where a.Id == id
            join p in db.ConsultancyPlans.AsNoTracking()
                on a.ConsultancyPlanId equals p.Id into planGrp
            from p in planGrp.DefaultIfEmpty()
            join v in db.SchoolVisits.AsNoTracking()
                on a.SchoolVisitId equals v.Id into visitGrp
            from v in visitGrp.DefaultIfEmpty()
            select new FollowUpActivityDto(
                a.Id, a.CorporationId,
                a.ConsultancyPlanId, p != null ? p.Name : null,
                a.SchoolVisitId, v != null ? v.VisitDate : (DateOnly?)null,
                a.ObservationRecordId,
                a.Title, a.Description, a.DueDate, a.AssignedTo,
                a.Status, a.CompletedAt, a.CompletedBy, a.Notes,
                a.CreatedAt, a.UpdatedAt, a.RowVersion)
        ).FirstOrDefaultAsync(ct);
}

// ── UpdateFollowUpActivityCommand ─────────────────────────────────────────────

public record UpdateFollowUpActivityCommand(
    Guid Id,
    string Title,
    string? Description,
    DateOnly? DueDate,
    Guid? AssignedTo,
    string? Notes,
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest;

public class UpdateFollowUpActivityCommandValidator
    : AbstractValidator<UpdateFollowUpActivityCommand>
{
    public UpdateFollowUpActivityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class UpdateFollowUpActivityCommandHandler
    : IRequestHandler<UpdateFollowUpActivityCommand>
{
    private readonly IAppDbContext _db;

    public UpdateFollowUpActivityCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateFollowUpActivityCommand req, CancellationToken ct)
    {
        var activity = await _db.FollowUpActivities
            .FirstOrDefaultAsync(a => a.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Follow-up activity {req.Id} not found.");

        activity.Update(req.Title, req.Description, req.DueDate,
            req.AssignedTo, req.Notes, req.UpdatedBy);

        await _db.SaveChangesAsync(ct);
    }
}

// ── StartFollowUpActivityCommand ──────────────────────────────────────────────

public record StartFollowUpActivityCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class StartFollowUpActivityCommandHandler
    : IRequestHandler<StartFollowUpActivityCommand>
{
    private readonly IAppDbContext _db;

    public StartFollowUpActivityCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(StartFollowUpActivityCommand req, CancellationToken ct)
    {
        var activity = await _db.FollowUpActivities
            .FirstOrDefaultAsync(a => a.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Follow-up activity {req.Id} not found.");

        activity.StartProgress(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CompleteFollowUpActivityCommand ───────────────────────────────────────────

public record CompleteFollowUpActivityCommand(
    Guid Id,
    string? Notes,
    Guid? CompletedBy = null) : IRequest;

public sealed class CompleteFollowUpActivityCommandHandler
    : IRequestHandler<CompleteFollowUpActivityCommand>
{
    private readonly IAppDbContext _db;

    public CompleteFollowUpActivityCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CompleteFollowUpActivityCommand req, CancellationToken ct)
    {
        var activity = await _db.FollowUpActivities
            .FirstOrDefaultAsync(a => a.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Follow-up activity {req.Id} not found.");

        activity.Complete(req.Notes, req.CompletedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CancelFollowUpActivityCommand ─────────────────────────────────────────────

public record CancelFollowUpActivityCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class CancelFollowUpActivityCommandHandler
    : IRequestHandler<CancelFollowUpActivityCommand>
{
    private readonly IAppDbContext _db;

    public CancelFollowUpActivityCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CancelFollowUpActivityCommand req, CancellationToken ct)
    {
        var activity = await _db.FollowUpActivities
            .FirstOrDefaultAsync(a => a.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Follow-up activity {req.Id} not found.");

        activity.Cancel(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteFollowUpActivityCommand ─────────────────────────────────────────────

public record DeleteFollowUpActivityCommand(Guid Id) : IRequest;

public sealed class DeleteFollowUpActivityCommandHandler
    : IRequestHandler<DeleteFollowUpActivityCommand>
{
    private readonly IAppDbContext _db;

    public DeleteFollowUpActivityCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteFollowUpActivityCommand req, CancellationToken ct)
    {
        var activity = await _db.FollowUpActivities
            .FirstOrDefaultAsync(a => a.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Follow-up activity {req.Id} not found.");

        if (activity.Status is "completed")
            throw new InvalidOperationException("Cannot delete a completed follow-up activity.");

        _db.FollowUpActivities.Remove(activity);
        await _db.SaveChangesAsync(ct);
    }
}
