using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── GrantPortalAccessCommand ──────────────────────────────────────────────────

/// <summary>
/// Creates or reactivates a GuardianPortalAccess record for the given guardian/student pair,
/// and enables portal_access on the StudentGuardian link.
/// </summary>
public record GrantPortalAccessCommand(
    Guid GuardianId,
    Guid StudentId,
    bool CanViewSessions,
    bool CanViewAttendance,
    bool CanViewReports,
    bool CanViewPlan,
    bool CanViewFinance,
    bool CanViewCamera) : IRequest<GuardianPortalAccessDto>;

public class GrantPortalAccessCommandValidator : AbstractValidator<GrantPortalAccessCommand>
{
    public GrantPortalAccessCommandValidator()
    {
        RuleFor(x => x.GuardianId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
    }
}

public sealed class GrantPortalAccessCommandHandler
    : IRequestHandler<GrantPortalAccessCommand, GuardianPortalAccessDto>
{
    private readonly IAppDbContext _db;

    public GrantPortalAccessCommandHandler(IAppDbContext db) => _db = db;

    public async Task<GuardianPortalAccessDto> Handle(GrantPortalAccessCommand req, CancellationToken ct)
    {
        var guardian = await _db.Guardians
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == req.GuardianId, ct)
            ?? throw new KeyNotFoundException($"Guardian {req.GuardianId} not found.");

        var studentExists = await _db.Students
            .AnyAsync(s => s.Id == req.StudentId, ct);
        if (!studentExists)
            throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var access = await _db.GuardianPortalAccesses
            .FirstOrDefaultAsync(a => a.GuardianId == req.GuardianId && a.StudentId == req.StudentId, ct);

        if (access is null)
        {
            access = new GuardianPortalAccess
            {
                CorporationId     = guardian.CorporationId,
                GuardianId        = req.GuardianId,
                StudentId         = req.StudentId,
                CanViewSessions   = req.CanViewSessions,
                CanViewAttendance = req.CanViewAttendance,
                CanViewReports    = req.CanViewReports,
                CanViewPlan       = req.CanViewPlan,
                CanViewFinance    = req.CanViewFinance,
                CanViewCamera     = req.CanViewCamera,
                GrantedAt         = DateTimeOffset.UtcNow
            };
            _db.GuardianPortalAccesses.Add(access);
        }
        else
        {
            access.CanViewSessions   = req.CanViewSessions;
            access.CanViewAttendance = req.CanViewAttendance;
            access.CanViewReports    = req.CanViewReports;
            access.CanViewPlan       = req.CanViewPlan;
            access.CanViewFinance    = req.CanViewFinance;
            access.CanViewCamera     = req.CanViewCamera;
            access.RevokedAt         = null;
        }

        var link = await _db.StudentGuardians
            .FirstOrDefaultAsync(sg => sg.StudentId == req.StudentId && sg.GuardianId == req.GuardianId, ct);
        if (link is not null)
            link.PortalAccess = true;

        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToPortalAccessDto(access);
    }
}

// ── RevokePortalAccessCommand ─────────────────────────────────────────────────

public record RevokePortalAccessCommand(Guid GuardianId, Guid StudentId) : IRequest;

public sealed class RevokePortalAccessCommandHandler : IRequestHandler<RevokePortalAccessCommand>
{
    private readonly IAppDbContext _db;

    public RevokePortalAccessCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RevokePortalAccessCommand req, CancellationToken ct)
    {
        var access = await _db.GuardianPortalAccesses
            .FirstOrDefaultAsync(a => a.GuardianId == req.GuardianId && a.StudentId == req.StudentId, ct)
            ?? throw new KeyNotFoundException(
                $"Portal access record not found for guardian {req.GuardianId} / student {req.StudentId}.");

        access.RevokedAt = DateTimeOffset.UtcNow;

        var link = await _db.StudentGuardians
            .FirstOrDefaultAsync(sg => sg.StudentId == req.StudentId && sg.GuardianId == req.GuardianId, ct);
        if (link is not null)
            link.PortalAccess = false;

        await _db.SaveChangesAsync(ct);
    }
}

// ── UpdatePortalPermissionsCommand ────────────────────────────────────────────

public record UpdatePortalPermissionsCommand(
    Guid GuardianId,
    Guid StudentId,
    bool CanViewSessions,
    bool CanViewAttendance,
    bool CanViewReports,
    bool CanViewPlan,
    bool CanViewFinance,
    bool CanViewCamera) : IRequest<GuardianPortalAccessDto>;

public sealed class UpdatePortalPermissionsCommandHandler
    : IRequestHandler<UpdatePortalPermissionsCommand, GuardianPortalAccessDto>
{
    private readonly IAppDbContext _db;

    public UpdatePortalPermissionsCommandHandler(IAppDbContext db) => _db = db;

    public async Task<GuardianPortalAccessDto> Handle(UpdatePortalPermissionsCommand req, CancellationToken ct)
    {
        var access = await _db.GuardianPortalAccesses
            .FirstOrDefaultAsync(a => a.GuardianId == req.GuardianId && a.StudentId == req.StudentId && a.RevokedAt == null, ct)
            ?? throw new KeyNotFoundException(
                $"Active portal access record not found for guardian {req.GuardianId} / student {req.StudentId}.");

        access.CanViewSessions   = req.CanViewSessions;
        access.CanViewAttendance = req.CanViewAttendance;
        access.CanViewReports    = req.CanViewReports;
        access.CanViewPlan       = req.CanViewPlan;
        access.CanViewFinance    = req.CanViewFinance;
        access.CanViewCamera     = req.CanViewCamera;

        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToPortalAccessDto(access);
    }
}
