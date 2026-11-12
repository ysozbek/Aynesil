using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Educators;
using Aynesil.Application.Common.Interfaces;
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
    bool ParentVisible) : IRequest<SessionNoteDto>;

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

    public async Task<SessionNoteDto> Handle(WriteSessionNoteCommand req, CancellationToken ct)
    {
        var session = await _db.Sessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.SessionId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Session"));

        // authored_by FK → educators.educator: the author is the educator linked to the signed-in user.
        var educatorId = await ActingEducator.RequireIdAsync(
            _db, _tenant.RequireCorporationId(), _currentUser.UserId, ct);
        var educators = await ActingEducator.LoadNamesAsync(_db, [educatorId], ct);

        var note = SessionNote.Write(
            session.CorporationId, req.SessionId,
            req.Body, req.ParentVisible,
            educatorId);

        _db.SessionNotes.Add(note);
        await _db.SaveChangesAsync(ct);

        return new SessionNoteDto(
            note.Id, req.SessionId, note.AuthoredBy,
            note.Body, note.ParentVisible,
            note.CreatedAt, note.UpdatedAt, note.RowVersion,
            educators.GetValueOrDefault(educatorId));
    }
}

// ── EditSessionNoteCommand ────────────────────────────────────────────────────

public record EditSessionNoteCommand(
    Guid Id,
    string Body,
    bool ParentVisible,
    int RowVersion) : IRequest<SessionNoteDto>;

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

    public EditSessionNoteCommandHandler(IAppDbContext db) => _db = db;

    public async Task<SessionNoteDto> Handle(EditSessionNoteCommand req, CancellationToken ct)
    {
        var note = await _db.SessionNotes.FirstOrDefaultAsync(n => n.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "SessionNote"));

        if (note.RowVersion != req.RowVersion)
            throw new BusinessRuleViolationException(MsgKeys.Error.Concurrency);

        note.Edit(req.Body, req.ParentVisible);
        await _db.SaveChangesAsync(ct);

        // Düzenleyen değil, notu yazan eğitmenin adı (authored_by → educators.educator).
        var authors = await ActingEducator.LoadNamesAsync(
            _db, note.AuthoredBy.HasValue ? [note.AuthoredBy.Value] : [], ct);

        return new SessionNoteDto(
            note.Id, note.SessionId, note.AuthoredBy,
            note.Body, note.ParentVisible,
            note.CreatedAt, note.UpdatedAt, note.RowVersion,
            note.AuthoredBy.HasValue ? authors.GetValueOrDefault(note.AuthoredBy.Value) : null);
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
