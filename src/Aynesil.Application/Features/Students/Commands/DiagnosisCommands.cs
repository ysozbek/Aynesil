using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── AddDiagnosisCommand ───────────────────────────────────────────────────────

public record AddDiagnosisCommand(
    Guid StudentId,
    Guid? CategoryId,
    string? IcdCode,
    string? Description,
    DateOnly? DiagnosedOn,
    string? DiagnosedBy,
    Guid? SourceFileId) : IRequest<DiagnosisDto>;

public class AddDiagnosisCommandValidator : AbstractValidator<AddDiagnosisCommand>
{
    public AddDiagnosisCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.IcdCode).MaximumLength(20).When(x => x.IcdCode is not null);
        RuleFor(x => x.DiagnosedBy).MaximumLength(500).When(x => x.DiagnosedBy is not null);
    }
}

public sealed class AddDiagnosisCommandHandler
    : IRequestHandler<AddDiagnosisCommand, DiagnosisDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddDiagnosisCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DiagnosisDto> Handle(AddDiagnosisCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var diagnosis = new Diagnosis
        {
            CorporationId = student.CorporationId,
            StudentId     = req.StudentId,
            CategoryId    = req.CategoryId,
            IcdCode       = req.IcdCode,
            Description   = req.Description,
            DiagnosedOn   = req.DiagnosedOn,
            DiagnosedBy   = req.DiagnosedBy,
            SourceFileId  = req.SourceFileId,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow,
            CreatedBy     = _currentUser.UserId
        };

        _db.Diagnoses.Add(diagnosis);
        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToDiagnosisDto(diagnosis);
    }
}

// ── UpdateDiagnosisCommand ────────────────────────────────────────────────────

public record UpdateDiagnosisCommand(
    Guid Id,
    Guid? CategoryId,
    string? IcdCode,
    string? Description,
    DateOnly? DiagnosedOn,
    string? DiagnosedBy,
    Guid? SourceFileId,
    int RowVersion) : IRequest<DiagnosisDto>;

public sealed class UpdateDiagnosisCommandHandler
    : IRequestHandler<UpdateDiagnosisCommand, DiagnosisDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateDiagnosisCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DiagnosisDto> Handle(UpdateDiagnosisCommand req, CancellationToken ct)
    {
        var diagnosis = await _db.Diagnoses
            .FirstOrDefaultAsync(d => d.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Diagnosis {req.Id} not found.");

        if (diagnosis.RowVersion != req.RowVersion)
            throw new InvalidOperationException("The diagnosis record was modified by another user. Please refresh and retry.");

        diagnosis.CategoryId   = req.CategoryId;
        diagnosis.IcdCode      = req.IcdCode;
        diagnosis.Description  = req.Description;
        diagnosis.DiagnosedOn  = req.DiagnosedOn;
        diagnosis.DiagnosedBy  = req.DiagnosedBy;
        diagnosis.SourceFileId = req.SourceFileId;
        diagnosis.UpdatedAt    = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToDiagnosisDto(diagnosis);
    }
}

// ── DeleteDiagnosisCommand ────────────────────────────────────────────────────

public record DeleteDiagnosisCommand(Guid Id) : IRequest;

public sealed class DeleteDiagnosisCommandHandler : IRequestHandler<DeleteDiagnosisCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteDiagnosisCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteDiagnosisCommand req, CancellationToken ct)
    {
        var diagnosis = await _db.Diagnoses
            .FirstOrDefaultAsync(d => d.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Diagnosis {req.Id} not found.");

        diagnosis.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
