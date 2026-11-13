using Aynesil.Application.Features.Leaves.Queries;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Ops.Entities;
using FluentAssertions;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>İzin listesi ekranının arama kutusu eğitmen adına göre çalışır (frontend LeaveRequestListQuery.search).</summary>
public class LeaveSearchTests
{
    private static readonly DateTimeOffset T = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Search_filters_by_educator_name_case_insensitively()
    {
        using var ctx = new TenantTestContext();
        var zeynep = Educator.Create(TenantA, "Zeynep", "Ak");
        var mehmet = Educator.Create(TenantA, "Mehmet", "Kaya");
        await ctx.SeedAsync(zeynep, mehmet,
            LeaveRequest.Submit(TenantA, zeynep.Id, null, "day", T, T.AddDays(1), 1m, null),
            LeaveRequest.Submit(TenantA, mehmet.Id, null, "day", T, T.AddDays(1), 1m, null));

        var handler = ctx.Handler<GetLeaveRequestsQueryHandler>();
        var byName = await handler.Handle(new GetLeaveRequestsQuery { Search = "  ZEYN " }, default);
        var all = await handler.Handle(new GetLeaveRequestsQuery(), default);
        var none = await handler.Handle(new GetLeaveRequestsQuery { Search = "bulunmaz" }, default);

        byName.Items.Should().ContainSingle().Which.EducatorFullName.Should().Be("Zeynep Ak");
        all.TotalCount.Should().Be(2);
        none.TotalCount.Should().Be(0);
    }
}
