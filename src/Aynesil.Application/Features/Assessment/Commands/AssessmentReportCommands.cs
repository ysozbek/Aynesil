using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Domain.Modules.Assessment.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Commands;

// ── Create report ─────────────────────────────────────────────────────────────

public record CreateAssessmentReportCommand(
    Guid CorporationId,
    Guid AssessmentSessionId,
    string? Summary,
    string? Findings,
    Guid? FileId) : IRequest<AssessmentReportDto>;

public class CreateAssessmentReportCommandValidator
    : AbstractValidator<CreateAssessmentReportCommand>
{
    public CreateAssessmentReportCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.AssessmentSessionId).NotEmpty();
    }
}

public sealed class CreateAssessmentReportCommandHandler
    : IRequestHandler<CreateAssessmentReportCommand, AssessmentReportDto>
{
    private readonly IAppDbContext _db;

    public CreateAssessmentReportCommandHandler(IAppDbContext db) => _db = db;

    public async Task<AssessmentReportDto> Handle(
        CreateAssessmentReportCommand req, CancellationToken ct)
    {
        var session = await _db.AssessmentSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.AssessmentSessionId, ct)
            ?? throw new KeyNotFoundException(
                $"Assessment session {req.AssessmentSessionId} not found.");

        if (session.Status != AssessmentSession.SessionStatuses.Completed)
            throw new InvalidOperationException(
                "Reports can only be created for completed assessment sessions.");

        var report = AssessmentReport.Create(
            req.CorporationId, req.AssessmentSessionId,
            req.Summary, req.Findings, req.FileId);

        _db.AssessmentReports.Add(report);
        await _db.SaveChangesAsync(ct);

        return AssessmentProjection.ToReportDto(report);
    }
}

// ── Update report (draft only) ────────────────────────────────────────────────

public record UpdateAssessmentReportCommand(
    Guid Id,
    string? Summary,
    string? Findings,
    Guid? FileId,
    int RowVersion) : IRequest<AssessmentReportDto>;

public class UpdateAssessmentReportCommandValidator
    : AbstractValidator<UpdateAssessmentReportCommand>
{
    public UpdateAssessmentReportCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

public sealed class UpdateAssessmentReportCommandHandler
    : IRequestHandler<UpdateAssessmentReportCommand, AssessmentReportDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateAssessmentReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentReportDto> Handle(
        UpdateAssessmentReportCommand req, CancellationToken ct)
    {
        var report = await _db.AssessmentReports
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Assessment report {req.Id} not found.");

        report.Update(req.Summary, req.Findings, req.FileId, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        return AssessmentProjection.ToReportDto(report);
    }
}

// ── Finalize report (immutable lock) ─────────────────────────────────────────

public record FinalizeAssessmentReportCommand(Guid Id, int RowVersion) : IRequest<AssessmentReportDto>;

public sealed class FinalizeAssessmentReportCommandHandler
    : IRequestHandler<FinalizeAssessmentReportCommand, AssessmentReportDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FinalizeAssessmentReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentReportDto> Handle(
        FinalizeAssessmentReportCommand req, CancellationToken ct)
    {
        var report = await _db.AssessmentReports
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Assessment report {req.Id} not found.");

        if (_currentUser.UserId is null)
            throw new InvalidOperationException("Authenticated user required to finalize a report.");

        report.Finalize(_currentUser.UserId.Value);
        await _db.SaveChangesAsync(ct);

        return AssessmentProjection.ToReportDto(report);
    }
}
