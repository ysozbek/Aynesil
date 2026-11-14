using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Features.Scheduling.Commands;
using Aynesil.Application.Features.Scheduling.Queries;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// scheduling.session_note.authored_by references educators.educator, not iam.user_account:
/// the author is the educator record linked to the signed-in user (educator.user_id).
/// </summary>
public class SessionNoteAuthorTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    private static Session NewSession() => Session.Schedule(TenantA, Guid.CreateVersion7(), At, At.AddMinutes(40));

    private static Educator LinkedEducator(Guid userId, Guid corporationId = default)
    {
        var educator = Educator.Create(corporationId == default ? TenantA : corporationId, "Ayşe", "Yılmaz");
        educator.LinkUserAccount(userId);
        return educator;
    }

    [Fact]
    public async Task Write_stores_the_educator_linked_to_the_signed_in_user_as_author()
    {
        using var ctx = new TenantTestContext();
        var session = NewSession();
        var educator = LinkedEducator(ctx.UserId);
        await ctx.SeedAsync(session, educator, Educator.Create(TenantA, "Başka", "Eğitmen"));

        var dto = await ctx.Handler<WriteSessionNoteCommandHandler>().Handle(
            new WriteSessionNoteCommand(session.Id, "Seans iyi geçti.", true), default);

        dto.AuthoredBy.Should().Be(educator.Id).And.NotBe(ctx.UserId);
        dto.AuthorName.Should().Be("Ayşe Yılmaz");
        (await ctx.Db.SessionNotes.SingleAsync()).AuthoredBy.Should().Be(educator.Id);
    }

    [Fact]
    public async Task Write_without_an_educator_record_is_a_validation_error_and_saves_nothing()
    {
        using var ctx = new TenantTestContext();
        var session = NewSession();
        await ctx.SeedAsync(session);

        var act = () => ctx.Handler<WriteSessionNoteCommandHandler>().Handle(
            new WriteSessionNoteCommand(session.Id, "Not", false), default);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("educator");
        (await ctx.Db.SessionNotes.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Write_ignores_an_educator_linked_to_the_same_user_in_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var session = NewSession();
        await ctx.SeedAsync(session, LinkedEducator(ctx.UserId, TenantB));

        var act = () => ctx.Handler<WriteSessionNoteCommandHandler>().Handle(
            new WriteSessionNoteCommand(session.Id, "Not", false), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Edit_returns_the_name_of_the_educator_who_wrote_the_note()
    {
        using var ctx = new TenantTestContext();
        var session = NewSession();
        var author = LinkedEducator(ctx.UserId); // düzenleyen = notu yazan eğitmen
        var note = SessionNote.Write(TenantA, session.Id, "İlk", false, author.Id);
        await ctx.SeedAsync(session, author, note);

        var dto = await ctx.Handler<EditSessionNoteCommandHandler>().Handle(
            new EditSessionNoteCommand(note.Id, "Düzenlendi", true, note.RowVersion), default);

        dto.AuthoredBy.Should().Be(author.Id);
        dto.AuthorName.Should().Be("Ayşe Yılmaz");
        dto.Body.Should().Be("Düzenlendi");
        dto.ParentVisible.Should().BeTrue();
    }

    [Fact]
    public async Task Edit_by_another_educator_without_manage_permission_is_forbidden()
    {
        using var ctx = new TenantTestContext();
        var session = NewSession();
        var author = Educator.Create(TenantA, "Yazan", "Eğitmen");
        var me = LinkedEducator(ctx.UserId); // başka bir eğitmen
        var note = SessionNote.Write(TenantA, session.Id, "İlk", false, author.Id);
        await ctx.SeedAsync(session, author, me, note);

        var act = () => ctx.Handler<EditSessionNoteCommandHandler>().Handle(
            new EditSessionNoteCommand(note.Id, "Ele geçirildi", true, note.RowVersion), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        (await ctx.Db.SessionNotes.SingleAsync()).Body.Should().Be("İlk");
    }

    [Fact]
    public async Task Edit_by_a_user_with_no_educator_record_and_no_permission_is_forbidden()
    {
        using var ctx = new TenantTestContext();
        var session = NewSession();
        var author = Educator.Create(TenantA, "Yazan", "Eğitmen");
        var note = SessionNote.Write(TenantA, session.Id, "İlk", false, author.Id);
        await ctx.SeedAsync(session, author, note);

        var act = () => ctx.Handler<EditSessionNoteCommandHandler>().Handle(
            new EditSessionNoteCommand(note.Id, "x", false, note.RowVersion), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Edit_by_an_administrator_may_change_another_educators_note()
    {
        using var ctx = new TenantTestContext();
        ctx.CurrentUser.Setup(u => u.HasPermission(Aynesil.Shared.Constants.Permissions.SessionNotes.Delete)).Returns(true);
        var session = NewSession();
        var author = Educator.Create(TenantA, "Yazan", "Eğitmen");
        var note = SessionNote.Write(TenantA, session.Id, "İlk", false, author.Id);
        await ctx.SeedAsync(session, author, note);

        var dto = await ctx.Handler<EditSessionNoteCommandHandler>().Handle(
            new EditSessionNoteCommand(note.Id, "Yönetici düzeltti", false, note.RowVersion), default);

        dto.Body.Should().Be("Yönetici düzeltti");
        dto.AuthoredBy.Should().Be(author.Id, "yazar değişmez");
    }

    [Fact]
    public async Task Edit_with_a_stale_row_version_is_rejected()
    {
        using var ctx = new TenantTestContext();
        var session = NewSession();
        var author = LinkedEducator(ctx.UserId);
        var note = SessionNote.Write(TenantA, session.Id, "İlk", false, author.Id);
        await ctx.SeedAsync(session, author, note);

        var act = () => ctx.Handler<EditSessionNoteCommandHandler>().Handle(
            new EditSessionNoteCommand(note.Id, "x", false, note.RowVersion + 7), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Session_projection_resolves_note_author_names_through_educators()
    {
        using var ctx = new TenantTestContext();
        var session = NewSession();
        var author = Educator.Create(TenantA, "Yazan", "Eğitmen");
        var note = SessionNote.Write(TenantA, session.Id, "Not", false, author.Id);
        await ctx.SeedAsync(session, author, note);

        var dto = await ctx.Handler<GetSessionQueryHandler>().Handle(new GetSessionQuery(session.Id), default);

        dto.Notes.Should().ContainSingle().Which.AuthorName.Should().Be("Yazan Eğitmen");
    }
}
