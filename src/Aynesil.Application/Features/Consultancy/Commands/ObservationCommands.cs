using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Commands;

// ── CreateObservationCommand ──────────────────────────────────────────────────

public record CreateObservationCommand(
    Guid CorporationId,
    Guid SchoolVisitId,
    string Observation,
    Guid? ObservationTypeId,
    string? Subject,
    string? Recommendations,
    Guid? CreatedBy = null) : IRequest<ObservationRecordDto>;

public class CreateObservationCommandValidator : AbstractValidator<CreateObservationCommand>
{
    public CreateObservationCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.SchoolVisitId).NotEmpty();
        RuleFor(x => x.Observation).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Subject).MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Subject));
        RuleFor(x => x.Recommendations).MaximumLength(4000)
            .When(x => !string.IsNullOrWhiteSpace(x.Recommendations));
    }
}

public sealed class CreateObservationCommandHandler
    : IRequestHandler<CreateObservationCommand, ObservationRecordDto>
{
    private readonly IAppDbContext _db;

    public CreateObservationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ObservationRecordDto> Handle(
        CreateObservationCommand req, CancellationToken ct)
    {
        var visitExists = await _db.SchoolVisits
            .AnyAsync(v => v.Id == req.SchoolVisitId
                        && v.Status != "cancelled", ct);

        if (!visitExists)
            throw new KeyNotFoundException(
                $"School visit {req.SchoolVisitId} not found or has been cancelled.");

        var record = ObservationRecord.Record(
            req.CorporationId, req.SchoolVisitId,
            req.Observation, req.ObservationTypeId,
            req.Subject, req.Recommendations, req.CreatedBy);

        _db.ObservationRecords.Add(record);
        await _db.SaveChangesAsync(ct);

        var typeCode = req.ObservationTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == req.ObservationTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return new ObservationRecordDto(
            record.Id, record.CorporationId, record.SchoolVisitId,
            record.ObservationTypeId, typeCode,
            record.Subject, record.Observation, record.Recommendations,
            record.CreatedAt, record.CreatedBy);
    }
}

// ── UpdateObservationCommand ──────────────────────────────────────────────────

public record UpdateObservationCommand(
    Guid Id,
    string Observation,
    Guid? ObservationTypeId,
    string? Subject,
    string? Recommendations) : IRequest;

public class UpdateObservationCommandValidator : AbstractValidator<UpdateObservationCommand>
{
    public UpdateObservationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Observation).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Subject).MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Subject));
        RuleFor(x => x.Recommendations).MaximumLength(4000)
            .When(x => !string.IsNullOrWhiteSpace(x.Recommendations));
    }
}

public sealed class UpdateObservationCommandHandler : IRequestHandler<UpdateObservationCommand>
{
    private readonly IAppDbContext _db;

    public UpdateObservationCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateObservationCommand req, CancellationToken ct)
    {
        var record = await _db.ObservationRecords
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Observation {req.Id} not found.");

        record.Update(req.Observation, req.ObservationTypeId, req.Subject, req.Recommendations);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteObservationCommand ──────────────────────────────────────────────────

public record DeleteObservationCommand(Guid Id) : IRequest;

public sealed class DeleteObservationCommandHandler : IRequestHandler<DeleteObservationCommand>
{
    private readonly IAppDbContext _db;

    public DeleteObservationCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteObservationCommand req, CancellationToken ct)
    {
        var record = await _db.ObservationRecords
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Observation {req.Id} not found.");

        _db.ObservationRecords.Remove(record);
        await _db.SaveChangesAsync(ct);
    }
}
