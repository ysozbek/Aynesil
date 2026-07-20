using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.CareTeam.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.CareTeam.Commands;

// ── AssignCareTeamMemberCommand ───────────────────────────────────────────────

/// <summary>Creates a new care-team assignment for a student. Requires care_team:assign permission.</summary>
public record AssignCareTeamMemberCommand(
    Guid   CorporationId,
    Guid   StudentId,
    Guid   EducatorId,
    /// <summary>Must reference a ref.ref_value with ref_type = 'care_team_role'.</summary>
    Guid   RoleId,
    bool   IsPrimary,
    DateOnly ActiveFrom,
    DateOnly? ActiveTo,
    Guid?  CampusId,
    /// <summary>Optional ref.ref_value with ref_type = 'care_team_grant_type'. Null = permanent.</summary>
    Guid?  GrantTypeId,
    Guid?  SourceAssignmentId,
    /// <summary>Required when GrantTypeCode is 'emergency' or 'delegated'.</summary>
    string? Reason) : IRequest<CareTeamAssignmentDto>;

public sealed class AssignCareTeamMemberValidator : AbstractValidator<AssignCareTeamMemberCommand>
{
    public AssignCareTeamMemberValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.ActiveFrom).NotEmpty();
        RuleFor(x => x.ActiveTo)
            .GreaterThan(x => x.ActiveFrom)
            .When(x => x.ActiveTo.HasValue)
            .WithMessage("ActiveTo must be after ActiveFrom.");
    }
}

public sealed class AssignCareTeamMemberCommandHandler
    : IRequestHandler<AssignCareTeamMemberCommand, CareTeamAssignmentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AssignCareTeamMemberCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CareTeamAssignmentDto> Handle(
        AssignCareTeamMemberCommand req, CancellationToken ct)
    {
        // Validate student exists within tenant
        var studentExists = await _db.Students
            .AnyAsync(s => s.Id == req.StudentId && s.CorporationId == req.CorporationId, ct);
        if (!studentExists)
            throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        // Validate educator exists within tenant
        var educatorExists = await _db.Educators
            .AnyAsync(e => e.Id == req.EducatorId && e.CorporationId == req.CorporationId, ct);
        if (!educatorExists)
            throw new KeyNotFoundException($"Educator {req.EducatorId} not found.");

        // Validate RoleId is a valid care_team_role ref_value
        var roleValid = await _db.RefValues
            .AnyAsync(rv => rv.Id == req.RoleId && rv.RefType!.Code == "care_team_role", ct);
        if (!roleValid)
            throw new InvalidOperationException($"RoleId {req.RoleId} is not a valid care_team_role.");

        // Validate GrantTypeId if provided
        if (req.GrantTypeId.HasValue)
        {
            var grantTypeValid = await _db.RefValues
                .AnyAsync(rv => rv.Id == req.GrantTypeId.Value && rv.RefType!.Code == "care_team_grant_type", ct);
            if (!grantTypeValid)
                throw new InvalidOperationException($"GrantTypeId {req.GrantTypeId} is not a valid care_team_grant_type.");

            // Reason is required for emergency and delegated grant types
            var grantTypeCode = await _db.RefValues
                .Where(rv => rv.Id == req.GrantTypeId.Value)
                .Select(rv => rv.Code)
                .FirstOrDefaultAsync(ct);

            if (grantTypeCode is "emergency" or "delegated" && string.IsNullOrWhiteSpace(req.Reason))
                throw new InvalidOperationException(
                    $"Reason is required for grant_type '{grantTypeCode}'.");
        }

        // Validate SourceAssignmentId if provided
        if (req.SourceAssignmentId.HasValue)
        {
            var sourceExists = await _db.StudentCareAssignments
                .AnyAsync(a => a.Id == req.SourceAssignmentId.Value, ct);
            if (!sourceExists)
                throw new KeyNotFoundException($"SourceAssignmentId {req.SourceAssignmentId} not found.");
        }

        // If IsPrimary = true, check for existing active primary for this student
        if (req.IsPrimary)
        {
            var now = DateOnly.FromDateTime(DateTime.UtcNow);
            var existingPrimary = await _db.StudentCareAssignments
                .AnyAsync(a =>
                    a.StudentId == req.StudentId &&
                    a.IsPrimary &&
                    a.Status == "active" &&
                    a.ActiveFrom <= now &&
                    (a.ActiveTo == null || a.ActiveTo > now), ct);

            if (existingPrimary)
                throw new InvalidOperationException(
                    "A primary care-team assignment already exists for this student. " +
                    "Remove the existing primary assignment before assigning a new one.");
        }

        var assignment = new StudentCareAssignment
        {
            CorporationId      = req.CorporationId,
            StudentId          = req.StudentId,
            EducatorId         = req.EducatorId,
            CampusId           = req.CampusId,
            RoleId             = req.RoleId,
            IsPrimary          = req.IsPrimary,
            Status             = "active",
            ActiveFrom         = req.ActiveFrom,
            ActiveTo           = req.ActiveTo,
            GrantTypeId        = req.GrantTypeId,
            SourceAssignmentId = req.SourceAssignmentId,
            GrantedBy          = _currentUser.UserId,
            Reason             = req.Reason,
            CreatedBy          = _currentUser.UserId
        };

        _db.StudentCareAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        return await LoadAssignmentDtoAsync(_db, assignment.Id, ct)
            ?? throw new InvalidOperationException("Assignment not found after save.");
    }

    internal static async Task<CareTeamAssignmentDto?> LoadAssignmentDtoAsync(
        IAppDbContext db, Guid id, CancellationToken ct)
    {
        return await (
            from a in db.StudentCareAssignments.AsNoTracking()
            join edu in db.Educators.AsNoTracking() on a.EducatorId equals edu.Id
            join role in db.RefValues.AsNoTracking() on a.RoleId equals role.Id into roleGrp
            from role in roleGrp.DefaultIfEmpty()
            join gtype in db.RefValues.AsNoTracking() on a.GrantTypeId equals gtype.Id into gtGrp
            from gtype in gtGrp.DefaultIfEmpty()
            where a.Id == id
            select new CareTeamAssignmentDto(
                a.Id, a.CorporationId, a.StudentId, a.EducatorId,
                edu.FirstName + " " + edu.LastName,
                a.CampusId, a.RoleId,
                role != null ? role.Code : null,
                a.IsPrimary, a.Status, a.ActiveFrom, a.ActiveTo,
                a.GrantTypeId, gtype != null ? gtype.Code : null,
                a.SourceAssignmentId, a.GrantedBy, a.Reason,
                a.CreatedAt, a.RowVersion)
        ).FirstOrDefaultAsync(ct);
    }
}


