using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Leaves.Dtos;
using Aynesil.Domain.Modules.Ops.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leaves.Commands;

// ── SubmitLeaveRequestCommand ─────────────────────────────────────────────────

public record SubmitLeaveRequestCommand(
    Guid CorporationId,
    Guid EducatorId,
    Guid? LeaveTypeId,
    string Unit,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal? Quantity,
    string? Reason,
    Guid? CreatedBy = null) : IRequest<Guid>;

public class SubmitLeaveRequestCommandValidator : AbstractValidator<SubmitLeaveRequestCommand>
{
    private static readonly string[] ValidUnits = ["day", "hour"];

    public SubmitLeaveRequestCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.Unit)
            .Must(u => ValidUnits.Contains(u))
            .WithMessage("Invalid unit. Must be: day, hour.");
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("EndsAt must be after StartsAt.");
        RuleFor(x => x.Quantity)
            .GreaterThan(0).When(x => x.Quantity.HasValue)
            .WithMessage("Quantity must be positive.");
    }
}

public sealed class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, Guid>
{
    private readonly IAppDbContext _db;

    public SubmitLeaveRequestCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(SubmitLeaveRequestCommand req, CancellationToken ct)
    {
        var educatorExists = await _db.Educators
            .AnyAsync(e => e.Id == req.EducatorId && e.CorporationId == req.CorporationId, ct);

        if (!educatorExists)
            throw new KeyNotFoundException($"Educator {req.EducatorId} not found.");

        // Application-level overlap check (DB constraint will enforce at commit, but we give a friendly message).
        var overlap = await _db.LeaveRequests
            .AnyAsync(lr =>
                lr.EducatorId == req.EducatorId
                && (lr.Status == "pending" || lr.Status == "approved")
                && lr.StartsAt < req.EndsAt
                && lr.EndsAt > req.StartsAt, ct);

        if (overlap)
            throw new InvalidOperationException(
                "A pending or approved leave request already exists for the same educator during this period.");

        var request = LeaveRequest.Submit(
            req.CorporationId, req.EducatorId, req.LeaveTypeId,
            req.Unit, req.StartsAt, req.EndsAt,
            req.Quantity, req.Reason, req.CreatedBy);

        _db.LeaveRequests.Add(request);
        await _db.SaveChangesAsync(ct);
        return request.Id;
    }
}

// ── UpdateLeaveRequestCommand ─────────────────────────────────────────────────

public record UpdateLeaveRequestCommand(
    Guid Id,
    Guid? LeaveTypeId,
    string Unit,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal? Quantity,
    string? Reason,
    int RowVersion) : IRequest;

public class UpdateLeaveRequestCommandValidator : AbstractValidator<UpdateLeaveRequestCommand>
{
    private static readonly string[] ValidUnits = ["day", "hour"];

    public UpdateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Unit)
            .Must(u => ValidUnits.Contains(u))
            .WithMessage("Invalid unit. Must be: day, hour.");
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("EndsAt must be after StartsAt.");
        RuleFor(x => x.Quantity)
            .GreaterThan(0).When(x => x.Quantity.HasValue)
            .WithMessage("Quantity must be positive.");
    }
}

public sealed class UpdateLeaveRequestCommandHandler : IRequestHandler<UpdateLeaveRequestCommand>
{
    private readonly IAppDbContext _db;

    public UpdateLeaveRequestCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateLeaveRequestCommand req, CancellationToken ct)
    {
        var leave = await _db.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"LeaveRequest {req.Id} not found.");

        // Overlap check excluding the current request.
        var overlap = await _db.LeaveRequests
            .AnyAsync(lr =>
                lr.Id != req.Id
                && lr.EducatorId == leave.EducatorId
                && (lr.Status == "pending" || lr.Status == "approved")
                && lr.StartsAt < req.EndsAt
                && lr.EndsAt > req.StartsAt, ct);

        if (overlap)
            throw new InvalidOperationException(
                "A pending or approved leave request already exists for the same educator during this period.");

        leave.Update(req.LeaveTypeId, req.Unit, req.StartsAt, req.EndsAt, req.Quantity, req.Reason);
        await _db.SaveChangesAsync(ct);
    }
}

// ── ApproveLeaveRequestCommand ────────────────────────────────────────────────

public record ApproveLeaveRequestCommand(
    Guid Id,
    Guid? ApproverId,
    string? Comment,
    int StepNo = 1) : IRequest<LeaveApprovalDto>;

public class ApproveLeaveRequestCommandValidator : AbstractValidator<ApproveLeaveRequestCommand>
{
    public ApproveLeaveRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.StepNo).GreaterThan(0);
    }
}

