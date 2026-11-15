using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Portal.Queries;
using Aynesil.Domain.Modules.Education.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Veli portalı BEP özeti: yalnızca onaylı/yürürlükteki ve veliye açık planlar; CanViewPlan kapısı.</summary>
public class PortalBepTests
{
    private static EducationPlan Plan(Guid studentId, string title, string target, bool visible)
    {
        var plan = EducationPlan.Create(TenantA, studentId, title);
        if (target == "draft") return plan;
        plan.SubmitForReview();
        if (target == "in_review") return plan;
        plan.Approve(Guid.CreateVersion7());
        if (visible) plan.MakeGuardianVisible();
        if (target == "active") plan.Activate();
        if (target == "closed") { plan.Activate(); plan.Close(); }
        return plan;
    }

    private static async Task<(TenantTestContext ctx, Guid studentId)> ArrangeAsync(bool canViewPlan = true, bool revoked = false, bool linked = true)
    {
        var ctx = new TenantTestContext();
        var guardian = Guardian.Create(TenantA, "Ayşe", "Kaya");
        if (linked) guardian.LinkPortalAccount(ctx.UserId);
        var studentId = Guid.CreateVersion7();
        var access = new GuardianPortalAccess
        {
            CorporationId = TenantA, GuardianId = guardian.Id, StudentId = studentId,
            CanViewPlan = canViewPlan, RevokedAt = revoked ? DateTimeOffset.UtcNow : null,
        };
        await ctx.SeedAsync(guardian, access);
        return (ctx, studentId);
    }

    private static Task<IReadOnlyList<Aynesil.Application.Features.Portal.Dtos.PortalEducationPlanDto>> BepAsync(TenantTestContext ctx, Guid studentId)
        => ctx.Handler<GetPortalEducationPlanQueryHandler>().Handle(new GetPortalEducationPlanQuery(studentId, ctx.UserId), default);

    [Fact]
    public async Task Shows_only_approved_or_active_plans_that_are_open_to_the_guardian()
    {
        var (ctx, studentId) = await ArrangeAsync();
        using var _ = ctx;
        await ctx.SeedAsync(
            Plan(studentId, "Onaylı ve açık", "approved", true),
            Plan(studentId, "Yürürlükte ve açık", "active", true),
            Plan(studentId, "Onaylı ama veliye kapalı", "approved", false),
            Plan(studentId, "Taslak", "draft", false),
            Plan(studentId, "İncelemede", "in_review", false),
            Plan(studentId, "Kapanmış", "closed", true),
            Plan(Guid.CreateVersion7(), "Başka öğrencinin planı", "approved", true));

        var plans = await BepAsync(ctx, studentId);

        plans.Select(p => p.Title).Should().BeEquivalentTo(["Onaylı ve açık", "Yürürlükte ve açık"]);
        plans.Single(p => p.Title == "Onaylı ve açık").ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Hides_a_soft_deleted_plan()
    {
        var (ctx, studentId) = await ArrangeAsync();
        using var _ = ctx;
        var plan = Plan(studentId, "Silinen", "approved", true);
        plan.SoftDelete(null);
        await ctx.SeedAsync(plan);

        (await BepAsync(ctx, studentId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_nothing_when_CanViewPlan_is_off()
    {
        var (ctx, studentId) = await ArrangeAsync(canViewPlan: false);
        using var _ = ctx;
        await ctx.SeedAsync(Plan(studentId, "Onaylı ve açık", "approved", true));

        (await BepAsync(ctx, studentId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Revoked_or_missing_portal_access_is_forbidden()
    {
        var (revoked, s1) = await ArrangeAsync(revoked: true);
        using var _a = revoked;
        await revoked.SeedAsync(Plan(s1, "Plan", "approved", true));
        var act1 = () => BepAsync(revoked, s1);
        await act1.Should().ThrowAsync<ForbiddenAccessException>();

        var (unlinked, s2) = await ArrangeAsync(linked: false);
        using var _b = unlinked;
        var act2 = () => BepAsync(unlinked, s2);
        await act2.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
