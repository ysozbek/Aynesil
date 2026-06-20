using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using Aynesil.Domain.Modules.Students.Events;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── AddCaseNoteCommand ────────────────────────────────────────────────────────

public record AddCaseNoteCommand(
    Guid StudentId,
    string? NoteType,
    string Body,
    bool IsConfidential,
    Guid? AuthoredBy) : IRequest<CaseNoteDto>;

public class AddCaseNoteCommandValidator : AbstractValidator<AddCaseNoteCommand>
{
    public AddCaseNoteCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
    }
}

public sealed class AddCaseNoteCommandHandler
    : IRequestHandler<AddCaseNoteCommand, CaseNoteDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddCaseNoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CaseNoteDto> Handle(AddCaseNoteCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var note = new CaseNote
        {
            CorporationId  = student.CorporationId,
            StudentId      = req.StudentId,
            NoteType       = req.NoteType,
            Body           = req.Body,
            IsConfidential = req.IsConfidential,
            AuthoredBy     = req.AuthoredBy ?? _currentUser.UserId,
            CreatedAt      = DateTimeOffset.UtcNow,
            UpdatedAt      = DateTimeOffset.UtcNow
        };

        note.AddDomainEvent(new CaseNoteAddedEvent(
            note.Id, req.StudentId, student.CorporationId,
            req.IsConfidential, note.AuthoredBy));

        _db.CaseNotes.Add(note);
        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToCaseNoteDto(note);
    }
}

// ── UpdateCaseNoteCommand ─────────────────────────────────────────────────────

public record UpdateCaseNoteCommand(
    Guid Id,
    string? NoteType,
    string Body,
    bool IsConfidential,
    int RowVersion) : IRequest<CaseNoteDto>;

public class UpdateCaseNoteCommandValidator : AbstractValidator<UpdateCaseNoteCommand>
{
    public UpdateCaseNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
    }
}

public sealed class UpdateCaseNoteCommandHandler
    : IRequestHandler<UpdateCaseNoteCommand, CaseNoteDto>
{
    private readonly IAppDbContext _db;

    public UpdateCaseNoteCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CaseNoteDto> Handle(UpdateCaseNoteCommand req, CancellationToken ct)
    {
        var note = await _db.CaseNotes
            .FirstOrDefaultAsync(n => n.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Case note {req.Id} not found.");

        if (note.RowVersion != req.RowVersion)
            throw new InvalidOperationException("The case note was modified by another user. Please refresh and retry.");

        note.NoteType       = req.NoteType;
        note.Body           = req.Body;
        note.IsConfidential = req.IsConfidential;
        note.UpdatedAt      = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToCaseNoteDto(note);
    }
}

// ── DeleteCaseNoteCommand ─────────────────────────────────────────────────────

public record DeleteCaseNoteCommand(Guid Id) : IRequest;

public sealed class DeleteCaseNoteCommandHandler : IRequestHandler<DeleteCaseNoteCommand>
{
    private readonly IAppDbContext _db;

    public DeleteCaseNoteCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCaseNoteCommand req, CancellationToken ct)
    {
        var note = await _db.CaseNotes
            .FirstOrDefaultAsync(n => n.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Case note {req.Id} not found.");

        note.SoftDelete();
        await _db.SaveChangesAsync(ct);
    }
}
