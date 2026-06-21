using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Plans.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Plans.Commands;

// ── CreateAcademicPeriodCommand ───────────────────────────────────────────────

public record CreateAcademicPeriodCommand(
    Guid CorporationId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? TermId,
    bool IsCurrent) : IRequest<AcademicPeriodDto>;

public class CreateAcademicPeriodCommandValidator : AbstractValidator<CreateAcademicPeriodCommand>
{
    public CreateAcademicPeriodCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty().GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be after StartDate.");
    }
}

public sealed class CreateAcademicPeriodCommandHandler
    : IRequestHandler<CreateAcademicPeriodCommand, AcademicPeriodDto>
{
    private readonly IAppDbContext _db;

    public CreateAcademicPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task<AcademicPeriodDto> Handle(CreateAcademicPeriodCommand req, CancellationToken ct)
    {
        if (req.IsCurrent)
            await UnmarkCurrentAsync(req.CorporationId, ct);

        var period = AcademicPeriod.Create(
            req.CorporationId, req.Name, req.StartDate, req.EndDate, req.TermId, req.IsCurrent);

        _db.AcademicPeriods.Add(period);
        await _db.SaveChangesAsync(ct);

        return await ToDto(period, ct);
    }

    private async Task UnmarkCurrentAsync(Guid corporationId, CancellationToken ct)
    {
        var current = await _db.AcademicPeriods
            .Where(p => p.CorporationId == corporationId && p.IsCurrent)
            .ToListAsync(ct);

        foreach (var p in current)
            p.UnmarkAsCurrent();
    }

    private async Task<AcademicPeriodDto> ToDto(AcademicPeriod p, CancellationToken ct)
    {
        var termLabel = p.TermId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == p.TermId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        return new AcademicPeriodDto(
            p.Id, p.CorporationId, p.Name, p.TermId, termLabel,
            p.StartDate, p.EndDate, p.IsCurrent,
            p.CreatedAt, p.UpdatedAt, p.RowVersion);
    }
}

// ── UpdateAcademicPeriodCommand ───────────────────────────────────────────────

public record UpdateAcademicPeriodCommand(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? TermId,
    int RowVersion) : IRequest<AcademicPeriodDto>;

public class UpdateAcademicPeriodCommandValidator : AbstractValidator<UpdateAcademicPeriodCommand>
{
    public UpdateAcademicPeriodCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be after StartDate.");
    }
}

public sealed class UpdateAcademicPeriodCommandHandler
    : IRequestHandler<UpdateAcademicPeriodCommand, AcademicPeriodDto>
{
    private readonly IAppDbContext _db;

    public UpdateAcademicPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task<AcademicPeriodDto> Handle(UpdateAcademicPeriodCommand req, CancellationToken ct)
    {
        var period = await _db.AcademicPeriods.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"AcademicPeriod {req.Id} not found.");

        if (period.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "The academic period was modified by another user. Please refresh and retry.");

        period.Update(req.Name, req.StartDate, req.EndDate, req.TermId);
        await _db.SaveChangesAsync(ct);

        var termLabel = req.TermId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == req.TermId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        return new AcademicPeriodDto(
            period.Id, period.CorporationId, period.Name, period.TermId, termLabel,
            period.StartDate, period.EndDate, period.IsCurrent,
            period.CreatedAt, period.UpdatedAt, period.RowVersion);
    }
}

// ── SetCurrentAcademicPeriodCommand ───────────────────────────────────────────

public record SetCurrentAcademicPeriodCommand(Guid Id) : IRequest<AcademicPeriodDto>;

public sealed class SetCurrentAcademicPeriodCommandHandler
    : IRequestHandler<SetCurrentAcademicPeriodCommand, AcademicPeriodDto>
{
    private readonly IAppDbContext _db;

    public SetCurrentAcademicPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task<AcademicPeriodDto> Handle(
        SetCurrentAcademicPeriodCommand req, CancellationToken ct)
    {
        var period = await _db.AcademicPeriods.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"AcademicPeriod {req.Id} not found.");

        var existing = await _db.AcademicPeriods
            .Where(p => p.CorporationId == period.CorporationId && p.IsCurrent && p.Id != req.Id)
            .ToListAsync(ct);

        foreach (var p in existing)
            p.UnmarkAsCurrent();

        period.MarkAsCurrent();
        await _db.SaveChangesAsync(ct);

        var termLabel = period.TermId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == period.TermId.Value).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        return new AcademicPeriodDto(
            period.Id, period.CorporationId, period.Name, period.TermId, termLabel,
            period.StartDate, period.EndDate, period.IsCurrent,
            period.CreatedAt, period.UpdatedAt, period.RowVersion);
    }
}

// ── DeleteAcademicPeriodCommand ───────────────────────────────────────────────

public record DeleteAcademicPeriodCommand(Guid Id) : IRequest;

public sealed class DeleteAcademicPeriodCommandHandler : IRequestHandler<DeleteAcademicPeriodCommand>
{
    private readonly IAppDbContext _db;

    public DeleteAcademicPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteAcademicPeriodCommand req, CancellationToken ct)
    {
        var period = await _db.AcademicPeriods.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"AcademicPeriod {req.Id} not found.");

        var hasPlan = await _db.EducationPlans.AnyAsync(
            p => p.AcademicPeriodId == req.Id, ct);

        if (hasPlan)
            throw new InvalidOperationException(
                "Cannot delete an academic period that has education plans assigned to it.");

        _db.AcademicPeriods.Remove(period);
        await _db.SaveChangesAsync(ct);
    }
}
