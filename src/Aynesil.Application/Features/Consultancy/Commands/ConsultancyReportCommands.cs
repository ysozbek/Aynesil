using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Commands;

// ── CreateConsultancyReportCommand ────────────────────────────────────────────

public record CreateConsultancyReportCommand(
    string Title,
    Guid? ConsultancyPlanId,
    Guid? SchoolVisitId,
    string? Summary,
    Guid? FileId) : IRequest<ConsultancyReportDto>;

public class CreateConsultancyReportCommandValidator
    : AbstractValidator<CreateConsultancyReportCommand>
{
    public CreateConsultancyReportCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x)
            .Must(x => x.ConsultancyPlanId.HasValue || x.SchoolVisitId.HasValue)
            .WithKey(MsgKeys.Validation.AtLeastOneOf, Msg.FieldList("ConsultancyPlanId", "SchoolVisitId"));
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Summary) || x.FileId.HasValue)
            .WithKey(MsgKeys.Validation.AtLeastOneOf, Msg.FieldList("Summary", "AttachedFile"));
    }
}

public sealed class CreateConsultancyReportCommandHandler
    : IRequestHandler<CreateConsultancyReportCommand, ConsultancyReportDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public CreateConsultancyReportCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<ConsultancyReportDto> Handle(
        CreateConsultancyReportCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        if (req.ConsultancyPlanId.HasValue)
        {
            var planExists = await _db.ConsultancyPlans
                .AnyAsync(p => p.CorporationId == corporationId && p.Id == req.ConsultancyPlanId.Value, ct);
            if (!planExists)
                throw new KeyNotFoundException(
                    $"Consultancy plan {req.ConsultancyPlanId} not found.");
        }

        if (req.SchoolVisitId.HasValue)
        {
            var visitExists = await _db.SchoolVisits
                .AnyAsync(v => v.CorporationId == corporationId && v.Id == req.SchoolVisitId.Value, ct);
            if (!visitExists)
                throw new KeyNotFoundException(
                    $"School visit {req.SchoolVisitId} not found.");
        }

        var report = ConsultancyReport.Create(
            corporationId, req.Title,
            req.ConsultancyPlanId, req.SchoolVisitId,
            req.Summary, req.FileId, _currentUser.UserId);

        _db.ConsultancyReports.Add(report);
        await _db.SaveChangesAsync(ct);

        var planName = req.ConsultancyPlanId.HasValue
            ? await _db.ConsultancyPlans.AsNoTracking()
                .Where(p => p.CorporationId == corporationId && p.Id == req.ConsultancyPlanId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        DateOnly? visitDate = req.SchoolVisitId.HasValue
            ? await _db.SchoolVisits.AsNoTracking()
                .Where(v => v.CorporationId == corporationId && v.Id == req.SchoolVisitId.Value)
                .Select(v => v.VisitDate)
                .FirstOrDefaultAsync(ct)
            : null;

        return new ConsultancyReportDto(
            report.Id, report.CorporationId,
            report.ConsultancyPlanId, planName,
            report.SchoolVisitId, visitDate,
            report.Title, report.Summary,
            report.FileId, report.AuthoredBy, report.CreatedAt);
    }
}

// ── DeleteConsultancyReportCommand ────────────────────────────────────────────

public record DeleteConsultancyReportCommand(Guid Id) : IRequest;

public sealed class DeleteConsultancyReportCommandHandler
    : IRequestHandler<DeleteConsultancyReportCommand>
{
    private readonly IAppDbContext _db;

    public DeleteConsultancyReportCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteConsultancyReportCommand req, CancellationToken ct)
    {
        var report = await _db.ConsultancyReports
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Consultancy report {req.Id} not found.");

        _db.ConsultancyReports.Remove(report);
        await _db.SaveChangesAsync(ct);
    }
}
