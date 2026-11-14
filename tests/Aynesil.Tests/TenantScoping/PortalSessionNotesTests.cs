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
            SessionNote.Write(TenantA, s.Session.Id, "Veliye açık", true, s.Author.Id, s.StudentId),
            SessionNote.Write(TenantA, s.Session.Id, "Dahili", false, s.Author.Id, s.StudentId));

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
        var note = SessionNote.Write(TenantA, s.Session.Id, "Silinecek", true, s.Author.Id, s.StudentId);
        note.SoftDelete(null);
        await s.Ctx.SeedAsync(note);

        (await HistoryAsync(s)).Single().Notes.Should().BeEmpty();
    }

    [Fact]
    public async Task Portal_returns_no_sessions_or_notes_when_CanViewSessions_is_off()
    {
        var s = await ArrangeAsync(canViewSessions: false);
        using var _ = s.Ctx;
        await s.Ctx.SeedAsync(SessionNote.Write(TenantA, s.Session.Id, "Veliye açık", true, s.Author.Id, s.StudentId));

        (await HistoryAsync(s)).Should().BeEmpty();
    }

    [Fact]
    public async Task Portal_rejects_a_guardian_with_revoked_access()
    {
        var s = await ArrangeAsync(revoked: true);
        using var _ = s.Ctx;
        await s.Ctx.SeedAsync(SessionNote.Write(TenantA, s.Session.Id, "Veliye açık", true, s.Author.Id, s.StudentId));

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
            SessionNote.Write(TenantA, other.Id, "Başkasının", true, s.Author.Id, Guid.CreateVersion7()));

        (await HistoryAsync(s)).Single().Notes.Should().BeEmpty();
    }

    [Fact]
    public async Task Editing_the_text_sets_edited_trace_and_portal_shows_it()
    {
        var s = await ArrangeAsync();
        using var _ = s.Ctx;
        s.Author.LinkUserAccount(s.Ctx.UserId);
        var note = SessionNote.Write(TenantA, s.Session.Id, "İlk", true, s.Author.Id, s.StudentId);
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

        note.Edit("Aynı", false, null, Guid.CreateVersion7());

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
        var note = SessionNote.Write(TenantA, s.Session.Id, "Önce açık", true, s.Author.Id, s.StudentId);
        s.Ctx.Db.Educators.Update(s.Author);
        await s.Ctx.SeedAsync(note);
        (await HistoryAsync(s)).Single().Notes.Should().HaveCount(1);

        await s.Ctx.Handler<EditSessionNoteCommandHandler>().Handle(
            new EditSessionNoteCommand(note.Id, "Önce açık", false, note.RowVersion), default);

        (await HistoryAsync(s)).Single().Notes.Should().BeEmpty();
    }

    // ── Grup seansı (V56): öğrenciye bağlı not ────────────────────────────────

    /// <summary>İkinci öğrenci (B) ve onun velisi (başka kullanıcı) ile aynı seansa katılır; çağıran A'nın velisidir.</summary>
    private static async Task<(Setup A, Guid StudentB, Guid GuardianBUser)> GroupAsync()
    {
        var a = await ArrangeAsync();
        var studentB = Guid.CreateVersion7();
        var guardianBUser = Guid.CreateVersion7();
        var guardianB = Guardian.Create(TenantA, "Veli", "B");
        guardianB.LinkPortalAccount(guardianBUser);
        await a.Ctx.SeedAsync(
            SessionParticipant.Create(TenantA, a.Session.Id, studentB), guardianB,
            new GuardianPortalAccess { CorporationId = TenantA, GuardianId = guardianB.Id, StudentId = studentB });
        return (a, studentB, guardianBUser);
    }

    [Fact]
    public async Task Group_session_note_written_for_B_is_not_visible_to_As_guardian_but_is_to_Bs()
    {
        var (a, studentB, guardianBUser) = await GroupAsync();
        using var _ = a.Ctx;
        await a.Ctx.SeedAsync(
            SessionNote.Write(TenantA, a.Session.Id, "A için", true, a.Author.Id, a.StudentId),
            SessionNote.Write(TenantA, a.Session.Id, "B için", true, a.Author.Id, studentB));

        var forA = (await HistoryAsync(a)).Single().Notes;
        forA.Select(n => n.Body).Should().Equal("A için");

        var forB = await a.Ctx.Handler<GetPortalSessionHistoryQueryHandler>().Handle(
            new GetPortalSessionHistoryQuery(studentB, guardianBUser), default);
        forB.Single().Notes.Select(n => n.Body).Should().Equal("B için");
    }

    [Fact]
    public async Task Studentless_parent_visible_note_in_a_group_session_is_shown_to_nobody()
    {
        var (a, studentB, guardianBUser) = await GroupAsync();
        using var _ = a.Ctx;
        await a.Ctx.SeedAsync(SessionNote.Write(TenantA, a.Session.Id, "Sahipsiz grup notu", true, a.Author.Id, null));

        (await HistoryAsync(a)).Single().Notes.Should().BeEmpty();
        (await a.Ctx.Handler<GetPortalSessionHistoryQueryHandler>().Handle(
            new GetPortalSessionHistoryQuery(studentB, guardianBUser), default)).Single().Notes.Should().BeEmpty();
    }

    // ── Yazma kuralları ───────────────────────────────────────────────────────

    private static async Task<(TenantTestContext Ctx, Session Session, Guid A, Guid B)> WriterAsync(int participants)
    {
        var ctx = new TenantTestContext();
        var educator = Educator.Create(TenantA, "Eğitmen", "Yazan");
        educator.LinkUserAccount(ctx.UserId);
        var session = Session.Schedule(TenantA, Guid.CreateVersion7(), At, At.AddMinutes(45));
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var rows = new List<object> { educator, session, SessionParticipant.Create(TenantA, session.Id, a) };
        if (participants > 1) rows.Add(SessionParticipant.Create(TenantA, session.Id, b));
        await ctx.SeedAsync(rows.ToArray());
        return (ctx, session, a, b);
    }

    [Fact]
    public async Task Write_in_a_single_student_session_links_the_student_automatically()
    {
        var (ctx, session, a, _) = await WriterAsync(1);
        using var __ = ctx;

        var dto = await ctx.Handler<WriteSessionNoteCommandHandler>().Handle(
            new WriteSessionNoteCommand(session.Id, "Not", true), default);

        dto.StudentId.Should().Be(a);
        (await ctx.Db.SessionNotes.SingleAsync()).StudentId.Should().Be(a);
    }

    [Fact]
    public async Task Write_parent_visible_in_a_group_session_requires_a_student()
    {
        var (ctx, session, _, _) = await WriterAsync(2);
        using var __ = ctx;

        var act = () => ctx.Handler<WriteSessionNoteCommandHandler>().Handle(
            new WriteSessionNoteCommand(session.Id, "Not", true), default);

        var ex = await act.Should().ThrowAsync<Aynesil.Application.Common.Exceptions.ValidationException>();
        ex.Which.Errors.Should().ContainKey("studentId");
        (await ctx.Db.SessionNotes.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Write_internal_note_in_a_group_session_needs_no_student_and_chosen_student_is_stored()
    {
        var (ctx, session, _, b) = await WriterAsync(2);
        using var __ = ctx;
        var handler = ctx.Handler<WriteSessionNoteCommandHandler>();

        (await handler.Handle(new WriteSessionNoteCommand(session.Id, "Dahili", false), default)).StudentId.Should().BeNull();
        (await handler.Handle(new WriteSessionNoteCommand(session.Id, "B için", true, b), default)).StudentId.Should().Be(b);
    }

    [Fact]
    public async Task Write_rejects_a_student_who_is_not_a_participant()
    {
        var (ctx, session, _, _) = await WriterAsync(2);
        using var __ = ctx;

        var act = () => ctx.Handler<WriteSessionNoteCommandHandler>().Handle(
            new WriteSessionNoteCommand(session.Id, "Not", true, Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<Aynesil.Application.Common.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Edit_making_a_studentless_group_note_visible_requires_a_student()
    {
        var (ctx, session, a, _) = await WriterAsync(2);
        using var __ = ctx;
        var write = await ctx.Handler<WriteSessionNoteCommandHandler>().Handle(
            new WriteSessionNoteCommand(session.Id, "Dahili", false), default);
        var edit = ctx.Handler<EditSessionNoteCommandHandler>();

        var act = () => edit.Handle(new EditSessionNoteCommand(write.Id, "Dahili", true, write.RowVersion), default);
        await act.Should().ThrowAsync<Aynesil.Application.Common.Exceptions.ValidationException>();

        var ok = await edit.Handle(new EditSessionNoteCommand(write.Id, "Dahili", true, write.RowVersion, a), default);
        ok.StudentId.Should().Be(a);
    }
}
