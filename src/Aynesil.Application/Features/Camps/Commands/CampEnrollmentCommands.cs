using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Persistence;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── EnrollStudentCommand ──────────────────────────────────────────────────────

/// <summary>CorporationId is taken from the request context (JWT), never from the client.</summary>
public record EnrollStudentCommand(
    Guid CampPeriodId,
    Guid StudentId,
    string Status = "enrolled",
    Guid? StudentPackageId = null) : IRequest<CampEnrollmentDto>;

public class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
{
    private static readonly string[] ValidStatuses = ["enrolled", "waitlist"];

    public EnrollStudentCommandValidator()
    {
        RuleFor(x => x.CampPeriodId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("Status"), EnumSets.CampEnrollmentStatus);
    }
}

public sealed class EnrollStudentCommandHandler
    : IRequestHandler<EnrollStudentCommand, CampEnrollmentDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public EnrollStudentCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<CampEnrollmentDto> Handle(EnrollStudentCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var campId = await _db.CampPeriods
            .Where(p => p.Id == req.CampPeriodId && p.CorporationId == corporationId)
            .Select(p => (Guid?)p.CampId)
            .FirstOrDefaultAsync(ct);
        if (campId is null)
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CampPeriod"));

        if (await _db.Camps.AnyAsync(c => c.Id == campId && !c.IsActive, ct))
            throw new BusinessRuleViolationException(MsgKeys.Rule.CampInactive);

        await CampPackages.EnsureBelongsToStudentAsync(_db, req.StudentPackageId, req.StudentId, ct);

        // D6: dönem satırı kilitlenir; eşzamanlı iki kayıt kontenjanı aşamaz.
        return await RowLock.CampPeriodAsync(_db, req.CampPeriodId, async () =>
        {
            // (camp_period_id, student_id) tekildir: aynı döneme ikinci satır açılmaz.
            var existing = await _db.CampEnrollments
                .FirstOrDefaultAsync(e => e.CampPeriodId == req.CampPeriodId && e.StudentId == req.StudentId, ct);
            if (existing is not null && existing.Status != "withdrawn")
                throw new BusinessRuleViolationException(MsgKeys.Rule.StudentAlreadyInCampPeriod);

            var status = req.Status;
            if (status == "enrolled" && await IsFullAsync(req.CampPeriodId, ct))
            {
                // Yeni kayıt: dolu dönemde 'enrolled' istenirse 409 (kullanıcı bekleme listesini seçer).
                // Çekilmiş kaydın geri açılması (D3): dolu dönemde bekleme listesine düşer.
                if (existing is null)
                    throw new BusinessRuleViolationException(MsgKeys.Rule.CampPeriodFullUseWaitlist);
                status = "waitlist";
            }

            CampEnrollment enrollment;
            if (existing is not null)
            {
                existing.Reopen(status, req.StudentPackageId);
                enrollment = existing;
            }
            else
            {
                enrollment = CampEnrollment.Create(corporationId, req.CampPeriodId, req.StudentId, status, req.StudentPackageId);
                _db.CampEnrollments.Add(enrollment);
            }
            await _db.SaveChangesAsync(ct);

            return new CampEnrollmentDto(
                enrollment.Id, enrollment.CorporationId,
                enrollment.CampPeriodId, enrollment.StudentId,
                enrollment.StudentPackageId, enrollment.Status,
                enrollment.EnrolledAt, 0, 0, 0);
        }, ct);
    }

    private async Task<bool> IsFullAsync(Guid campPeriodId, CancellationToken ct)
    {
        var capacity = await _db.CampPeriods
            .Where(p => p.Id == campPeriodId)
            .Select(p => p.Capacity)
            .FirstOrDefaultAsync(ct);
        if (!capacity.HasValue) return false;
        var enrolled = await _db.CampEnrollments
            .CountAsync(e => e.CampPeriodId == campPeriodId && e.Status == "enrolled", ct);
        return enrolled >= capacity.Value;
    }
}

/// <summary>D5: kamp kaydına bağlanan paket kaydın öğrencisine ait olmalı (bileşik FK yalnızca aynı kurumu garanti eder).</summary>
internal static class CampPackages
{
    public static async Task EnsureBelongsToStudentAsync(IAppDbContext db, Guid? studentPackageId, Guid studentId, CancellationToken ct)
    {
        if (studentPackageId is not Guid packageId) return;
        if (!await db.StudentPackages.AnyAsync(p => p.Id == packageId && p.StudentId == studentId, ct))
            throw new ValidationException([ValidationFailures.For("studentPackageId", MsgKeys.Rule.CampPackageNotStudents)]);
    }
}

// ── MoveToWaitlistCommand ─────────────────────────────────────────────────────

public record MoveToWaitlistCommand(Guid EnrollmentId) : IRequest;

public sealed class MoveToWaitlistCommandHandler : IRequestHandler<MoveToWaitlistCommand>
{
    private readonly IAppDbContext _db;

