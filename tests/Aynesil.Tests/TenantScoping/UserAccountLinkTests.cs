using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Identity;
using Aynesil.Application.Features.Educators.Commands;
using Aynesil.Application.Features.Students.Commands;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Educator/guardian ↔ user account link handlers (V48). InMemory has no RLS or unique indexes, so these
/// prove the handlers' own checks: same corporation, one record per user, no silent re-link.
/// The real-database counterpart (RLS, unique index, login, session note) is UserAccountLinkTests in
/// Aynesil.IntegrationTests.
/// </summary>
public class UserAccountLinkTests
{
    private static UserAccount User(Guid corporationId, string username)
        => UserAccount.Create(corporationId, username, username, activateImmediately: true);

    [Fact]
    public async Task Educator_link_and_unlink()
    {
        using var ctx = new TenantTestContext();
        var educator = Educator.Create(TenantA, "Ayşe", "Yılmaz");
        var user = User(TenantA, "ayse");
        await ctx.SeedAsync(educator, user);

        var dto = await ctx.Handler<LinkEducatorUserAccountCommandHandler>()
            .Handle(new LinkEducatorUserAccountCommand(educator.Id, user.Id), default);
        dto.Username.Should().Be("ayse");
        (await ctx.Db.Educators.SingleAsync()).UserId.Should().Be(user.Id);

        var byUser = await ctx.Handler<GetEducatorByUserQueryHandler>().Handle(new GetEducatorByUserQuery(user.Id), default);
        byUser!.Id.Should().Be(educator.Id);

        ctx.Db.ChangeTracker.Clear();
        await ctx.Handler<UnlinkEducatorUserAccountCommandHandler>()
            .Handle(new UnlinkEducatorUserAccountCommand(educator.Id), default);
        (await ctx.Db.Educators.SingleAsync()).UserId.Should().BeNull();
        (await ctx.Db.UserAccounts.CountAsync()).Should().Be(1, "unlinking keeps the account");
    }

    [Fact]
    public async Task Educator_link_rejects_a_user_of_another_corporation()
    {
        using var ctx = new TenantTestContext();
        var educator = Educator.Create(TenantA, "Ayşe", "Yılmaz");
        var foreign = User(TenantB, "foreign");
        await ctx.SeedAsync(educator, foreign);

        await FluentActions.Awaiting(() => ctx.Handler<LinkEducatorUserAccountCommandHandler>()
                .Handle(new LinkEducatorUserAccountCommand(educator.Id, foreign.Id), default))
            .Should().ThrowAsync<NotFoundException>();
        (await ctx.Db.Educators.SingleAsync()).UserId.Should().BeNull();
    }

    [Fact]
    public async Task A_user_cannot_back_two_educators()
    {
        using var ctx = new TenantTestContext();
        var user = User(TenantA, "ortak");
        var first = Educator.Create(TenantA, "Birinci", "Eğitmen");
        first.LinkUserAccount(user.Id);
        var second = Educator.Create(TenantA, "İkinci", "Eğitmen");
        await ctx.SeedAsync(user, first, second);

        var ex = await FluentActions.Awaiting(() => ctx.Handler<LinkEducatorUserAccountCommandHandler>()
                .Handle(new LinkEducatorUserAccountCommand(second.Id, user.Id), default))
            .Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be(Aynesil.Domain.Common.MsgKeys.Rule.UserAlreadyLinkedToEducator);
        ex.Which.Args.Should().Contain("Birinci Eğitmen");
        (await ctx.Db.Educators.SingleAsync(e => e.Id == second.Id)).UserId.Should().BeNull();
    }

    [Fact]
    public async Task A_linked_educator_is_not_silently_relinked_to_another_user()
    {
        using var ctx = new TenantTestContext();
        var current = User(TenantA, "mevcut");
        var other = User(TenantA, "diger");
        var educator = Educator.Create(TenantA, "Ayşe", "Yılmaz");
        educator.LinkUserAccount(current.Id);
        await ctx.SeedAsync(current, other, educator);

        await FluentActions.Awaiting(() => ctx.Handler<LinkEducatorUserAccountCommandHandler>()
                .Handle(new LinkEducatorUserAccountCommand(educator.Id, other.Id), default))
            .Should().ThrowAsync<ConflictException>();
        (await ctx.Db.Educators.SingleAsync()).UserId.Should().Be(current.Id);
    }

    [Fact]
    public async Task Guardian_link_rules_match_the_educator_ones()
    {
        using var ctx = new TenantTestContext();
        var user = User(TenantA, "veli");
        var foreign = User(TenantB, "yabanci");
        var linked = Guardian.Create(TenantA, "Bağlı", "Veli");
        linked.LinkPortalAccount(user.Id);
        var free = Guardian.Create(TenantA, "Boş", "Veli");
        await ctx.SeedAsync(user, foreign, linked, free);

        await FluentActions.Awaiting(() => ctx.Handler<LinkGuardianUserAccountCommandHandler>()
                .Handle(new LinkGuardianUserAccountCommand(free.Id, user.Id), default))
            .Should().ThrowAsync<ConflictException>();
        await FluentActions.Awaiting(() => ctx.Handler<LinkGuardianUserAccountCommandHandler>()
                .Handle(new LinkGuardianUserAccountCommand(free.Id, foreign.Id), default))
            .Should().ThrowAsync<NotFoundException>();

        await ctx.Handler<UnlinkGuardianUserAccountCommandHandler>()
            .Handle(new UnlinkGuardianUserAccountCommand(linked.Id), default);
        ctx.Db.ChangeTracker.Clear();
        var dto = await ctx.Handler<LinkGuardianUserAccountCommandHandler>()
            .Handle(new LinkGuardianUserAccountCommand(free.Id, user.Id), default);
        dto.Id.Should().Be(user.Id);
    }

    [Fact]
    public void Temporary_password_has_every_character_class_and_no_look_alikes()
    {
        for (var i = 0; i < 200; i++)
        {
            var p = UserAccountLinks.GenerateTemporaryPassword();
            p.Should().HaveLength(14);
            p.Should().MatchRegex("[a-z]").And.MatchRegex("[A-Z]").And.MatchRegex("[0-9]").And.MatchRegex("[!@#$%*?]");
            p.Should().NotContainAny("0", "O", "1", "l", "I");
        }
    }
}
