using Aynesil.Application.Features.Camps.Commands;
using Aynesil.Domain.Modules.Camps.Entities;
using Aynesil.Domain.Modules.Educators.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;
using CampRecordAttendance = Aynesil.Application.Features.Camps.Commands.RecordAttendanceCommand;
using CampRecordAttendanceHandler = Aynesil.Application.Features.Camps.Commands.RecordAttendanceCommandHandler;
using CampBulkAttendance = Aynesil.Application.Features.Camps.Commands.BulkRecordAttendanceCommand;
using CampBulkAttendanceHandler = Aynesil.Application.Features.Camps.Commands.BulkRecordAttendanceCommandHandler;

namespace Aynesil.Tests.TenantScoping;

public class CampCommandTenantTests
{
    private static readonly DateOnly Start = new(2026, 7, 1);
    private static readonly DateOnly End = new(2026, 7, 14);
    private static readonly DateOnly Day = new(2026, 7, 2);

    private static CampPeriod Period(Guid corp) => CampPeriod.Create(corp, Guid.CreateVersion7(), "Dönem", Start, End, 20);
    private static CampEnrollment Enrollment(Guid corp, Guid periodId) => CampEnrollment.Create(corp, periodId, Guid.CreateVersion7());
    private static CampActivity Activity(Guid corp, Guid periodId) => CampActivity.Create(corp, periodId, "Etkinlik", capacity: 20);

