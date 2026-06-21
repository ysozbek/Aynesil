using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Commands;

// ── ConsumeCreditsCommand ─────────────────────────────────────────────────────
// Primary consumer: session completion workflow.

public record ConsumeCreditsCommand(
    Guid StudentPackageId,
    decimal Amount,
    Guid? SessionId,
    string? Reason) : IRequest<CreditLedgerEntryDto>;

public class ConsumeCreditsCommandValidator : AbstractValidator<ConsumeCreditsCommand>
{
    public ConsumeCreditsCommandValidator()
    {
        RuleFor(x => x.StudentPackageId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class ConsumeCreditsCommandHandler
    : IRequestHandler<ConsumeCreditsCommand, CreditLedgerEntryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ConsumeCreditsCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<CreditLedgerEntryDto> Handle(
        ConsumeCreditsCommand req, CancellationToken ct)
    {
        var package = await _db.StudentPackages
            .Include(p => p.CreditLedgerEntries)
            .FirstOrDefaultAsync(p => p.Id == req.StudentPackageId, ct)
            ?? throw new KeyNotFoundException($"Student package {req.StudentPackageId} not found.");

        var remaining = package.CreditLedgerEntries.Sum(e => e.Delta);

        if (remaining < req.Amount)
            throw new InvalidOperationException(
                $"Insufficient credits. Package has {remaining:F2} credits remaining; {req.Amount:F2} requested.");

        var entry = package.ConsumeCredits(
            req.Amount, req.SessionId, req.Reason, _currentUser.UserId);

        _db.CreditLedgerEntries.Add(entry);

        // Auto-exhaust the package when no credits remain
        var newBalance = remaining - req.Amount;
        if (newBalance <= 0)
            package.MarkExhausted();

        await _db.SaveChangesAsync(ct);

        return new CreditLedgerEntryDto(
            entry.Id, entry.StudentPackageId, entry.EntryType,
            entry.Delta, newBalance,
            entry.SessionId, entry.Reason, entry.OccurredAt, entry.CreatedBy);
    }
}

// ── GrantBonusCreditsCommand ──────────────────────────────────────────────────

public record GrantBonusCreditsCommand(
    Guid StudentPackageId,
    decimal Amount,
    string Reason) : IRequest<CreditLedgerEntryDto>;

public class GrantBonusCreditsCommandValidator : AbstractValidator<GrantBonusCreditsCommand>
{
    public GrantBonusCreditsCommandValidator()
    {
        RuleFor(x => x.StudentPackageId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class GrantBonusCreditsCommandHandler
    : IRequestHandler<GrantBonusCreditsCommand, CreditLedgerEntryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GrantBonusCreditsCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<CreditLedgerEntryDto> Handle(
        GrantBonusCreditsCommand req, CancellationToken ct)
    {
        var package = await _db.StudentPackages
            .Include(p => p.CreditLedgerEntries)
            .FirstOrDefaultAsync(p => p.Id == req.StudentPackageId, ct)
            ?? throw new KeyNotFoundException($"Student package {req.StudentPackageId} not found.");

        var entry = package.GrantCredits(req.Amount, req.Reason, _currentUser.UserId);

        _db.CreditLedgerEntries.Add(entry);

        var newBalance = package.CreditLedgerEntries.Sum(e => e.Delta) + req.Amount;
        await _db.SaveChangesAsync(ct);

        return new CreditLedgerEntryDto(
            entry.Id, entry.StudentPackageId, entry.EntryType,
            entry.Delta, newBalance,
            entry.SessionId, entry.Reason, entry.OccurredAt, entry.CreatedBy);
    }
}

// ── RefundSessionCreditsCommand ───────────────────────────────────────────────
// Called when a session is cancelled after credits were consumed.

public record RefundSessionCreditsCommand(
    Guid StudentPackageId,
    decimal Amount,
    string Reason) : IRequest<CreditLedgerEntryDto>;

public class RefundSessionCreditsCommandValidator
    : AbstractValidator<RefundSessionCreditsCommand>
{
    public RefundSessionCreditsCommandValidator()
    {
        RuleFor(x => x.StudentPackageId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class RefundSessionCreditsCommandHandler
    : IRequestHandler<RefundSessionCreditsCommand, CreditLedgerEntryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RefundSessionCreditsCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<CreditLedgerEntryDto> Handle(
        RefundSessionCreditsCommand req, CancellationToken ct)
    {
        var package = await _db.StudentPackages
            .Include(p => p.CreditLedgerEntries)
            .FirstOrDefaultAsync(p => p.Id == req.StudentPackageId, ct)
            ?? throw new KeyNotFoundException($"Student package {req.StudentPackageId} not found.");

        var entry = package.RefundCredits(req.Amount, req.Reason, _currentUser.UserId);

        _db.CreditLedgerEntries.Add(entry);

        // Restore to active if it was exhausted
        if (package.Status == "exhausted")
            package.GrantCredits(0, "");   // status restored implicitly by handler logic

        var newBalance = package.CreditLedgerEntries.Sum(e => e.Delta) + req.Amount;
        await _db.SaveChangesAsync(ct);

        return new CreditLedgerEntryDto(
            entry.Id, entry.StudentPackageId, entry.EntryType,
            entry.Delta, newBalance,
            entry.SessionId, entry.Reason, entry.OccurredAt, entry.CreatedBy);
    }
}

// ── AdjustCreditsCommand ──────────────────────────────────────────────────────

public record AdjustCreditsCommand(
    Guid StudentPackageId,
    decimal Delta,
    string Reason) : IRequest<CreditLedgerEntryDto>;

public class AdjustCreditsCommandValidator : AbstractValidator<AdjustCreditsCommand>
{
    public AdjustCreditsCommandValidator()
    {
        RuleFor(x => x.StudentPackageId).NotEmpty();
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Adjustment delta cannot be zero.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class AdjustCreditsCommandHandler
    : IRequestHandler<AdjustCreditsCommand, CreditLedgerEntryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdjustCreditsCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<CreditLedgerEntryDto> Handle(
        AdjustCreditsCommand req, CancellationToken ct)
    {
        var package = await _db.StudentPackages
            .Include(p => p.CreditLedgerEntries)
            .FirstOrDefaultAsync(p => p.Id == req.StudentPackageId, ct)
            ?? throw new KeyNotFoundException($"Student package {req.StudentPackageId} not found.");

        var currentBalance = package.CreditLedgerEntries.Sum(e => e.Delta);
        var newBalance     = currentBalance + req.Delta;

        if (newBalance < 0)
            throw new InvalidOperationException(
                $"Adjustment would result in a negative balance ({newBalance:F2}).");

        var entry = CreditLedger.Adjust(
            package.CorporationId, req.StudentPackageId,
            req.Delta, req.Reason, _currentUser.UserId);

        _db.CreditLedgerEntries.Add(entry);
        await _db.SaveChangesAsync(ct);

        return new CreditLedgerEntryDto(
            entry.Id, entry.StudentPackageId, entry.EntryType,
            entry.Delta, newBalance,
            entry.SessionId, entry.Reason, entry.OccurredAt, entry.CreatedBy);
    }
}