    public MoveToWaitlistCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(MoveToWaitlistCommand req, CancellationToken ct)
    {
        var enrollment = await _db.CampEnrollments
            .FirstOrDefaultAsync(e => e.Id == req.EnrollmentId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Enrollment"));

        enrollment.MoveToWaitlist();
        await _db.SaveChangesAsync(ct);
    }
}

// ── PromoteFromWaitlistCommand ────────────────────────────────────────────────

public record PromoteFromWaitlistCommand(Guid EnrollmentId) : IRequest;

public sealed class PromoteFromWaitlistCommandHandler : IRequestHandler<PromoteFromWaitlistCommand>
{
    private readonly IAppDbContext _db;

    public PromoteFromWaitlistCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(PromoteFromWaitlistCommand req, CancellationToken ct)
    {
        var enrollment = await _db.CampEnrollments
            .Include(e => e.Period)
            .FirstOrDefaultAsync(e => e.Id == req.EnrollmentId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Enrollment"));

        // D6: dönem satırı kilitlenir; eşzamanlı alma/kayıt kontenjanı aşamaz.
        await RowLock.CampPeriodAsync(_db, enrollment.CampPeriodId, async () =>
        {
            if (enrollment.Period.Capacity.HasValue)
            {
                var activeCount = await _db.CampEnrollments
                    .CountAsync(e => e.CampPeriodId == enrollment.CampPeriodId
                                  && e.Status == "enrolled", ct);
                if (activeCount >= enrollment.Period.Capacity.Value)
                    throw new BusinessRuleViolationException(MsgKeys.Rule.CampPeriodStillFull);
            }

            enrollment.Enroll();
            await _db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }
}

// ── WithdrawEnrollmentCommand ─────────────────────────────────────────────────

public record WithdrawEnrollmentCommand(Guid EnrollmentId) : IRequest;

public sealed class WithdrawEnrollmentCommandHandler : IRequestHandler<WithdrawEnrollmentCommand>
{
    private readonly IAppDbContext _db;

    public WithdrawEnrollmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(WithdrawEnrollmentCommand req, CancellationToken ct)
    {
        var enrollment = await _db.CampEnrollments
            .FirstOrDefaultAsync(e => e.Id == req.EnrollmentId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Enrollment"));

        enrollment.Withdraw();
        await _db.SaveChangesAsync(ct);
    }
}

// ── CompleteEnrollmentCommand ─────────────────────────────────────────────────

public record CompleteEnrollmentCommand(Guid EnrollmentId) : IRequest;

public sealed class CompleteEnrollmentCommandHandler : IRequestHandler<CompleteEnrollmentCommand>
{
    private readonly IAppDbContext _db;

    public CompleteEnrollmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CompleteEnrollmentCommand req, CancellationToken ct)
    {
        var enrollment = await _db.CampEnrollments
            .Include(e => e.Period)
            .FirstOrDefaultAsync(e => e.Id == req.EnrollmentId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Enrollment"));

        // D4: toplu tamamlamadaki kural tekil tamamlamada da geçerli.
        if (enrollment.Period.StartDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessRuleViolationException(MsgKeys.Rule.CampEnrollmentCompleteNotStarted);

        enrollment.Complete();
        await _db.SaveChangesAsync(ct);
    }
}

// ── BulkCompleteEnrollmentsCommand ────────────────────────────────────────────

/// <summary>Marks all 'enrolled' students in a period as 'completed' at once.</summary>
public record BulkCompleteEnrollmentsCommand(Guid CampPeriodId) : IRequest<int>;

public sealed class BulkCompleteEnrollmentsCommandHandler
    : IRequestHandler<BulkCompleteEnrollmentsCommand, int>
{
    private readonly IAppDbContext _db;

    public BulkCompleteEnrollmentsCommandHandler(IAppDbContext db) => _db = db;

    public async Task<int> Handle(BulkCompleteEnrollmentsCommand req, CancellationToken ct)
    {
        var period = await _db.CampPeriods
            .FirstOrDefaultAsync(p => p.Id == req.CampPeriodId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "CampPeriod"));

        if (period.StartDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessRuleViolationException(MsgKeys.Rule.CampPeriodNotStarted);

        var enrollments = await _db.CampEnrollments
            .Where(e => e.CampPeriodId == req.CampPeriodId && e.Status == "enrolled")
            .ToListAsync(ct);

        foreach (var e in enrollments)
            e.Complete();

        await _db.SaveChangesAsync(ct);
        return enrollments.Count;
    }
}

// ── UpdateEnrollmentPackageCommand ────────────────────────────────────────────

public record UpdateEnrollmentPackageCommand(
    Guid EnrollmentId,
    Guid? StudentPackageId) : IRequest;

public sealed class UpdateEnrollmentPackageCommandHandler
    : IRequestHandler<UpdateEnrollmentPackageCommand>
{
    private readonly IAppDbContext _db;

    public UpdateEnrollmentPackageCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateEnrollmentPackageCommand req, CancellationToken ct)
    {
        var enrollment = await _db.CampEnrollments
            .FirstOrDefaultAsync(e => e.Id == req.EnrollmentId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Enrollment"));

        await CampPackages.EnsureBelongsToStudentAsync(_db, req.StudentPackageId, enrollment.StudentId, ct);
        enrollment.UpdatePackage(req.StudentPackageId);
        await _db.SaveChangesAsync(ct);
    }
}
