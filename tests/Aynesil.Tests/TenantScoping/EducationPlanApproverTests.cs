using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Plans.Commands;
using Aynesil.Domain.Modules.Education.Entities;
using Aynesil.Domain.Modules.Educators.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// BEP onay/ret: onaylayan istemciden gelmez; oturumdaki kullanıcıya bağlı eğitmen kaydından türetilir.
/// </summary>
public class EducationPlanApproverTests
{
    private static EducationPlan InReviewPlan()
    {
        var plan = EducationPlan.Create(TenantA, Guid.CreateVersion7(), "BEP");
        plan.SubmitForReview();
        return plan;
    }

    private static Educator LinkedEducator(Guid userId, Guid? corporationId = null)
    {
        var educator = Educator.Create(corporationId ?? TenantA, "Ayşe", "Yılmaz");
        educator.LinkUserAccount(userId);
        return educator;
    }

    [Fact]
    public async Task Approve_records_the_educator_linked_to_the_signed_in_user_as_approver()
    {
        using var ctx = new TenantTestContext();
        var plan = InReviewPlan();
        var educator = LinkedEducator(ctx.UserId);
        await ctx.SeedAsync(plan, educator, Educator.Create(TenantA, "Başka", "Eğitmen"));

        await ctx.Handler<ApproveEducationPlanCommandHandler>().Handle(
            new ApproveEducationPlanCommand(plan.Id, "Uygun"), default);

        var saved = await ctx.Db.EducationPlans.SingleAsync();
        saved.Status.Should().Be("approved");
        saved.ApprovedBy.Should().Be(educator.Id).And.NotBe(ctx.UserId);
        (await ctx.Db.EducationPlanApprovals.SingleAsync()).ApproverId.Should().Be(educator.Id);
    }

    [Fact]
    public async Task Reject_records_the_educator_linked_to_the_signed_in_user_as_approver()
    {
        using var ctx = new TenantTestContext();
        var plan = InReviewPlan();
        var educator = LinkedEducator(ctx.UserId);
        await ctx.SeedAsync(plan, educator);

        await ctx.Handler<RejectEducationPlanCommandHandler>().Handle(
            new RejectEducationPlanCommand(plan.Id, "Eksik"), default);

        (await ctx.Db.EducationPlans.SingleAsync()).Status.Should().Be("draft");
        var approval = await ctx.Db.EducationPlanApprovals.SingleAsync();
        approval.ApproverId.Should().Be(educator.Id);
        approval.Decision.Should().Be("rejected");
    }

    [Fact]
    public async Task Approve_without_an_educator_record_is_a_validation_error_and_changes_nothing()
    {
        using var ctx = new TenantTestContext();
        var plan = InReviewPlan();
        await ctx.SeedAsync(plan);

        var act = () => ctx.Handler<ApproveEducationPlanCommandHandler>().Handle(
            new ApproveEducationPlanCommand(plan.Id, null), default);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("educator");
        (await ctx.Db.EducationPlans.SingleAsync()).Status.Should().Be("in_review");
        (await ctx.Db.EducationPlanApprovals.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Reject_ignores_an_educator_linked_to_the_same_user_in_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var plan = InReviewPlan();
        await ctx.SeedAsync(plan, LinkedEducator(ctx.UserId, TenantB));

        var act = () => ctx.Handler<RejectEducationPlanCommandHandler>().Handle(
            new RejectEducationPlanCommand(plan.Id, null), default);

        await act.Should().ThrowAsync<ValidationException>();
        (await ctx.Db.EducationPlans.SingleAsync()).Status.Should().Be("in_review");
    }
}
