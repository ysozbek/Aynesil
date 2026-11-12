using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Programs.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Programs.Commands;

// ── CreateEnrollmentCommand ───────────────────────────────────────────────────

public record CreateEnrollmentCommand(
    Guid StudentId,
    Guid? CampusId,
    Guid? StatusId,
    DateOnly? EnrolledOn) : IRequest<EnrollmentDto>;

public class CreateEnrollmentCommandValidator : AbstractValidator<CreateEnrollmentCommand>
{
    public CreateEnrollmentCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
    }
}

public sealed class CreateEnrollmentCommandHandler
    : IRequestHandler<CreateEnrollmentCommand, EnrollmentDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public CreateEnrollmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _tenant = tenant;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EnrollmentDto> Handle(CreateEnrollmentCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var studentExists = await _db.Students.AnyAsync(s => s.CorporationId == corporationId && s.Id == req.StudentId, ct);
        if (!studentExists)
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Student"));

        if (req.StatusId.HasValue)
        {
            var valid = await _db.RefValues.AnyAsync(
                r => r.Id == req.StatusId.Value && r.RefType!.Code == "enrollment_status", ct);
            if (!valid)
                throw new NotFoundException(MsgKeys.Rule.InvalidRefValue, new EnumValue("RefType", "enrollment_status"), req.StatusId);
        }

        var enrollment = Enrollment.Create(
            corporationId, req.StudentId,
            req.CampusId, req.StatusId, req.EnrolledOn,
            _currentUser.UserId);

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);

        return (await ProgramProjection.LoadEnrollmentAsync(_db, enrollment.Id, ct))!;
    }
}

// ── ChangeEnrollmentStatusCommand ─────────────────────────────────────────────

public record ChangeEnrollmentStatusCommand(
    Guid EnrollmentId,
    Guid NewStatusId,
    int RowVersion) : IRequest<EnrollmentDto>;

public class ChangeEnrollmentStatusCommandValidator : AbstractValidator<ChangeEnrollmentStatusCommand>
{
    public ChangeEnrollmentStatusCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.NewStatusId).NotEmpty();
    }
}

public sealed class ChangeEnrollmentStatusCommandHandler
    : IRequestHandler<ChangeEnrollmentStatusCommand, EnrollmentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ChangeEnrollmentStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EnrollmentDto> Handle(ChangeEnrollmentStatusCommand req, CancellationToken ct)
    {
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.Id == req.EnrollmentId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Enrollment"));

        if (enrollment.RowVersion != req.RowVersion)
            throw new BusinessRuleViolationException(MsgKeys.Error.Concurrency);

        var valid = await _db.RefValues.AnyAsync(
            r => r.Id == req.NewStatusId && r.RefType!.Code == "enrollment_status", ct);
        if (!valid)
            throw new NotFoundException(MsgKeys.Rule.InvalidRefValue, new EnumValue("RefType", "enrollment_status"), req.NewStatusId);

        enrollment.ChangeStatus(req.NewStatusId, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        return (await ProgramProjection.LoadEnrollmentAsync(_db, enrollment.Id, ct))!;
    }
}

// ── EndEnrollmentCommand ──────────────────────────────────────────────────────

public record EndEnrollmentCommand(
    Guid EnrollmentId,
    DateOnly? EndedOn,
    string? TerminationReason,
    int RowVersion) : IRequest<EnrollmentDto>;

public sealed class EndEnrollmentCommandHandler : IRequestHandler<EndEnrollmentCommand, EnrollmentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EndEnrollmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EnrollmentDto> Handle(EndEnrollmentCommand req, CancellationToken ct)
    {
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.Id == req.EnrollmentId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Enrollment"));

        if (enrollment.RowVersion != req.RowVersion)
            throw new BusinessRuleViolationException(MsgKeys.Error.Concurrency);

        enrollment.End(
            req.EndedOn ?? DateOnly.FromDateTime(DateTime.UtcNow),
            req.TerminationReason, _currentUser.UserId);

        await _db.SaveChangesAsync(ct);

        return (await ProgramProjection.LoadEnrollmentAsync(_db, enrollment.Id, ct))!;
    }
}
