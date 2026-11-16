using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Camps.Commands;
using Aynesil.Application.Features.Camps.Queries;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Kampın günlük işleri: etkinlik, katılım (tekil/toplu), yoklama (tekil/toplu/düzeltme), kamp sonu raporu.
/// Kural ihlalleri BusinessRuleViolationException (HTTP 409) olarak beklenir.
/// </summary>
public class CampDailyOperationsTests
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static (Camp Camp, CampPeriod A, CampPeriod B) NewCamp()
    {
        var camp = Camp.Create(TenantA, "yaz", "Yaz Kampı");
        var a = CampPeriod.Create(TenantA, camp.Id, "A", Today.AddDays(-3), Today.AddDays(10), 10);
        var b = CampPeriod.Create(TenantA, camp.Id, "B", Today.AddDays(-3), Today.AddDays(10), 10);
        return (camp, a, b);
    }

    private static CampEnrollment Enr(Guid periodId, string status = "enrolled")
    {
        var e = CampEnrollment.Create(TenantA, periodId, Guid.CreateVersion7());
        switch (status)
        {
            case "waitlist": e.MoveToWaitlist(); break;
            case "completed": e.Complete(); break;
            case "withdrawn": e.Withdraw(); break;
        }
        return e;
    }

    /// <summary>SeedAsync izleyiciyi temizlediği için kaydı yeniden yükleyip tamamlar.</summary>
    private static async Task CompleteAsync(TenantTestContext ctx, Guid enrollmentId)
    {
        (await ctx.Db.CampEnrollments.SingleAsync(x => x.Id == enrollmentId)).Complete();
        await ctx.Db.SaveChangesAsync();
        ctx.Db.ChangeTracker.Clear();
    }

    // ── Etkinlik ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Activity_create_update_delete_round_trip()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        await ctx.SeedAsync(camp, a);

        var created = await ctx.Handler<CreateCampActivityCommandHandler>().Handle(
            new CreateCampActivityCommand(a.Id, "Yüzme", null, null, null, null, "Havuz", 5), default);

        await ctx.Handler<UpdateCampActivityCommandHandler>().Handle(
            new UpdateCampActivityCommand(created.Id, "Yüzme 2", null, "açıklama", null, null, "Havuz", 6, null, created.RowVersion), default);
        (await ctx.Db.CampActivities.SingleAsync()).Name.Should().Be("Yüzme 2");

        await ctx.Handler<DeleteCampActivityCommandHandler>().Handle(new DeleteCampActivityCommand(created.Id), default);
        (await ctx.Db.CampActivities.IgnoreQueryFilters().SingleAsync()).DeletedAt.Should().NotBeNull();
    }

    // ── Katılım ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Participation_for_enrollment_of_another_period_conflicts()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, b) = NewCamp();
        var act = CampActivity.Create(TenantA, a.Id, "Yüzme", capacity: 5);
        var other = Enr(b.Id);
        await ctx.SeedAsync(camp, a, b, act, other);

        var run = () => ctx.Handler<RegisterParticipationCommandHandler>().Handle(
            new RegisterParticipationCommand(act.Id, other.Id), default);
        await run.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("withdrawn")]
    [InlineData("waitlist")]
    public async Task Participation_requires_enrolled_student(string status)
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var act = CampActivity.Create(TenantA, a.Id, "Yüzme", capacity: 5);
        var e = Enr(a.Id, status);
        await ctx.SeedAsync(camp, a, act, e);

        var run = () => ctx.Handler<RegisterParticipationCommandHandler>().Handle(
            new RegisterParticipationCommand(act.Id, e.Id), default);
        await run.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Participation_duplicate_and_capacity_conflict()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var act = CampActivity.Create(TenantA, a.Id, "Yüzme", capacity: 1);
        var e1 = Enr(a.Id); var e2 = Enr(a.Id);
        await ctx.SeedAsync(camp, a, act, e1, e2);

        await ctx.Handler<RegisterParticipationCommandHandler>().Handle(new RegisterParticipationCommand(act.Id, e1.Id), default);
        await FluentActions.Awaiting(() => ctx.Handler<RegisterParticipationCommandHandler>()
            .Handle(new RegisterParticipationCommand(act.Id, e1.Id), default)).Should().ThrowAsync<BusinessRuleViolationException>();
        await FluentActions.Awaiting(() => ctx.Handler<RegisterParticipationCommandHandler>()
            .Handle(new RegisterParticipationCommand(act.Id, e2.Id), default)).Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Participation_capacity_ignores_absent_status_but_counts_registered()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var act = CampActivity.Create(TenantA, a.Id, "Yüzme", capacity: 1);
        var e1 = Enr(a.Id); var e2 = Enr(a.Id);
        await ctx.SeedAsync(camp, a, act, e1, e2);

        await ctx.Handler<RegisterParticipationCommandHandler>().Handle(new RegisterParticipationCommand(act.Id, e1.Id), default);
        // 'absent' kontenjan dolu olsa da kaydedilebilir (kontenjan yalnızca registered/attended sayar).
        await ctx.Handler<RegisterParticipationCommandHandler>().Handle(new RegisterParticipationCommand(act.Id, e2.Id, "absent"), default);
        (await ctx.Db.CampActivityParticipations.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task BulkParticipation_adds_new_and_skips_existing()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var act = CampActivity.Create(TenantA, a.Id, "Yüzme", capacity: 5);
        var e1 = Enr(a.Id); var e2 = Enr(a.Id);
        await ctx.SeedAsync(camp, a, act, e1, e2, CampActivityParticipation.Register(TenantA, act.Id, e1.Id));

        var added = await ctx.Handler<BulkRegisterParticipationCommandHandler>().Handle(
            new BulkRegisterParticipationCommand(act.Id, [
                new BulkParticipationInput(e1.Id, "attended", null),
                new BulkParticipationInput(e2.Id, "attended", null)]), default);

        added.Should().Be(1);
        (await ctx.Db.CampActivityParticipations.CountAsync()).Should().Be(2);
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("withdrawn")]
    [InlineData("waitlist")]
    public async Task BulkParticipation_rejects_non_enrolled_student(string status)
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var act = CampActivity.Create(TenantA, a.Id, "Yüzme", capacity: 5);
        var ok = Enr(a.Id); var bad = Enr(a.Id, status);
        await ctx.SeedAsync(camp, a, act, ok, bad);

        var run = () => ctx.Handler<BulkRegisterParticipationCommandHandler>().Handle(
            new BulkRegisterParticipationCommand(act.Id, [
                new BulkParticipationInput(ok.Id, "attended", null),
                new BulkParticipationInput(bad.Id, "attended", null)]), default);
        await run.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.CampActivityParticipations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BulkParticipation_rejects_enrollment_of_another_period()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, b) = NewCamp();
        var act = CampActivity.Create(TenantA, a.Id, "Yüzme", capacity: 5);
        var other = Enr(b.Id);
        await ctx.SeedAsync(camp, a, b, act, other);

        var run = () => ctx.Handler<BulkRegisterParticipationCommandHandler>().Handle(
            new BulkRegisterParticipationCommand(act.Id, [new BulkParticipationInput(other.Id, "attended", null)]), default);
        await run.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task BulkParticipation_over_capacity_conflicts()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var act = CampActivity.Create(TenantA, a.Id, "Yüzme", capacity: 1);
        var e1 = Enr(a.Id); var e2 = Enr(a.Id);
        await ctx.SeedAsync(camp, a, act, e1, e2);

        var run = () => ctx.Handler<BulkRegisterParticipationCommandHandler>().Handle(
            new BulkRegisterParticipationCommand(act.Id, [
                new BulkParticipationInput(e1.Id, "attended", null),
                new BulkParticipationInput(e2.Id, "attended", null)]), default);
        await run.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Participation_correction_changes_status_but_not_for_closed_enrollment()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var act = CampActivity.Create(TenantA, a.Id, "Yüzme", capacity: 5);
        var open = Enr(a.Id); var done = Enr(a.Id);
        var p1 = CampActivityParticipation.Register(TenantA, act.Id, open.Id);
        var p2 = CampActivityParticipation.Register(TenantA, act.Id, done.Id);
        await ctx.SeedAsync(camp, a, act, open, done, p1, p2);

        await ctx.Handler<UpdateParticipationCommandHandler>().Handle(new UpdateParticipationCommand(p1.Id, "attended"), default);
        (await ctx.Db.CampActivityParticipations.SingleAsync(p => p.Id == p1.Id)).Status.Should().Be("attended");

        await CompleteAsync(ctx, done.Id);
        await FluentActions.Awaiting(() => ctx.Handler<UpdateParticipationCommandHandler>()
            .Handle(new UpdateParticipationCommand(p2.Id, "attended"), default)).Should().ThrowAsync<BusinessRuleViolationException>();
    }

    // ── Yoklama ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("completed")]
    [InlineData("withdrawn")]
    [InlineData("waitlist")]
    public async Task Attendance_single_requires_enrolled_status(string status)
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var e = Enr(a.Id, status);
        await ctx.SeedAsync(camp, a, e);

        var run = () => ctx.Handler<RecordAttendanceCommandHandler>().Handle(
            new RecordAttendanceCommand(e.Id, Today, "present"), default);
        await run.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("withdrawn")]
    [InlineData("waitlist")]
    public async Task Attendance_bulk_rejects_non_enrolled_and_writes_nothing(string status)
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var ok = Enr(a.Id); var bad = Enr(a.Id, status);
        await ctx.SeedAsync(camp, a, ok, bad);

        var run = () => ctx.Handler<BulkRecordAttendanceCommandHandler>().Handle(
            new BulkRecordAttendanceCommand(a.Id, Today, [
                new BulkAttendanceInput(ok.Id, "present", null),
                new BulkAttendanceInput(bad.Id, "present", null)]), default);
        await run.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.CampAttendances.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Attendance_bulk_rejects_enrollment_of_another_period()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, b) = NewCamp();
        var other = Enr(b.Id);
        await ctx.SeedAsync(camp, a, b, other);

        var run = () => ctx.Handler<BulkRecordAttendanceCommandHandler>().Handle(
            new BulkRecordAttendanceCommand(a.Id, Today, [new BulkAttendanceInput(other.Id, "present", null)]), default);
        await run.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Attendance_bulk_records_once_per_day_and_skips_existing()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var e1 = Enr(a.Id); var e2 = Enr(a.Id);
        await ctx.SeedAsync(camp, a, e1, e2);

        var inputs = new[] { new BulkAttendanceInput(e1.Id, "present", null), new BulkAttendanceInput(e2.Id, "absent", null) };
        (await ctx.Handler<BulkRecordAttendanceCommandHandler>().Handle(new BulkRecordAttendanceCommand(a.Id, Today, inputs), default)).Should().Be(2);
        (await ctx.Handler<BulkRecordAttendanceCommandHandler>().Handle(new BulkRecordAttendanceCommand(a.Id, Today, inputs), default)).Should().Be(0);
        (await ctx.Db.CampAttendances.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Attendance_correction_works_while_enrolled_and_is_blocked_after_completion()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var e = Enr(a.Id);
        var rec = CampAttendance.Record(TenantA, e.Id, Today, "absent");
        await ctx.SeedAsync(camp, a, e, rec);

        await ctx.Handler<UpdateAttendanceCommandHandler>().Handle(new UpdateAttendanceCommand(rec.Id, "excused", null), default);
        (await ctx.Db.CampAttendances.SingleAsync()).Status.Should().Be("excused");

        await CompleteAsync(ctx, e.Id);
        await FluentActions.Awaiting(() => ctx.Handler<UpdateAttendanceCommandHandler>()
            .Handle(new UpdateAttendanceCommand(rec.Id, "present", null), default)).Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.CampAttendances.SingleAsync()).Status.Should().Be("excused");
    }

    [Fact]
    public async Task Attendance_list_can_be_filtered_by_period_and_day()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, b) = NewCamp();
        var ea = Enr(a.Id); var eb = Enr(b.Id);
        await ctx.SeedAsync(camp, a, b, ea, eb,
            CampAttendance.Record(TenantA, ea.Id, Today, "present"),
            CampAttendance.Record(TenantA, ea.Id, Today.AddDays(-1), "absent"),
            CampAttendance.Record(TenantA, eb.Id, Today, "present"));

        var res = await ctx.Handler<GetCampAttendanceQueryHandler>().Handle(
            new GetCampAttendanceQuery { CampPeriodId = a.Id, DateFrom = Today, DateTo = Today, PageSize = 50 }, default);

        res.Items.Should().ContainSingle().Which.CampEnrollmentId.Should().Be(ea.Id);
    }

    // ── Kamp sonu raporu ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("enrolled")]
    [InlineData("waitlist")]
    [InlineData("withdrawn")]
    public async Task Report_requires_completed_enrollment(string status)
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var e = Enr(a.Id, status);
        await ctx.SeedAsync(camp, a, e);

        var run = () => ctx.Handler<CreateCampReportCommandHandler>().Handle(
            new CreateCampReportCommand(e.Id, "Güzel bir kamptı."), default);
        var ex = await run.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Message.Should().NotBeNullOrEmpty();
        (await ctx.Db.CampReports.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Report_for_completed_enrollment_is_created_and_listed()
    {
        using var ctx = new TenantTestContext();
        var (camp, a, _) = NewCamp();
        var e = Enr(a.Id, "completed");
        await ctx.SeedAsync(camp, a, e);

        var dto = await ctx.Handler<CreateCampReportCommandHandler>().Handle(
            new CreateCampReportCommand(e.Id, "Güzel bir kamptı."), default);
        dto.AuthoredBy.Should().Be(ctx.UserId);

        var list = await ctx.Handler<GetCampReportsQueryHandler>().Handle(new GetCampReportsQuery(e.Id), default);
        list.Should().ContainSingle().Which.Summary.Should().Be("Güzel bir kamptı.");
    }

    [Fact]
    public async Task Report_for_unknown_enrollment_is_not_found()
    {
        using var ctx = new TenantTestContext();
        var run = () => ctx.Handler<CreateCampReportCommandHandler>().Handle(
            new CreateCampReportCommand(Guid.CreateVersion7(), "x"), default);
        await run.Should().ThrowAsync<NotFoundException>();
    }
}
