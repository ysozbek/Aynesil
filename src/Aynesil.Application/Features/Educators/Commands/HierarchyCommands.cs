using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Educators.Dtos;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Educators.Events;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Educators.Commands;

// ── LinkHierarchyCommand ──────────────────────────────────────────────────────

public record LinkHierarchyCommand(
    Guid EducatorId,
    Guid SupervisorId,
    Guid? RelationshipId,
    Guid? CampusId,
    DateOnly? ActiveFrom) : IRequest<EducatorHierarchyDto>;

public class LinkHierarchyCommandValidator : AbstractValidator<LinkHierarchyCommand>
{
    public LinkHierarchyCommandValidator()
    {
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.SupervisorId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.EducatorId != x.SupervisorId)
            .WithMessage("An educator cannot supervise themselves.");
    }
}

public sealed class LinkHierarchyCommandHandler
    : IRequestHandler<LinkHierarchyCommand, EducatorHierarchyDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public LinkHierarchyCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducatorHierarchyDto> Handle(LinkHierarchyCommand req, CancellationToken ct)
    {
        var educator = await _db.Educators.FirstOrDefaultAsync(e => e.Id == req.EducatorId, ct)
            ?? throw new KeyNotFoundException($"Educator {req.EducatorId} not found.");

        var supervisorExists = await _db.Educators.AnyAsync(e => e.Id == req.SupervisorId, ct);
        if (!supervisorExists)
            throw new KeyNotFoundException($"Supervisor educator {req.SupervisorId} not found.");

        if (req.RelationshipId.HasValue)
        {
            var valid = await _db.RefValues.AnyAsync(
                r => r.Id == req.RelationshipId.Value
                  && r.RefType!.Code == "educator_relationship", ct);
            if (!valid)
                throw new KeyNotFoundException(
                    $"Invalid educator_relationship ref_value: {req.RelationshipId}");
        }

        var edge = new EducatorHierarchy
        {
            CorporationId  = educator.CorporationId,
            EducatorId     = req.EducatorId,
            SupervisorId   = req.SupervisorId,
            RelationshipId = req.RelationshipId,
            CampusId       = req.CampusId,
            ActiveFrom     = req.ActiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _db.EducatorHierarchies.Add(edge);

        educator.AddDomainEvent(new EducatorHierarchyLinkedEvent(
            req.EducatorId, req.SupervisorId, educator.CorporationId,
            req.RelationshipId, req.CampusId, _currentUser.UserId));

        await _db.SaveChangesAsync(ct);

        var educatorName = $"{educator.FirstName} {educator.LastName}";
        var supervisor = await _db.Educators.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == req.SupervisorId, ct);
        var supervisorName = supervisor is not null
            ? $"{supervisor.FirstName} {supervisor.LastName}"
            : string.Empty;

        var relLabel = edge.RelationshipId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == edge.RelationshipId.Value)
                .Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var campusName = edge.CampusId.HasValue
            ? await _db.Campuses.AsNoTracking()
                .Where(c => c.Id == edge.CampusId.Value)
                .Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;

        return new EducatorHierarchyDto(
            edge.Id, edge.EducatorId, educatorName,
            edge.SupervisorId, supervisorName,
            edge.RelationshipId, relLabel,
            edge.CampusId, campusName,
            edge.ActiveFrom, edge.ActiveTo, edge.IsActive);
    }
}

// ── UnlinkHierarchyCommand ────────────────────────────────────────────────────

public record UnlinkHierarchyCommand(Guid HierarchyEdgeId) : IRequest;

public sealed class UnlinkHierarchyCommandHandler : IRequestHandler<UnlinkHierarchyCommand>
{
    private readonly IAppDbContext _db;

    public UnlinkHierarchyCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UnlinkHierarchyCommand req, CancellationToken ct)
    {
        var edge = await _db.EducatorHierarchies
            .FirstOrDefaultAsync(h => h.Id == req.HierarchyEdgeId, ct)
            ?? throw new KeyNotFoundException($"Hierarchy edge {req.HierarchyEdgeId} not found.");

        _db.EducatorHierarchies.Remove(edge);
        await _db.SaveChangesAsync(ct);
    }
}

// ── EndHierarchyCommand ───────────────────────────────────────────────────────

public record EndHierarchyCommand(Guid HierarchyEdgeId, DateOnly? EndDate) : IRequest<EducatorHierarchyDto>;

public sealed class EndHierarchyCommandHandler : IRequestHandler<EndHierarchyCommand, EducatorHierarchyDto>
{
    private readonly IAppDbContext _db;

    public EndHierarchyCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EducatorHierarchyDto> Handle(EndHierarchyCommand req, CancellationToken ct)
    {
        var edge = await _db.EducatorHierarchies
            .FirstOrDefaultAsync(h => h.Id == req.HierarchyEdgeId, ct)
            ?? throw new KeyNotFoundException($"Hierarchy edge {req.HierarchyEdgeId} not found.");

        edge.ActiveTo = req.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        await _db.SaveChangesAsync(ct);

        var educatorName = await _db.Educators.AsNoTracking()
            .Where(e => e.Id == edge.EducatorId)
            .Select(e => e.FirstName + " " + e.LastName).FirstOrDefaultAsync(ct) ?? string.Empty;

        var supervisorName = await _db.Educators.AsNoTracking()
            .Where(e => e.Id == edge.SupervisorId)
            .Select(e => e.FirstName + " " + e.LastName).FirstOrDefaultAsync(ct) ?? string.Empty;

        return new EducatorHierarchyDto(
            edge.Id, edge.EducatorId, educatorName,
            edge.SupervisorId, supervisorName,
            edge.RelationshipId, null,
            edge.CampusId, null,
            edge.ActiveFrom, edge.ActiveTo, edge.IsActive);
    }
}
