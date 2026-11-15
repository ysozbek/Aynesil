using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Educators.Commands;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Educators.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Eğitmen hiyerarşisinde ilişkiyi sonlandırma (geçmiş silinmez, ActiveTo yazılır).</summary>
public class EndHierarchyTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static async Task<(TenantTestContext ctx, EducatorHierarchy edge)> Arrange(DateOnly? from = null, DateOnly? to = null)
    {
        var ctx = new TenantTestContext();
        var a = Educator.Create(TenantA, "Alt", "Eğitmen");
        var b = Educator.Create(TenantA, "Üst", "Denetçi");
        var edge = new EducatorHierarchy { CorporationId = TenantA, EducatorId = a.Id, SupervisorId = b.Id, ActiveFrom = from ?? Today.AddDays(-30), ActiveTo = to };
        await ctx.SeedAsync(a, b, edge);
        return (ctx, edge);
    }

    [Fact]
    public async Task Ends_the_relationship_at_the_given_date_and_keeps_the_record()
    {
        var (ctx, edge) = await Arrange();
        using var _ = ctx;

        var dto = await ctx.Handler<EndHierarchyCommandHandler>().Handle(
            new EndHierarchyCommand(edge.Id, Today.AddDays(-1), edge.EducatorId), default);

        dto.ActiveTo.Should().Be(Today.AddDays(-1));
        dto.IsActive.Should().BeFalse();
        dto.EducatorFullName.Should().Be("Alt Eğitmen");
        (await ctx.Db.EducatorHierarchies.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Without_a_date_it_ends_today_and_a_future_end_keeps_it_active_until_then()
    {
        var (ctx, edge) = await Arrange();
        using var _ = ctx;
        var handler = ctx.Handler<EndHierarchyCommandHandler>();

        var future = await handler.Handle(new EndHierarchyCommand(edge.Id, Today.AddDays(10), edge.EducatorId), default);
        future.IsActive.Should().BeTrue();

        var today = await handler.Handle(new EndHierarchyCommand(edge.Id, null, edge.EducatorId), default);
        today.ActiveTo.Should().Be(Today);
    }

    [Fact]
    public async Task End_date_before_the_start_is_rejected()
    {
        var (ctx, edge) = await Arrange(from: Today.AddDays(-5));
        using var _ = ctx;

        var act = () => ctx.Handler<EndHierarchyCommandHandler>().Handle(
            new EndHierarchyCommand(edge.Id, Today.AddDays(-6), edge.EducatorId), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.EducatorHierarchies.SingleAsync()).ActiveTo.Should().BeNull();
    }

    [Fact]
    public async Task An_already_ended_relationship_cannot_be_ended_again()
    {
        var (ctx, edge) = await Arrange(to: Today.AddDays(-3));
        using var _ = ctx;

        var act = () => ctx.Handler<EndHierarchyCommandHandler>().Handle(
            new EndHierarchyCommand(edge.Id, Today, edge.EducatorId), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task The_educator_in_the_path_must_be_the_supervised_side_of_the_edge()
    {
        var (ctx, edge) = await Arrange();
        using var _ = ctx;

        var act = () => ctx.Handler<EndHierarchyCommandHandler>().Handle(
            new EndHierarchyCommand(edge.Id, Today, edge.SupervisorId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