// ── UpdateCareTeamAssignmentCommand ───────────────────────────────────────────

/// <summary>Updates dates, role, or primary status of an existing assignment.</summary>
public record UpdateCareTeamAssignmentCommand(
    Guid      Id,
    Guid?     RoleId,
    bool?     IsPrimary,
    DateOnly? ActiveFrom,
    DateOnly? ActiveTo,
    Guid?     CampusId,
    string?   Reason,
    int       RowVersion) : IRequest<CareTeamAssignmentDto>;

public sealed class UpdateCareTeamAssignmentValidator : AbstractValidator<UpdateCareTeamAssignmentCommand>
{
    public UpdateCareTeamAssignmentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
        RuleFor(x => x.ActiveTo)
            .GreaterThan(x => x.ActiveFrom!.Value)
            .When(x => x.ActiveTo.HasValue && x.ActiveFrom.HasValue)
            .WithMessage("ActiveTo must be after ActiveFrom.");
    }
}

public sealed class UpdateCareTeamAssignmentCommandHandler
    : IRequestHandler<UpdateCareTeamAssignmentCommand, CareTeamAssignmentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCareTeamAssignmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CareTeamAssignmentDto> Handle(
        UpdateCareTeamAssignmentCommand req, CancellationToken ct)
    {
        var assignment = await _db.StudentCareAssignments
            .FirstOrDefaultAsync(a => a.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"CareTeamAssignment {req.Id} not found.");

        if (assignment.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "Concurrency conflict: the assignment was modified by another request. Reload and retry.");

        if (req.RoleId.HasValue)
        {
            var roleValid = await _db.RefValues
                .AnyAsync(rv => rv.Id == req.RoleId.Value && rv.RefType!.Code == "care_team_role", ct);
            if (!roleValid)
                throw new InvalidOperationException($"RoleId {req.RoleId} is not a valid care_team_role.");
            assignment.RoleId = req.RoleId.Value;
        }

        if (req.IsPrimary.HasValue)
            assignment.IsPrimary = req.IsPrimary.Value;

        if (req.ActiveFrom.HasValue)
            assignment.ActiveFrom = req.ActiveFrom.Value;

        if (req.ActiveTo.HasValue)
            assignment.ActiveTo = req.ActiveTo;

        if (req.CampusId.HasValue)
            assignment.CampusId = req.CampusId;

        if (req.Reason is not null)
            assignment.Reason = req.Reason;

        assignment.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);

        return await AssignCareTeamMemberCommandHandler
                .LoadAssignmentDtoAsync(_db, assignment.Id, ct)
            ?? throw new InvalidOperationException("Assignment not found after save.");
    }
}


// ── RemoveCareTeamAssignmentCommand ───────────────────────────────────────────

/// <summary>
/// Soft-ends an assignment by setting ActiveTo = today and Status = "ended".
/// History is preserved for audit. Hard delete is forbidden.
/// </summary>
public record RemoveCareTeamAssignmentCommand(
    Guid    Id,
    string? Reason,
    int     RowVersion) : IRequest;

public sealed class RemoveCareTeamAssignmentCommandHandler
    : IRequestHandler<RemoveCareTeamAssignmentCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RemoveCareTeamAssignmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveCareTeamAssignmentCommand req, CancellationToken ct)
    {
        var assignment = await _db.StudentCareAssignments
            .FirstOrDefaultAsync(a => a.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"CareTeamAssignment {req.Id} not found.");

        if (assignment.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "Concurrency conflict: the assignment was modified by another request. Reload and retry.");

        if (assignment.Status == "ended")
            throw new InvalidOperationException("Assignment is already ended.");

        // Soft removal: set end date to today, mark as ended. Never hard-delete.
        assignment.ActiveTo  = DateOnly.FromDateTime(DateTime.UtcNow);
        assignment.Status    = "ended";
        assignment.Reason    = req.Reason ?? assignment.Reason;
        assignment.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);
    }
}
