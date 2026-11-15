using Aynesil.Application.Features.Scheduling.Commands;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Toplu yoklama: tümünü "geldi" işaretle, sonra tek tek düzelt.</summary>
public class BulkAttendanceTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    private static (Session session, Guid a, Guid b) GroupSession()
    {
        var session = Session.Schedule(TenantA, Guid.CreateVersion7(), At, At.AddMinutes(40));
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        session.Participants.Add(SessionParticipant.Create(TenantA, session.Id, a));
        session.Participants.Add(SessionParticipant.Create(TenantA, session.Id, b));
        return (session, a, b);
    }

    [Fact]
    public async Task Marks_every_participant_present_then_a_single_correction_overwrites_one()
    {
        using var ctx = new TenantTestContext();
        var (session, a, b) = GroupSession();
        await ctx.SeedAsync(session);

        var all = await ctx.Handler<BulkRecordAttendanceCommandHandler>().Handle(new BulkRecordAttendanceCommand(session.Id,
            [new(a, "present", null, null, null), new(b, "present", null, null, null)]), default);
        all.Should().HaveCount(2).And.OnlyContain(x => x.Status == "present");

        await ctx.Handler<RecordAttendanceCommandHandler>().Handle(new RecordAttendanceCommand(session.Id, b, "absent", null, null, "Hasta"), default);

        var rows = await ctx.Db.Attendances.ToListAsync();
        rows.Should().HaveCount(2);
        rows.Single(r => r.StudentId == a).Status.Should().Be("present");
        rows.Single(r => r.StudentId == b).Status.Should().Be("absent");
    }

    [Fact]
    public async Task A_second_bulk_call_updates_existing_rows_instead_of_adding_new_ones()
    {
        using var ctx = new TenantTestContext();
        var (session, a, b) = GroupSession();
        await ctx.SeedAsync(session);
        var handler = ctx.Handler<BulkRecordAttendanceCommandHandler>();

        await handler.Handle(new BulkRecordAttendanceCommand(session.Id, [new(a, "late", null, null, null)]), default);
        await handler.Handle(new BulkRecordAttendanceCommand(session.Id,
            [new(a, "present", null, null, null), new(b, "present", null, null, null)]), default);

        (await ctx.Db.Attendances.CountAsync()).Should().Be(2);
        (await ctx.Db.Attendances.SingleAsync(x => x.StudentId == a)).Status.Should().Be("present");
    }

    [Fact]
    public async Task Duplicate_students_in_one_request_keep_the_last_entry()
    {
        using var ctx = new TenantTestContext();
        var (session, a, _) = GroupSession();
        await ctx.SeedAsync(session);

        var res = await ctx.Handler<BulkRecordAttendanceCommandHandler>().Handle(new BulkRecordAttendanceCommand(session.Id,
            [new(a, "present", null, null, null), new(a, "absent", null, null, null)]), default);

        res.Should().ContainSingle().Which.Status.Should().Be("absent");
        (await ctx.Db.Attendances.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task A_student_who_is_not_a_participant_is_rejected_and_nothing_is_saved()
    {
        using var ctx = new TenantTestContext();
        var (session, a, _) = GroupSession();
        await ctx.SeedAsync(session);

        var act = () => ctx.Handler<BulkRecordAttendanceCommandHandler>().Handle(new BulkRecordAttendanceCommand(session.Id,
            [new(a, "present", null, null, null), new(Guid.CreateVersion7(), "present", null, null, null)]), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.Attendances.CountAsync()).Should().Be(0);
    }
}
