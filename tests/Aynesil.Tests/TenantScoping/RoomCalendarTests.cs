using Aynesil.Application.Features.Scheduling.Queries;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentAssertions;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Oda takvimi: yalnızca o odanın, aralıktaki seansları; başka kurumun odası boş döner.</summary>
public class RoomCalendarTests
{
    private static readonly DateTimeOffset Day = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    private static Session In(Room room, int hour, Guid? corp = null)
        => Session.Schedule(corp ?? room.CorporationId, Guid.CreateVersion7(), Day.AddHours(hour), Day.AddHours(hour).AddMinutes(40), roomId: room.Id);

    [Fact]
    public async Task Returns_only_the_rooms_sessions_inside_the_range_ordered_by_start_including_cancelled()
    {
        using var ctx = new TenantTestContext();
        var room = Room.Create(TenantA, "A", "Derslik A", 4, false);
        var other = Room.Create(TenantA, "B", "Derslik B", 4, false);
        var late = In(room, 3);
        var early = In(room, 0);
        var cancelled = In(room, 1);
        cancelled.Cancel("Hasta");
        var outside = Session.Schedule(TenantA, Guid.CreateVersion7(), Day.AddDays(3), Day.AddDays(3).AddMinutes(40), roomId: room.Id);
        await ctx.SeedAsync(room, other, late, early, cancelled, outside, In(other, 2));

        var result = await ctx.Handler<GetRoomCalendarQueryHandler>().Handle(
            new GetRoomCalendarQuery(room.Id, Day.AddHours(-1), Day.AddDays(1)), default);

        result.Select(r => r.Id).Should().ContainInOrder(early.Id, cancelled.Id, late.Id).And.HaveCount(3);
        result.Should().OnlyContain(r => r.RoomName == "Derslik A");
        result.Single(r => r.Id == cancelled.Id).Status.Should().Be("cancelled");
    }

    [Fact]
    public async Task A_room_of_another_tenant_returns_nothing()
    {
        using var ctx = new TenantTestContext();
        var foreignRoom = Room.Create(TenantB, "X", "Yabancı oda", 4, false);
        await ctx.SeedAsync(foreignRoom, In(foreignRoom, 0));

        var result = await ctx.Handler<GetRoomCalendarQueryHandler>().Handle(
            new GetRoomCalendarQuery(foreignRoom.Id, Day.AddHours(-1), Day.AddDays(1)), default);

        result.Should().BeEmpty();
    }
}
