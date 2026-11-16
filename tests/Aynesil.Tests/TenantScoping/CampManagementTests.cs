using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Camps.Commands;
using Aynesil.Application.Features.Camps.Queries;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Camps.Entities;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Kamp yönetimi: etkinleştir/pasifleştir/sil, dönem düzenle/sil, bekleme listesi, toplu tamamlama,
/// eğitmen atama/rol/kaldırma. Bağlı kayıt varsa BusinessRuleViolationException (HTTP 409) beklenir.
/// </summary>
public class CampManagementTests
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static (Camp Camp, CampPeriod Past, CampPeriod Future) NewCamp()
    {
        var camp = Camp.Create(TenantA, "yaz", "Yaz Kampı");
        var past = CampPeriod.Create(TenantA, camp.Id, "Geçmiş", Today.AddDays(-30), Today.AddDays(-16), 5);
        var future = CampPeriod.Create(TenantA, camp.Id, "Gelecek", Today.AddDays(10), Today.AddDays(24), 5);
        return (camp, past, future);
    }

    private static CampEnrollment Enr(Guid periodId, string status = "enrolled") =>
        CampEnrollment.Create(TenantA, periodId, Guid.CreateVersion7(), status);

    // ── Kamp etkinleştir / pasifleştir / sil ──────────────────────────────────

    [Fact]
    public async Task Deactivate_then_activate_toggles_flag()
    {
        using var ctx = new TenantTestContext();
        var (camp, past, _) = NewCamp();
        await ctx.SeedAsync(camp, past);

        await ctx.Handler<DeactivateCampCommandHandler>().Handle(new DeactivateCampCommand(camp.Id), default);
        (await ctx.Db.Camps.SingleAsync()).IsActive.Should().BeFalse();

        await ctx.Handler<ActivateCampCommandHandler>().Handle(new ActivateCampCommand(camp.Id), default);
        (await ctx.Db.Camps.SingleAsync()).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_is_blocked_by_enrollment_in_upcoming_period()
    {
        using var ctx = new TenantTestContext();
        var (camp, past, future) = NewCamp();
        await ctx.SeedAsync(camp, past, future, Enr(future.Id, "waitlist"));

        var act = () => ctx.Handler<DeactivateCampCommandHandler>().Handle(new DeactivateCampCommand(camp.Id), default);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.Camps.SingleAsync()).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_ignores_completed_history_of_finished_periods()
    {
        using var ctx = new TenantTestContext();
        var (camp, past, _) = NewCamp();
        var e = Enr(past.Id); e.Complete();
        await ctx.SeedAsync(camp, past, e);

        await ctx.Handler<DeactivateCampCommandHandler>().Handle(new DeactivateCampCommand(camp.Id), default);
        (await ctx.Db.Camps.SingleAsync()).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Activate_when_already_active_conflicts()
    {
        using var ctx = new TenantTestContext();
        var (camp, _, _) = NewCamp();
        await ctx.SeedAsync(camp);

        var act = () => ctx.Handler<ActivateCampCommandHandler>().Handle(new ActivateCampCommand(camp.Id), default);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Theory]
    [InlineData("enrolled")]
    [InlineData("waitlist")]
    public async Task Delete_is_blocked_by_enrolled_or_waitlisted_students(string status)
    {
        using var ctx = new TenantTestContext();
        var (camp, past, _) = NewCamp();
        await ctx.SeedAsync(camp, past, Enr(past.Id, status));

        var act = () => ctx.Handler<DeleteCampCommandHandler>().Handle(new DeleteCampCommand(camp.Id), default);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.Camps.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Delete_soft_deletes_camp_without_active_enrollments()
    {
        using var ctx = new TenantTestContext();
        var (camp, past, _) = NewCamp();
        var done = Enr(past.Id); done.Complete();
        await ctx.SeedAsync(camp, past, done);

        await ctx.Handler<DeleteCampCommandHandler>().Handle(new DeleteCampCommand(camp.Id), default);
        (await ctx.Db.Camps.IgnoreQueryFilters().SingleAsync()).DeletedAt.Should().NotBeNull();
    }

    // ── Dönem düzenle / sil ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePeriod_changes_fields()
    {
        using var ctx = new TenantTestContext();
        var (camp, _, future) = NewCamp();
        await ctx.SeedAsync(camp, future);

        await ctx.Handler<UpdateCampPeriodCommandHandler>().Handle(
            new UpdateCampPeriodCommand(future.Id, "Yeni Ad", Today.AddDays(11), Today.AddDays(20), 8), default);

        var saved = await ctx.Db.CampPeriods.SingleAsync();
        saved.Name.Should().Be("Yeni Ad");
        saved.Capacity.Should().Be(8);
    }

    [Fact]
    public async Task UpdatePeriod_capacity_below_enrolled_count_conflicts()
    {
        using var ctx = new TenantTestContext();
        var (camp, _, future) = NewCamp();
        await ctx.SeedAsync(camp, future, Enr(future.Id), Enr(future.Id), Enr(future.Id));

        var act = () => ctx.Handler<UpdateCampPeriodCommandHandler>().Handle(
            new UpdateCampPeriodCommand(future.Id, future.Name, future.StartDate, future.EndDate, 2), default);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task DeletePeriod_is_blocked_by_enrollments_and_by_activities()
    {
        using var ctx = new TenantTestContext();
        var (camp, past, future) = NewCamp();
        await ctx.SeedAsync(camp, past, future, Enr(past.Id), CampActivity.Create(TenantA, future.Id, "Yüzme", capacity: 5));

        var h = ctx.Handler<DeleteCampPeriodCommandHandler>();
        (await FluentActions.Awaiting(() => h.Handle(new DeleteCampPeriodCommand(past.Id), default))
            .Should().ThrowAsync<BusinessRuleViolationException>()).Which.Message.Should().NotBeNullOrEmpty();
        await FluentActions.Awaiting(() => ctx.Handler<DeleteCampPeriodCommandHandler>()
            .Handle(new DeleteCampPeriodCommand(future.Id), default)).Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.CampPeriods.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task DeletePeriod_removes_empty_period()
    {
        using var ctx = new TenantTestContext();
        var (camp, past, future) = NewCamp();
        await ctx.SeedAsync(camp, past, future);

        await ctx.Handler<DeleteCampPeriodCommandHandler>().Handle(new DeleteCampPeriodCommand(future.Id), default);
        (await ctx.Db.CampPeriods.CountAsync()).Should().Be(1);
    }

    // ── Kayıt: bekleme listesi / tamamla / toplu tamamla ──────────────────────

    [Fact]
    public async Task MoveToWaitlist_then_promote_round_trips()
    {
        using var ctx = new TenantTestContext();
        var (camp, _, future) = NewCamp();
        var e = Enr(future.Id);
        await ctx.SeedAsync(camp, future, e);

        await ctx.Handler<MoveToWaitlistCommandHandler>().Handle(new MoveToWaitlistCommand(e.Id), default);
        (await ctx.Db.CampEnrollments.SingleAsync()).Status.Should().Be("waitlist");

        await ctx.Handler<PromoteFromWaitlistCommandHandler>().Handle(new PromoteFromWaitlistCommand(e.Id), default);
        (await ctx.Db.CampEnrollments.SingleAsync()).Status.Should().Be("enrolled");
    }

    [Theory]
    [InlineData("waitlist")]
    [InlineData("completed")]
    [InlineData("withdrawn")]
    public async Task MoveToWaitlist_requires_enrolled_status(string status)
    {
        using var ctx = new TenantTestContext();
        var (camp, _, future) = NewCamp();
        var e = Enr(future.Id);
        switch (status)
        {
            case "waitlist": e.MoveToWaitlist(); break;
            case "completed": e.Complete(); break;
            default: e.Withdraw(); break;
        }
        await ctx.SeedAsync(camp, future, e);

        var act = () => ctx.Handler<MoveToWaitlistCommandHandler>().Handle(new MoveToWaitlistCommand(e.Id), default);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Complete_requires_enrolled_status()
    {
        using var ctx = new TenantTestContext();
        var (camp, _, future) = NewCamp();
        var e = Enr(future.Id, "waitlist");
        await ctx.SeedAsync(camp, future, e);

        var act = () => ctx.Handler<CompleteEnrollmentCommandHandler>().Handle(new CompleteEnrollmentCommand(e.Id), default);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Enroll_into_inactive_camp_conflicts()
    {
        using var ctx = new TenantTestContext();
        var (camp, _, future) = NewCamp();
        camp.Deactivate();
        await ctx.SeedAsync(camp, future);

        var act = () => ctx.Handler<EnrollStudentCommandHandler>().Handle(
            new EnrollStudentCommand(future.Id, Guid.CreateVersion7()), default);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task BulkComplete_completes_only_enrolled_students_and_returns_count()
    {
        using var ctx = new TenantTestContext();
        var (camp, past, _) = NewCamp();
        await ctx.SeedAsync(camp, past, Enr(past.Id), Enr(past.Id), Enr(past.Id, "waitlist"));

        var count = await ctx.Handler<BulkCompleteEnrollmentsCommandHandler>()
            .Handle(new BulkCompleteEnrollmentsCommand(past.Id), default);

        count.Should().Be(2);
        (await ctx.Db.CampEnrollments.CountAsync(e => e.Status == "completed")).Should().Be(2);
        (await ctx.Db.CampEnrollments.CountAsync(e => e.Status == "waitlist")).Should().Be(1);
    }

    [Fact]
    public async Task BulkComplete_before_period_start_conflicts_and_unknown_period_is_404()
    {
        using var ctx = new TenantTestContext();
        var (camp, _, future) = NewCamp();
        await ctx.SeedAsync(camp, future, Enr(future.Id));

        await FluentActions.Awaiting(() => ctx.Handler<BulkCompleteEnrollmentsCommandHandler>()
            .Handle(new BulkCompleteEnrollmentsCommand(future.Id), default)).Should().ThrowAsync<BusinessRuleViolationException>();
        await FluentActions.Awaiting(() => ctx.Handler<BulkCompleteEnrollmentsCommandHandler>()
            .Handle(new BulkCompleteEnrollmentsCommand(Guid.CreateVersion7()), default)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Enrollment_list_returns_student_name()
    {
        using var ctx = new TenantTestContext();
        var (camp, past, _) = NewCamp();
        var student = Student.Create(TenantA, "Can", "Kaya");
        var e = CampEnrollment.Create(TenantA, past.Id, student.Id);
        await ctx.SeedAsync(camp, past, student, e);

        var page = await ctx.Handler<GetCampEnrollmentsQueryHandler>()
            .Handle(new GetCampEnrollmentsQuery { CampPeriodId = past.Id }, default);
        page.Items.Single().StudentName.Should().Be("Can Kaya");
    }

    // ── Eğitmen atama / rol / kaldırma ────────────────────────────────────────

    [Fact]
    public async Task Educator_assign_change_role_and_remove()
    {
        using var ctx = new TenantTestContext();
        var (camp, past, _) = NewCamp();
        var educator = Educator.Create(TenantA, "Ece", "Yılmaz");
        await ctx.SeedAsync(camp, past, educator);

        var dto = await ctx.Handler<AssignCampEducatorCommandHandler>().Handle(
            new AssignCampEducatorCommand(camp.Id, educator.Id, "assistant"), default);
        dto.Role.Should().Be("assistant");

        await ctx.Handler<UpdateCampEducatorRoleCommandHandler>().Handle(new UpdateCampEducatorRoleCommand(dto.Id, "lead"), default);
        (await ctx.Db.CampEducators.SingleAsync()).Role.Should().Be("lead");

        var listed = await ctx.Handler<GetCampEducatorsQueryHandler>()
            .Handle(new GetCampEducatorsQuery { CampId = camp.Id }, default);
        listed.Single().EducatorName.Should().Be("Ece Yılmaz");

        await ctx.Handler<RemoveCampEducatorCommandHandler>().Handle(new RemoveCampEducatorCommand(dto.Id), default);
        (await ctx.Db.CampEducators.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Educator_assign_twice_to_same_scope_conflicts()
    {
        using var ctx = new TenantTestContext();
        var (camp, _, _) = NewCamp();
        var educator = Educator.Create(TenantA, "Ece", "Yılmaz");
        await ctx.SeedAsync(camp, educator);

        await ctx.Handler<AssignCampEducatorCommandHandler>().Handle(new AssignCampEducatorCommand(camp.Id, educator.Id), default);
        await FluentActions.Awaiting(() => ctx.Handler<AssignCampEducatorCommandHandler>()
            .Handle(new AssignCampEducatorCommand(camp.Id, educator.Id), default)).Should().ThrowAsync<BusinessRuleViolationException>();
    }
}