public sealed class ApproveLeaveRequestCommandHandler
    : IRequestHandler<ApproveLeaveRequestCommand, LeaveApprovalDto>
{
    private readonly IAppDbContext _db;

    public ApproveLeaveRequestCommandHandler(IAppDbContext db) => _db = db;

    public async Task<LeaveApprovalDto> Handle(ApproveLeaveRequestCommand req, CancellationToken ct)
    {
        var leave = await _db.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"LeaveRequest {req.Id} not found.");

        // Balance validation — if a balance record exists, enforce it.
        if (leave.Quantity.HasValue && leave.LeaveTypeId.HasValue)
        {
            var periodYear = leave.StartsAt.Year;
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(b =>
                    b.EducatorId == leave.EducatorId
                    && b.LeaveTypeId == leave.LeaveTypeId
                    && b.PeriodYear == periodYear, ct);

            if (balance != null && balance.Remaining < leave.Quantity.Value)
                throw new InvalidOperationException(
                    $"Insufficient leave balance. Available: {balance.Remaining} {balance.Unit}, Requested: {leave.Quantity.Value} {leave.Unit}.");

            // Deduct from balance.
            if (balance != null)
                balance.Consume(leave.Quantity.Value);
        }

        leave.Approve();

        var approval = LeaveApproval.Record(
            leave.CorporationId, leave.Id,
            req.ApproverId, "approved",
            req.Comment, req.StepNo);

        _db.LeaveApprovals.Add(approval);
        await _db.SaveChangesAsync(ct);

        return new LeaveApprovalDto(
            approval.Id, approval.LeaveRequestId,
            approval.StepNo, approval.ApproverId,
            approval.Decision, approval.Comment,
            approval.DecidedAt);
    }
}

// ── RejectLeaveRequestCommand ─────────────────────────────────────────────────

public record RejectLeaveRequestCommand(
    Guid Id,
    Guid? ApproverId,
    string? Comment,
    int StepNo = 1) : IRequest<LeaveApprovalDto>;

public class RejectLeaveRequestCommandValidator : AbstractValidator<RejectLeaveRequestCommand>
{
    public RejectLeaveRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.StepNo).GreaterThan(0);
    }
}

public sealed class RejectLeaveRequestCommandHandler
    : IRequestHandler<RejectLeaveRequestCommand, LeaveApprovalDto>
{
    private readonly IAppDbContext _db;

    public RejectLeaveRequestCommandHandler(IAppDbContext db) => _db = db;

    public async Task<LeaveApprovalDto> Handle(RejectLeaveRequestCommand req, CancellationToken ct)
    {
        var leave = await _db.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"LeaveRequest {req.Id} not found.");

        leave.Reject();

        var approval = LeaveApproval.Record(
            leave.CorporationId, leave.Id,
            req.ApproverId, "rejected",
            req.Comment, req.StepNo);

        _db.LeaveApprovals.Add(approval);
        await _db.SaveChangesAsync(ct);

        return new LeaveApprovalDto(
            approval.Id, approval.LeaveRequestId,
            approval.StepNo, approval.ApproverId,
            approval.Decision, approval.Comment,
            approval.DecidedAt);
    }
}

// ── CancelLeaveRequestCommand ─────────────────────────────────────────────────

public record CancelLeaveRequestCommand(Guid Id) : IRequest;

public sealed class CancelLeaveRequestCommandHandler : IRequestHandler<CancelLeaveRequestCommand>
{
    private readonly IAppDbContext _db;

    public CancelLeaveRequestCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CancelLeaveRequestCommand req, CancellationToken ct)
    {
        var leave = await _db.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"LeaveRequest {req.Id} not found.");

        var wasApproved = leave.Status == "approved";

        leave.Cancel();

        // Restore balance if cancelling an already-approved leave.
        if (wasApproved && leave.Quantity.HasValue && leave.LeaveTypeId.HasValue)
        {
            var periodYear = leave.StartsAt.Year;
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(b =>
                    b.EducatorId == leave.EducatorId
                    && b.LeaveTypeId == leave.LeaveTypeId
                    && b.PeriodYear == periodYear, ct);

            balance?.Restore(leave.Quantity.Value);
        }

        await _db.SaveChangesAsync(ct);
    }
}

// ── InitializeLeaveBalanceCommand ─────────────────────────────────────────────

public record InitializeLeaveBalanceCommand(
    Guid CorporationId,
    Guid EducatorId,
    Guid? LeaveTypeId,
    int PeriodYear,
    decimal Entitled,
    string Unit = "day") : IRequest<Guid>;

public class InitializeLeaveBalanceCommandValidator : AbstractValidator<InitializeLeaveBalanceCommand>
{
    private static readonly string[] ValidUnits = ["day", "hour"];

    public InitializeLeaveBalanceCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.PeriodYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Entitled).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Unit)
            .Must(u => ValidUnits.Contains(u))
            .WithMessage("Invalid unit. Must be: day, hour.");
    }
}

public sealed class InitializeLeaveBalanceCommandHandler
    : IRequestHandler<InitializeLeaveBalanceCommand, Guid>
{
    private readonly IAppDbContext _db;

    public InitializeLeaveBalanceCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(InitializeLeaveBalanceCommand req, CancellationToken ct)
    {
        var duplicate = await _db.LeaveBalances
            .AnyAsync(b =>
                b.EducatorId == req.EducatorId
                && b.LeaveTypeId == req.LeaveTypeId
                && b.PeriodYear == req.PeriodYear, ct);

        if (duplicate)
            throw new InvalidOperationException(
                $"A leave balance for educator {req.EducatorId}, leave type {req.LeaveTypeId}, year {req.PeriodYear} already exists.");

        var balance = LeaveBalance.Initialize(
            req.CorporationId, req.EducatorId, req.LeaveTypeId,
            req.PeriodYear, req.Entitled, req.Unit);

        _db.LeaveBalances.Add(balance);
        await _db.SaveChangesAsync(ct);
        return balance.Id;
    }
}

// ── AdjustLeaveEntitlementCommand ─────────────────────────────────────────────

public record AdjustLeaveEntitlementCommand(
    Guid Id,
    decimal Entitled) : IRequest;

public class AdjustLeaveEntitlementCommandValidator : AbstractValidator<AdjustLeaveEntitlementCommand>
{
    public AdjustLeaveEntitlementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Entitled).GreaterThanOrEqualTo(0);
    }
}

public sealed class AdjustLeaveEntitlementCommandHandler
    : IRequestHandler<AdjustLeaveEntitlementCommand>
{
    private readonly IAppDbContext _db;

    public AdjustLeaveEntitlementCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(AdjustLeaveEntitlementCommand req, CancellationToken ct)
    {
        var balance = await _db.LeaveBalances
            .FirstOrDefaultAsync(b => b.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"LeaveBalance {req.Id} not found.");

        balance.AdjustEntitlement(req.Entitled);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CarryForwardLeaveBalanceCommand ───────────────────────────────────────────

/// <summary>
/// Carries forward unused leave from one period year to the next.
/// Creates (or updates) the target year's balance by adding the carry-forward amount.
/// </summary>
public record CarryForwardLeaveBalanceCommand(
    Guid EducatorId,
    Guid CorporationId,
    Guid? LeaveTypeId,
    int FromYear,
    int ToYear,
    decimal? MaxCarryForward = null) : IRequest;

public class CarryForwardLeaveBalanceCommandValidator : AbstractValidator<CarryForwardLeaveBalanceCommand>
{
    public CarryForwardLeaveBalanceCommandValidator()
    {
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.FromYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.ToYear).InclusiveBetween(2000, 2100)
            .GreaterThan(x => x.FromYear).WithMessage("ToYear must be greater than FromYear.");
        RuleFor(x => x.MaxCarryForward)
            .GreaterThanOrEqualTo(0).When(x => x.MaxCarryForward.HasValue)
            .WithMessage("MaxCarryForward must be non-negative.");
    }
}

public sealed class CarryForwardLeaveBalanceCommandHandler
    : IRequestHandler<CarryForwardLeaveBalanceCommand>
{
    private readonly IAppDbContext _db;

    public CarryForwardLeaveBalanceCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CarryForwardLeaveBalanceCommand req, CancellationToken ct)
    {
        var source = await _db.LeaveBalances
            .FirstOrDefaultAsync(b =>
                b.EducatorId == req.EducatorId
                && b.LeaveTypeId == req.LeaveTypeId
                && b.PeriodYear == req.FromYear, ct)
            ?? throw new KeyNotFoundException(
                $"No leave balance found for educator {req.EducatorId}, leave type {req.LeaveTypeId}, year {req.FromYear}.");

        var carryAmount = source.Remaining;
        if (req.MaxCarryForward.HasValue)
            carryAmount = Math.Min(carryAmount, req.MaxCarryForward.Value);

        if (carryAmount <= 0) return;

        var target = await _db.LeaveBalances
            .FirstOrDefaultAsync(b =>
                b.EducatorId == req.EducatorId
                && b.LeaveTypeId == req.LeaveTypeId
                && b.PeriodYear == req.ToYear, ct);

        if (target == null)
        {
            target = LeaveBalance.Initialize(
                req.CorporationId, req.EducatorId, req.LeaveTypeId,
                req.ToYear, carryAmount, source.Unit);

            _db.LeaveBalances.Add(target);
        }
        else
        {
            target.CarryForward(carryAmount);
        }

        await _db.SaveChangesAsync(ct);
    }
}
