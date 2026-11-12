using Aynesil.Application.Features.Leaves.Commands;
using Aynesil.Domain.Modules.Ops.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Y5: izin talebi onay/ret kaydındaki onaylayan istemciden gelmez; oturumdaki kullanıcıdır
/// (leave_approval.approver_id → iam.user_account). Komutta ApproverId alanı yoktur
/// (TenantContractTests.ForbiddenRequestMembers da bunu bütün istekler için denetler).
/// </summary>
public class LeaveApproverTests
{
    private static LeaveRequest PendingLeave() => LeaveRequest.Submit(
        TenantA, Guid.CreateVersion7(), leaveTypeId: null, "day",
        DateTimeOffset.UtcNow.AddDays(3), DateTimeOffset.UtcNow.AddDays(4), quantity: null, "Tatil");

    [Fact]
    public void Commands_do_not_accept_an_approver_from_the_client()
    {
        typeof(ApproveLeaveRequestCommand).GetProperty("ApproverId").Should().BeNull();
        typeof(RejectLeaveRequestCommand).GetProperty("ApproverId").Should().BeNull();
    }

    [Fact]
    public async Task Approve_records_the_signed_in_user_as_approver()
    {
        using var ctx = new TenantTestContext();
        var leave = PendingLeave();
        await ctx.SeedAsync(leave);

        var dto = await ctx.Handler<ApproveLeaveRequestCommandHandler>().Handle(
            new ApproveLeaveRequestCommand(leave.Id, "Uygun"), default);

        dto.ApproverId.Should().Be(ctx.UserId);
        var approval = await ctx.Db.LeaveApprovals.SingleAsync();
        approval.ApproverId.Should().Be(ctx.UserId);
        approval.Decision.Should().Be("approved");
        (await ctx.Db.LeaveRequests.SingleAsync()).Status.Should().Be("approved");
    }

    [Fact]
    public async Task Reject_records_the_signed_in_user_as_approver()
    {
        using var ctx = new TenantTestContext();
        var leave = PendingLeave();
        await ctx.SeedAsync(leave);

        await ctx.Handler<RejectLeaveRequestCommandHandler>().Handle(
            new RejectLeaveRequestCommand(leave.Id, "Uygun değil"), default);

        var approval = await ctx.Db.LeaveApprovals.SingleAsync();
        approval.ApproverId.Should().Be(ctx.UserId);
        approval.Decision.Should().Be("rejected");
    }

    [Fact]
    public async Task Approve_without_a_signed_in_user_is_unauthorized_and_writes_nothing()
    {
        using var ctx = new TenantTestContext();
        ctx.CurrentUser.SetupGet(u => u.UserId).Returns((Guid?)null);
        var leave = PendingLeave();
        await ctx.SeedAsync(leave);

        var act = () => ctx.Handler<ApproveLeaveRequestCommandHandler>().Handle(
            new ApproveLeaveRequestCommand(leave.Id, null), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        (await ctx.Db.LeaveApprovals.CountAsync()).Should().Be(0);
        (await ctx.Db.LeaveRequests.SingleAsync()).Status.Should().Be("pending");
    }
}
