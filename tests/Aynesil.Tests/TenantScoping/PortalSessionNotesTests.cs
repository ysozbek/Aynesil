using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Portal.Queries;
using Aynesil.Application.Features.Scheduling.Commands;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Scheduling.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Veli portalı seans geçmişi: yalnızca parent_visible notlar, CanViewSessions kapısı (PortalAccessHelper),
/// düzenleme izi (edited_at/edited_by yalnızca metin değişince) ve görünürlük kapatılınca notun düşmesi.
/// </summary>
public class PortalSessionNotesTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 10, 10, 0, 0, TimeSpan.Zero);

    private sealed record Setup(TenantTestContext Ctx, Session Session, Guid StudentId, Educator Author);

    private static async Task<Setup> ArrangeAsync(bool canViewSessions = true, bool revoked = false)
    {
        var ctx = new TenantTestContext();
        var guardian = Guardian.Create(TenantA, "Ayşe", "Kaya");
        guardian.LinkPortalAccount(ctx.UserId);
        var studentId = Guid.CreateVersion7();
        var access = new GuardianPortalAccess
        {
            CorporationId = TenantA, GuardianId = guardian.Id, StudentId = studentId,
            CanViewSessions = canViewSessions, RevokedAt = revoked ? DateTimeOffset.UtcNow : null,
        };
        var session = Session.Schedule(TenantA, Guid.CreateVersion7(), At, At.AddMinutes(45));
        var participant = SessionParticipant.Create(TenantA, session.Id, studentId);
        var author = Educator.Create(TenantA, "Eğitmen", "Yazan");
        await ctx.SeedAsync(guardian, access, session, participant, author);
        return new Setup(ctx, session, studentId, author);
    }

    private static Task<IReadOnlyList<Aynesil.Application.Features.Portal.Dtos.PortalSessionDto>> HistoryAsync(Setup s)
        => s.Ctx.Handler<GetPortalSessionHistoryQueryHandler>().Handle(
            new GetPortalSessionHistoryQuery(s.StudentId, s.Ctx.UserId), default);

    [Fact]
    public async Task Portal_shows_only_parent_visible_notes()
    {
        var s = await ArrangeAsync();
        using var _ = s.Ctx;
        await s.Ctx.SeedAsync(
            SessionNote.Write(TenantA, s.Session.Id, "Veliye açık", true, s.Author.Id),
            SessionNote.Write(TenantA, s.Session.Id, "Dahili", false, s.Author.Id));

        var sessions = await HistoryAsync(s);

        var notes = sessions.Should().ContainSingle().Which.Notes;
        notes.Should().ContainSingle().Which.Body.Should().Be("Veliye açık");
        notes[0].EditedAt.Should().BeNull();
    }

    [Fact]
    public async Task Portal_hides_soft_deleted_notes()
    {
        var s = await ArrangeAsync();
        using var _ = s.Ctx;
        var note = SessionNote.Write(TenantA, s.Session.Id, "Silinecek", true, s.Author.Id);
        note.SoftDelete(null);
        await s.Ctx.SeedAsync(note);

        (await HistoryAsync(s)).Single().Notes.Should().BeEmpty();
    }

    [Fact]
    public async Task Portal_returns_no_sessions_or_notes_when_CanViewSessions_is_off()
    {
        var s = await ArrangeAsync(canViewSessions: false);
        using var _ = s.Ctx;
        await s.Ctx.SeedAsync(SessionNote.Write(TenantA, s.Session.Id, "Veliye açık", true, s.Author.Id));

        (await HistoryAsync(s)).Should().BeEmpty();
    }

    [Fact]
    public async Task Portal_rejects_a_guardian_with_revoked_access()
    {
        var s = await ArrangeAsync(revoked: true);
        using var _ = s.Ctx;
        await s.Ctx.SeedAsync(SessionNote.Write(TenantA, s.Session.Id, "Veliye açık", true, s.Author.Id));

        var act = () => HistoryAsync(s);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Portal_does_not_show_another_students_session_notes()
    {
        var s = await ArrangeAsync();
        using var _ = s.Ctx;
        var other = Session.Schedule(TenantA, Guid.CreateVersion7(), At, At.AddMinutes(45));
        await s.Ctx.SeedAsync(other, SessionParticipant.Create(TenantA, other.Id, Guid.CreateVersion7()),
            SessionNote.Write(TenantA, other.Id, "Başkasının", true, s.Author.Id));

        (await HistoryAsync(s)).Single().Notes.Should().BeEmpty();
    }

    [Fact]
    public async Task Editing_the_text_sets_edited_trace_and_portal_shows_it()
    {
        var s = await ArrangeAsync();
        using var _ = s.Ctx;
        s.Author.LinkUserAccount(s.Ctx.UserId);
        var note = SessionNote.Write(TenantA, s.Session.Id, "İlk", true, s.Author.Id);
        s.Ctx.Db.Educators.Update(s.Author);
        await s.Ctx.SeedAsync(note);

        await s.Ctx.Handler<EditSessionNoteCommandHandler>().Handle(
            new EditSessionNoteCommand(note.Id, "Düzeltildi", true, note.RowVersion), default);

        var saved = await s.Ctx.Db.SessionNotes.SingleAsync();
        saved.EditedAt.Should().NotBeNull();
        saved.EditedBy.Should().Be(s.Ctx.UserId);
        (await HistoryAsync(s)).Single().Notes.Single().EditedAt.Should().NotBeNull();
    }

    [Fact]
    public void Visibility_only_change_is_not_an_edit_and_closing_visibility_removes_it_from_portal()
    {
        var note = SessionNote.Write(TenantA, Guid.CreateVersion7(), "Aynı", true, null);

        note.Edit("Aynı", false, Guid.CreateVersion7());

        note.EditedAt.Should().BeNull("yalnızca görünürlük değişti");
        note.EditedBy.Should().BeNull();
        note.ParentVisible.Should().BeFalse();
    }

    [Fact]
    public async Task Closing_visibility_later_removes_the_note_from_the_portal()
    {
        var s = await ArrangeAsync();
        using var _ = s.Ctx;
        s.Author.LinkUserAccount(s.Ctx.UserId);
        var note = SessionNote.Write(TenantA, s.Session.Id, "Önce açık", true, s.Author.Id);
        s.Ctx.Db.Educators.Update(s.Author);
        await s.Ctx.SeedAsync(note);
        (await HistoryAsync(s)).Single().Notes.Should().HaveCount(1);

        await s.Ctx.Handler<EditSessionNoteCommandHandler>().Handle(
            new EditSessionNoteCommand(note.Id, "Önce açık", false, note.RowVersion), default);

        (await HistoryAsync(s)).Single().Notes.Should().BeEmpty();
    }
}
