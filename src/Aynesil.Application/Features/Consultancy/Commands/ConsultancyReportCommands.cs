using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Commands;

// ── CreateConsultancyReportCommand ────────────────────────────────────────────

public record CreateConsultancyReportCommand(
    Guid CorporationId,
    string Title,
    Guid? ConsultancyPlanId,
    Guid? SchoolVisitId,
    string? Summary,
    Guid? FileId,
    Guid? AuthoredBy = null) : IRequest<ConsultancyReportDto>;

public class CreateConsultancyReportCommandValidator
    : AbstractValidator<CreateConsultancyReportCommand>
{
    public CreateConsultancyReportCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x)
            .Must(x => x.ConsultancyPlanId.HasValue || x.SchoolVisitId.HasValue)
            .WithMessage("A report must be linked to a consultancy plan, a school visit, or both.");
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Summary) || x.FileId.HasValue)
            .WithMessage("A report must include at least a summary or an attached file.");
    }
}

public sealed class CreateConsultancyReportCommandHandler
    : IRequestHandler<CreateConsultancyReportCommand, ConsultancyReportDto>
{
    private readonly IAppDbContext _db;

    public CreateConsultancyReportCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ConsultancyReportDto> Handle(
        CreateConsultancyReportCommand req, CancellationToken ct)
    {
        if (req.ConsultancyPlanId.HasValue)
        {
            var planExists = await _db.ConsultancyPlans
                .AnyAsync(p => p.Id == req.ConsultancyPlanId.Value, ct);
            if (!planExists)
                throw new KeyNotFoundException(
                    $"Consultancy plan {req.ConsultancyPlanId} not found.");
        }

        if (req.SchoolVisitId.HasValue)
        {
            var visitExists = await _db.SchoolVisits
                .AnyAsync(v => v.Id == req.SchoolVisitId.Value, ct);
            if (!visitExists)
                throw new KeyNotFoundException(
                    $"School visit {req.SchoolVisitId} not found.");
        }

        var report = ConsultancyReport.Create(
            req.CorporationId, req.Title,
            req.ConsultancyPlanId, req.SchoolVisitId,
            req.Summary, req.FileId, req.AuthoredBy);

        _db.ConsultancyReports.Add(report);
        await _db.SaveChangesAsync(ct);

        var planName = req.ConsultancyPlanId.HasValue
            ? await _db.ConsultancyPlans.AsNoTracking()
                .Where(p => p.Id == req.ConsultancyPlanId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        DateOnly? visitDate = req.SchoolVisitId.HasValue
            ? await _db.SchoolVisits.AsNoTracking()
                .Where(v => v.Id == req.SchoolVisitId.Value)
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