    // ── CreateCamp ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCamp_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var dto = await ctx.Handler<CreateCampCommandHandler>().Handle(
            new CreateCampCommand("YAZ", "Yaz Kampı", null, null, null, null, 30), default);

        dto.CorporationId.Should().Be(TenantA);
        (await ctx.Db.Camps.SingleAsync()).CreatedBy.Should().Be(ctx.UserId);
    }

    // ── CreateCampActivity ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCampActivity_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantA);
        await ctx.SeedAsync(period);

        await ctx.Handler<CreateCampActivityCommandHandler>().Handle(
            new CreateCampActivityCommand(period.Id, "Yüzme", null, null, null, null, null, 10), default);

        (await ctx.Db.CampActivities.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateCampActivity_rejects_period_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantB);
        await ctx.SeedAsync(period);

        await ShouldRejectAsync(() => ctx.Handler<CreateCampActivityCommandHandler>().Handle(
            new CreateCampActivityCommand(period.Id, "Yüzme", null, null, null, null, null, 10), default));
        (await ctx.Db.CampActivities.CountAsync()).Should().Be(0);
    }

    // ── RecordAttendance (camp) ───────────────────────────────────────────────

    [Fact]
    public async Task RecordAttendance_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantA);
        var enrollment = Enrollment(TenantA, period.Id);
        await ctx.SeedAsync(period, enrollment);

        await ctx.Handler<CampRecordAttendanceHandler>().Handle(
            new CampRecordAttendance(enrollment.Id, Day, "present"), default);

        var saved = await ctx.Db.CampAttendances.SingleAsync();
        saved.CorporationId.Should().Be(TenantA);
        saved.RecordedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task RecordAttendance_rejects_enrollment_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantB);
        var enrollment = Enrollment(TenantB, period.Id);
        await ctx.SeedAsync(period, enrollment);

        await ShouldRejectAsync(() => ctx.Handler<CampRecordAttendanceHandler>().Handle(
            new CampRecordAttendance(enrollment.Id, Day, "present"), default));
        (await ctx.Db.CampAttendances.CountAsync()).Should().Be(0);
    }

    // ── BulkRecordAttendance (camp) ───────────────────────────────────────────

    [Fact]
    public async Task BulkRecordAttendance_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantA);
        var enrollment = Enrollment(TenantA, period.Id);
        await ctx.SeedAsync(period, enrollment);

        await ctx.Handler<CampBulkAttendanceHandler>().Handle(
            new CampBulkAttendance(period.Id, Day, [new BulkAttendanceInput(enrollment.Id, "present", null)]), default);

        (await ctx.Db.CampAttendances.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task BulkRecordAttendance_rejects_period_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantB);
        var enrollment = Enrollment(TenantB, period.Id);
        await ctx.SeedAsync(period, enrollment);

        await ShouldRejectAsync(() => ctx.Handler<CampBulkAttendanceHandler>().Handle(
            new CampBulkAttendance(period.Id, Day, [new BulkAttendanceInput(enrollment.Id, "present", null)]), default));
        (await ctx.Db.CampAttendances.CountAsync()).Should().Be(0);
    }

    // ── AssignCampEducator ────────────────────────────────────────────────────

    [Fact]
    public async Task AssignCampEducator_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var camp = Camp.Create(TenantA, "YAZ", "Yaz");
        var educator = Educator.Create(TenantA, "Ayşe", "Yılmaz");
        await ctx.SeedAsync(camp, educator);

        var dto = await ctx.Handler<AssignCampEducatorCommandHandler>().Handle(
            new AssignCampEducatorCommand(camp.Id, educator.Id), default);

        dto.CorporationId.Should().Be(TenantA);
        (await ctx.Db.CampEducators.SingleAsync()).AssignedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task AssignCampEducator_rejects_camp_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var camp = Camp.Create(TenantB, "YAZ", "Yaz");
        var educator = Educator.Create(TenantA, "Ayşe", "Yılmaz");
        await ctx.SeedAsync(camp, educator);

        await ShouldRejectAsync(() => ctx.Handler<AssignCampEducatorCommandHandler>().Handle(
            new AssignCampEducatorCommand(camp.Id, educator.Id), default));
        (await ctx.Db.CampEducators.CountAsync()).Should().Be(0);
    }

    // ── RegisterParticipation ─────────────────────────────────────────────────

    [Fact]
    public async Task RegisterParticipation_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantA);
        var activity = Activity(TenantA, period.Id);
        var enrollment = Enrollment(TenantA, period.Id);
        await ctx.SeedAsync(period, activity, enrollment);

        await ctx.Handler<RegisterParticipationCommandHandler>().Handle(
            new RegisterParticipationCommand(activity.Id, enrollment.Id), default);

        var saved = await ctx.Db.CampActivityParticipations.SingleAsync();
        saved.CorporationId.Should().Be(TenantA);
        saved.RecordedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task RegisterParticipation_rejects_activity_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantA);
        var foreignActivity = Activity(TenantB, Guid.CreateVersion7());
        var enrollment = Enrollment(TenantA, period.Id);
        await ctx.SeedAsync(period, foreignActivity, enrollment);

        await ShouldRejectAsync(() => ctx.Handler<RegisterParticipationCommandHandler>().Handle(
            new RegisterParticipationCommand(foreignActivity.Id, enrollment.Id), default));
        (await ctx.Db.CampActivityParticipations.CountAsync()).Should().Be(0);
    }

    // ── BulkRegisterParticipation ─────────────────────────────────────────────

    [Fact]
    public async Task BulkRegisterParticipation_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantA);
        var activity = Activity(TenantA, period.Id);
        var enrollment = Enrollment(TenantA, period.Id);
        await ctx.SeedAsync(period, activity, enrollment);

        await ctx.Handler<BulkRegisterParticipationCommandHandler>().Handle(
            new BulkRegisterParticipationCommand(activity.Id, [new BulkParticipationInput(enrollment.Id, "registered", null)]), default);

        (await ctx.Db.CampActivityParticipations.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task BulkRegisterParticipation_rejects_activity_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantB);
        var activity = Activity(TenantB, period.Id);
        var enrollment = Enrollment(TenantB, period.Id);
        await ctx.SeedAsync(period, activity, enrollment);

        await ShouldRejectAsync(() => ctx.Handler<BulkRegisterParticipationCommandHandler>().Handle(
            new BulkRegisterParticipationCommand(activity.Id, [new BulkParticipationInput(enrollment.Id, "registered", null)]), default));
        (await ctx.Db.CampActivityParticipations.CountAsync()).Should().Be(0);
    }

    // ── CreateCampReport ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCampReport_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantA);
        var enrollment = Enrollment(TenantA, period.Id);
        await ctx.SeedAsync(period, enrollment);

        await ctx.Handler<CreateCampReportCommandHandler>().Handle(
            new CreateCampReportCommand(enrollment.Id, "Özet"), default);

        var saved = await ctx.Db.CampReports.SingleAsync();
        saved.CorporationId.Should().Be(TenantA);
        saved.AuthoredBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task CreateCampReport_rejects_enrollment_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var period = Period(TenantB);
        var enrollment = Enrollment(TenantB, period.Id);
        await ctx.SeedAsync(period, enrollment);

        await ShouldRejectAsync(() => ctx.Handler<CreateCampReportCommandHandler>().Handle(
            new CreateCampReportCommand(enrollment.Id, "Özet"), default));
        (await ctx.Db.CampReports.CountAsync()).Should().Be(0);
    }
}
