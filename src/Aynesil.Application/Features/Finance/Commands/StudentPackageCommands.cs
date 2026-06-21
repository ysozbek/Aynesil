using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Commands;

// ── PurchaseStudentPackageCommand ─────────────────────────────────────────────

public record PurchaseStudentPackageCommand(
    Guid CorporationId,
    Guid StudentId,
    decimal TotalCredits,
    decimal Price,
    Guid? PackageDefinitionId,
    DateOnly? ExpiresOn,
    string Currency = "TRY") : IRequest<StudentPackageDto>;

public class PurchaseStudentPackageCommandValidator
    : AbstractValidator<PurchaseStudentPackageCommand>
{
    public PurchaseStudentPackageCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.TotalCredits).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public sealed class PurchaseStudentPackageCommandHandler
    : IRequestHandler<PurchaseStudentPackageCommand, StudentPackageDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PurchaseStudentPackageCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<StudentPackageDto> Handle(
        PurchaseStudentPackageCommand req, CancellationToken ct)
    {
        var studentExists = await _db.Students
            .AnyAsync(s => s.Id == req.StudentId, ct);

        if (!studentExists)
            throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        if (req.PackageDefinitionId.HasValue)
        {
            var def = await _db.PackageDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == req.PackageDefinitionId, ct)
                ?? throw new KeyNotFoundException(
                    $"Package definition {req.PackageDefinitionId} not found.");

            if (!def.IsActive)
                throw new InvalidOperationException(
                    $"Package definition '{def.Name}' is inactive and cannot be purchased.");
        }

        var package = StudentPackage.Purchase(
            req.CorporationId, req.StudentId,
            req.TotalCredits, req.Price,
            req.PackageDefinitionId, req.ExpiresOn,
            req.Currency, _currentUser.UserId);

        // Grant initial credits via the ledger
        var grant = package.GrantCredits(
            req.TotalCredits, "Initial purchase credit grant", _currentUser.UserId);

        _db.StudentPackages.Add(package);
        _db.CreditLedgerEntries.Add(grant);

        await _db.SaveChangesAsync(ct);

        return (await FinanceProjection.LoadStudentPackageAsync(_db, package.Id, ct))!;
    }
}

// ── CancelStudentPackageCommand ───────────────────────────────────────────────

public record CancelStudentPackageCommand(Guid Id, int RowVersion) : IRequest<StudentPackageDto>;

public sealed class CancelStudentPackageCommandHandler
    : IRequestHandler<CancelStudentPackageCommand, StudentPackageDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelStudentPackageCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<StudentPackageDto> Handle(
        CancelStudentPackageCommand req, CancellationToken ct)
    {
        var package = await _db.StudentPackages
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student package {req.Id} not found.");

        if (package.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "Package was modified by another user. Please refresh and retry.");

        package.Cancel(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        return (await FinanceProjection.LoadStudentPackageAsync(_db, package.Id, ct))!;
    }
}

// ── ExpireStudentPackageCommand ───────────────────────────────────────────────
// Used by background jobs scanning packages past their expiry date.

public record ExpireStudentPackageCommand(Guid Id) : IRequest;

public sealed class ExpireStudentPackageCommandHandler
    : IRequestHandler<ExpireStudentPackageCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ExpireStudentPackageCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ExpireStudentPackageCommand req, CancellationToken ct)
    {
        var package = await _db.StudentPackages
            .Include(p => p.CreditLedgerEntries)
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student package {req.Id} not found.");

        var remaining = package.CreditLedgerEntries.Sum(e => e.Delta);

        package.MarkExpired();

        // Write an expire ledger entry for any remaining unused credits
        if (remaining > 0)
        {
            var expiry = package.ExpireCredits(
                remaining, "Package expired with unused credits", _currentUser.UserId);
            _db.CreditLedgerEntries.Add(expiry);
        }

        await _db.SaveChangesAsync(ct);
    }
}
