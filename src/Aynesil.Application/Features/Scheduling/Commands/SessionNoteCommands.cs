using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Educators;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Validation;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Domain.Modules.Scheduling.Entities;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Commands;

// ── WriteSessionNoteCommand ───────────────────────────────────────────────────

public record WriteSessionNoteCommand(
    Guid SessionId,
    string Body,
    bool ParentVisible,
    Guid? StudentId = null) : IRequest<SessionNoteDto>;

/// <summary>
/// Notun bağlandığı öğrenciyi çözer (V56). Kurallar:
///  • verilen öğrenci seansın katılımcısı olmalı;
///  • verilmezse: tek katılımcılı seansta o öğrenci otomatik seçilir;
///  • veliye görünür not bir öğrenciye bağlanmak zorundadır (grup seansında seçim zorunlu, katılımcısız seansta yapılamaz).
/// </summary>
internal static class SessionNoteStudent
{
    public static async Task<Guid?> ResolveAsync(
        IAppDbContext db, Guid sessionId, Guid? requested, bool parentVisible, CancellationToken ct)
    {
        var participants = await db.SessionParticipants.AsNoTracking()
            .Where(p => p.SessionId == sessionId).Select(p => p.StudentId).ToListAsync(ct);

        if (requested.HasValue)
        {
            if (!participants.Contains(requested.Value))
                throw new ValidationException([ValidationFailures.For("studentId", MsgKeys.Rule.SessionNoteStudentNotParticipant)]);
            return requested;
        }

        if (participants.Count == 1) return participants[0];

        if (parentVisible)
            throw new ValidationException([ValidationFailures.For("studentId", MsgKeys.Rule.SessionNoteStudentRequired)]);
        return null;
    }
}

public class WriteSessionNoteCommandValidator : AbstractValidator<WriteSessionNoteCommand>
{
    public WriteSessionNoteCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10_000);
    }
}

public sealed class WriteSessionNoteCommandHandler : IRequestHandler<WriteSessionNoteCommand, SessionNoteDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public WriteSessionNoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    internal static async Task<string?> StudentNameAsync(IAppDbContext db, Guid? studentId, CancellationToken ct)
    {
        if (!studentId.HasValue) return null;
        var n = await db.Students.AsNoTracking().Where(s => s.Id == studentId.Value)
            .Select(s => new { s.FirstName, s.LastName }).FirstOrDefaultAsync(ct);
        return n is null ? null : $"{n.FirstName} {n.LastName}".Trim();
    }

    public async Task<SessionNoteDto> Handle(WriteSessionNoteCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Session"));

        // authored_by FK → educators.educator: the author is the educator linked to the signed-in user.
        var educatorId = await ActingEducator.RequireIdAsync(
            _db, _tenant.RequireCorporationId(), _currentUser.UserId, ct);
        var educators = await ActingEducator.LoadNamesAsync(_db, [educatorId], ct);

        var studentId = await SessionNoteStudent.ResolveAsync(_db, req.SessionId, req.StudentId, req.ParentVisible, ct);

        var note = SessionNote.Write(
            session.CorporationId, req.SessionId,
            req.Body, req.ParentVisible,
            educatorId, studentId);

        _db.SessionNotes.Add(note);
        await _db.SaveChangesAsync(ct);

        return new SessionNoteDto(
            note.Id, req.SessionId, note.AuthoredBy,
            note.Body, note.ParentVisible,
            note.CreatedAt, note.UpdatedAt, note.RowVersion,
            educators.GetValueOrDefault(educatorId), null,
            note.StudentId, await StudentNameAsync(_db, note.StudentId, ct));
    }
}

// ── EditSessionNoteCommand ────────────────────────────────────────────────────

public record EditSessionNoteCommand(
    Guid Id,
    string Body,
    bool ParentVisible,
    int RowVersion,
    Guid? StudentId = null) : IRequest<SessionNoteDto>;

public class EditSessionNoteCommandValidator : AbstractValidator<EditSessionNoteCommand>
{
    public EditSessionNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10_000);
    }
}

public sealed class EditSessionNoteCommandHandler : IRequestHandler<EditSessionNoteCommand, SessionNoteDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public EditSessionNoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task<SessionNoteDto> Handle(EditSessionNoteCommand req, CancellationToken ct)
    {
        var note = await _db.SessionNotes.FirstOrDefaultAsync(n => n.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "SessionNote"));

        // Yalnızca notu yazan eğitmen düzenler; başkasının notunu session_note:delete (yönetici) yetkisi olan düzenleyebilir.
        var actingEducatorId = await ActingEducator.FindIdAsync(
            _db, _tenant.RequireCorporationId(), _currentUser.UserId, ct);
        var isAuthor = actingEducatorId.HasValue && note.AuthoredBy == actingEducatorId;
        if (!isAuthor && !_currentUser.HasPermission(Aynesil.Shared.Constants.Permissions.SessionNotes.Delete))
            throw new ForbiddenAccessException(MsgKeys.Rule.SessionNoteAuthorOnly);

        if (note.RowVersion != req.RowVersion)
            throw new BusinessRuleViolationException(MsgKeys.Error.Concurrency);

        // Öğrenci verilmezse mevcut bağ korunur; katılımcılıktan çıkmış öğrenciye bağlı not yeniden doğrulanır.
        Guid? requested = req.StudentId;
        if (!requested.HasValue && note.StudentId.HasValue
            && await _db.SessionParticipants.AnyAsync(p => p.SessionId == note.SessionId && p.StudentId == note.StudentId.Value, ct))
            requested = note.StudentId;
        var studentId = await SessionNoteStudent.ResolveAsync(_db, note.SessionId, requested, req.ParentVisible, ct);

        note.Edit(req.Body, req.ParentVisible, studentId, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        // Düzenleyen değil, notu yazan eğitmenin adı (authored_by → educators.educator).
        var authors = await ActingEducator.LoadNamesAsync(
            _db, note.AuthoredBy.HasValue ? [note.AuthoredBy.Value] : [], ct);

        return new SessionNoteDto(
            note.Id, note.SessionId, note.AuthoredBy,
            note.Body, note.ParentVisible,
            note.CreatedAt, note.UpdatedAt, note.RowVersion,
            note.AuthoredBy.HasValue ? authors.GetValueOrDefault(note.AuthoredBy.Value) : null,
            note.EditedAt, note.StudentId, await WriteSessionNoteCommandHandler.StudentNameAsync(_db, note.StudentId, ct));
    }
}

// ── DeleteSessionNoteCommand ──────────────────────────────────────────────────

public record DeleteSessionNoteCommand(Guid Id) : IRequest;

public sealed class DeleteSessionNoteCommandHandler : IRequestHandler<DeleteSessionNoteCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteSessionNoteCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteSessionNoteCommand req, CancellationToken ct)
    {
        var note = await _db.SessionNotes.FirstOrDefaultAsync(n => n.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "SessionNote"));

        note.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
