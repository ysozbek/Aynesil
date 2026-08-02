using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── EnrollStudentCommand ──────────────────────────────────────────────────────

public record EnrollStudentCommand(
    Guid CorporationId,
    Guid CampPeriodId,
    Guid StudentId,
    string Status = "enrolled",
    Guid? StudentPackageId = null) : IRequest<CampEnrollmentDto>;

public class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
{
    private static readonly string[] ValidStatuses = ["enrolled", "waitlist"];

    public EnrollStudentCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.CampPeriodId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be 'enrolled' or 'waitlist'.");
    }
}

public sealed class EnrollStudentCommandHandler
    : IRequestHandler<EnrollStudentCommand, CampEnrollmentDto>
{
    private readonly IAppDbContext _db;

    public EnrollStudentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CampEnrollmentDto> Handle(EnrollStudentCommand req, CancellationToken ct)
    {
        var periodExists = await _db.CampPeriods
            .AnyAsync(p => p.Id == req.CampPeriodId
                        && p.CorporationId == req.CorporationId, ct);
        if (!periodExists)
            throw new KeyNotFoundException($"Camp period {req.CampPeriodId} not found.");

        var alreadyEnrolled = await _db.CampEnrollments
            .AnyAsync(e => e.CampPeriodId == req.CampPeriodId
                        && e.StudentId == req.StudentId
                        && e.Status != "withdrawn", ct);
        if (alreadyEnrolled)
            throw new InvalidOperationException(
                "Student is already enrolled or waitlisted in this camp period.");

        if (req.Status == "enrolled")
        {
            var capacity = await _db.CampPeriods
                .Where(p => p.Id == req.CampPeriodId)
                .Select(p => p.Capacity)
                .FirstOrDefaultAsync(ct);

            if (capacity.HasValue)
            {
                var activeCount = await _db.CampEnrollments
                    .CountAsync(e => e.CampPeriodId == req.CampPeriodId
                                  && e.Status == "enrolled", ct);
                if (activeCount >= capacity.Value)
                    throw new InvalidOperationException(
                        "Camp period is at full capacity. Use 'waitlist' status.");
            }
        }

        var enrollment = CampEnrollment.Create(
            req.CorporationId, req.CampPeriodId, req.StudentId,
            req.Status, req.StudentPackageId);

        _db.CampEnrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);

        return new CampEnrollmentDto(
            enrollment.Id, enrollment.CorporationId,
            enrollment.CampPeriodId, enrollment.StudentId,
            enrollment.StudentPackageId, enrollment.Status,
            enrollment.EnrolledAt, 0, 0, 0);
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
            ?? throw new KeyNotFoundException($"Enrollment {req.EnrollmentId} not found.");

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
            ?? throw new KeyNotFoundException($"Enrollment {req.EnrollmentId} not found.");

        if (enrollment.Period.Capacity.HasValue)
        {
            var activeCount = await _db.CampEnrollments
                .CountAsync(e => e.CampPeriodId == enrollment.CampPeriodId
                              && e.Status == "enrolled", ct);
            if (activeCount >= enrollment.Period.Capacity.Value)
                throw new InvalidOperationException(
                    "Camp period is still at full capacity.");
        }

        enrollment.Enroll();
        await _db.SaveChangesAsync(ct);
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
            ?? throw new KeyNotFoundException($"Enrollment {req.EnrollmentId} not found.");

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
            .FirstOrDefaultAsync(e => e.Id == req.EnrollmentId, ct)
            ?? throw new KeyNotFoundException($"Enrollment {req.EnrollmentId} not found.");

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
            ?? throw new KeyNotFoundException($"Enrollment {req.EnrollmentId} not found.");

        enrollment.UpdatePackage(req.StudentPackageId);
        await _db.SaveChangesAsync(ct);
    }
}
