using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Camps.Dtos;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Camps.Commands;

// ── CreateCampPeriodCommand ───────────────────────────────────────────────────

public record CreateCampPeriodCommand(
    Guid CampId,
    Guid CorporationId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int? Capacity) : IRequest<CampPeriodDto>;

public class CreateCampPeriodCommandValidator : AbstractValidator<CreateCampPeriodCommand>
{
    public CreateCampPeriodCommandValidator()
    {
        RuleFor(x => x.CampId).NotEmpty();
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate)
            .Must((cmd, end) => end >= cmd.StartDate)
            .WithMessage("End date must be on or after start date.");
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class CreateCampPeriodCommandHandler
    : IRequestHandler<CreateCampPeriodCommand, CampPeriodDto>
{
    private readonly IAppDbContext _db;

    public CreateCampPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CampPeriodDto> Handle(CreateCampPeriodCommand req, CancellationToken ct)
    {
        var campExists = await _db.Camps
            .AnyAsync(c => c.Id == req.CampId
                        && c.CorporationId == req.CorporationId
                        && c.DeletedAt == null, ct);

        if (!campExists)
            throw new KeyNotFoundException($"Camp {req.CampId} not found.");

        var period = CampPeriod.Create(
            req.CorporationId, req.CampId,
            req.Name, req.StartDate, req.EndDate, req.Capacity);

        _db.CampPeriods.Add(period);
        await _db.SaveChangesAsync(ct);

        return new CampPeriodDto(
            period.Id, period.CampId, period.CorporationId,
            period.Name, period.StartDate, period.EndDate,
            period.Capacity, 0, 0);
    }
}

// ── UpdateCampPeriodCommand ───────────────────────────────────────────────────

public record UpdateCampPeriodCommand(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    int? Capacity) : IRequest;

public class UpdateCampPeriodCommandValidator : AbstractValidator<UpdateCampPeriodCommand>
{
    public UpdateCampPeriodCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate)
            .Must((cmd, end) => end >= cmd.StartDate)
            .WithMessage("End date must be on or after start date.");
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class UpdateCampPeriodCommandHandler : IRequestHandler<UpdateCampPeriodCommand>
{
    private readonly IAppDbContext _db;

    public UpdateCampPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCampPeriodCommand req, CancellationToken ct)
    {
        var period = await _db.CampPeriods
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camp period {req.Id} not found.");

        period.Update(req.Name, req.StartDate, req.EndDate, req.Capacity);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteCampPeriodCommand ───────────────────────────────────────────────────

public record DeleteCampPeriodCommand(Guid Id) : IRequest;

public sealed class DeleteCampPeriodCommandHandler : IRequestHandler<DeleteCampPeriodCommand>
{
    private readonly IAppDbContext _db;

    public DeleteCampPeriodCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCampPeriodCommand req, CancellationToken ct)
    {
        var period = await _db.CampPeriods
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Camp period {req.Id} not found.");

        var hasEnrollments = await _db.CampEnrollments
            .AnyAsync(e => e.CampPeriodId == req.Id, ct);

        if (hasEnrollments)
            throw new InvalidOperationException(
                "Cannot delete a camp period that has enrollments.");

        _db.CampPeriods.Remove(period);
        await _db.SaveChangesAsync(ct);
    }
}
