using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Commands;

// ── CreateObservationCommand ──────────────────────────────────────────────────

/// <summary>
/// CorporationId and CreatedBy are taken from the request context (JWT), never from the client.
/// </summary>
public record CreateObservationCommand(
    Guid SchoolVisitId,
    string Observation,
    Guid? ObservationTypeId,
    string? Subject,
    string? Recommendations) : IRequest<ObservationRecordDto>;

public class CreateObservationCommandValidator : AbstractValidator<CreateObservationCommand>
{
    public CreateObservationCommandValidator()
    {
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
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public CreateObservationCommandHandler(
        IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<ObservationRecordDto> Handle(
        CreateObservationCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var visitExists = await _db.SchoolVisits
            .AnyAsync(v => v.Id == req.SchoolVisitId
                        && v.CorporationId == corporationId
                        && v.Status != "cancelled", ct);

        if (!visitExists)
            throw new NotFoundException(MsgKeys.Rule.VisitNotFoundOrCancelled, req.SchoolVisitId);

        var record = ObservationRecord.Record(
            corporationId, req.SchoolVisitId,
            req.Observation, req.ObservationTypeId,
            req.Subject, req.Recommendations, _currentUser.UserId);

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
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Observation"));

        // Tamamlanmış ziyarette düzenlenebilir; iptal edilmiş ya da rapor yazılmış ziyarette kapalı.
        var visitStatus = await ObservationRules.EnsureNotLockedAsync(_db, record.SchoolVisitId, ct);
        if (visitStatus == "cancelled")
            throw new BusinessRuleViolationException(MsgKeys.Rule.ObservationVisitCancelled);

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
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Observation"));

        // Silme yalnızca planlanmış ziyarette; tamamlanmış/iptal edilmiş ziyarette 409.
        var visitStatus = await ObservationRules.EnsureNotLockedAsync(_db, record.SchoolVisitId, ct);
        if (visitStatus != "planned")
            throw new BusinessRuleViolationException(MsgKeys.Rule.ObservationDeleteNeedsPlannedVisit);

        _db.ObservationRecords.Remove(record);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Gözlem değiştirme/silme için ziyaret düzeyi kuralları.</summary>
internal static class ObservationRules
{
    /// <summary>Ziyarete rapor yazılmışsa 409 atar; aksi halde ziyaretin durumunu döner.</summary>
    public static async Task<string?> EnsureNotLockedAsync(IAppDbContext db, Guid visitId, CancellationToken ct)
    {
        if (await db.ConsultancyReports.AnyAsync(r => r.SchoolVisitId == visitId, ct))
            throw new BusinessRuleViolationException(MsgKeys.Rule.ObservationVisitHasReport);

        return await db.SchoolVisits.AsNoTracking()
            .Where(v => v.Id == visitId)
            .Select(v => v.Status)
            .FirstOrDefaultAsync(ct);
    }
}
