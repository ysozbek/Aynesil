using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Camps.Commands;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Camps.Entities;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// docs/raporlar/2026-11-17_dogrulama.md D3–D5:
///   D3 çekilmiş öğrenci aynı döneme yeniden kaydedilir (mevcut satır geri açılır; dolu dönemde bekleme listesine düşer);
///   D4 tekil tamamlama başlamamış dönemde 409;
///   D5 kayda bağlanan paket kaydın öğrencisine ait olmalı (oluşturma ve güncelleme; aksi 422).
/// D6 (dönem satırı kilidi) ilişkisel sağlayıcıda çalışır; entegrasyon testi CampEnrollmentConcurrencyTests.
/// </summary>
public class CampEnrollmentRulesTests
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static (Camp Camp, CampPeriod Period) NewPeriod(int? capacity = 5, int startsInDays = -1)
    {
        var camp = Camp.Create(TenantA, "yaz", "Yaz Kampı");
        var period = CampPeriod.Create(TenantA, camp.Id, "Dönem", Today.AddDays(startsInDays), Today.AddDays(startsInDays + 14), capacity);
        return (camp, period);
    }

    private static EnrollStudentCommand Enroll(Guid periodId, Guid studentId, string status = "enrolled", Guid? package = null)
        => new(periodId, studentId, status, package);

    // ── D3 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Withdrawn_student_is_re_enrolled_on_the_same_row()
    {
        using var ctx = new TenantTestContext();
        var (camp, period) = NewPeriod();
        var student = Guid.CreateVersion7();
        var old = CampEnrollment.Create(TenantA, period.Id, student);
        old.Withdraw();
        await ctx.SeedAsync(camp, period, old);

        var dto = await ctx.Handler<EnrollStudentCommandHandler>().Handle(Enroll(period.Id, student), default);

        dto.Id.Should().Be(old.Id);
        dto.Status.Should().Be("enrolled");
        (await ctx.Db.CampEnrollments.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Re_enrolling_into_a_full_period_falls_to_the_waitlist()
    {
        using var ctx = new TenantTestContext();
        var (camp, period) = NewPeriod(capacity: 1);
        var student = Guid.CreateVersion7();
        var old = CampEnrollment.Create(TenantA, period.Id, student);
        old.Withdraw();
        var seatTaker = CampEnrollment.Create(TenantA, period.Id, Guid.CreateVersion7());
        await ctx.SeedAsync(camp, period, old, seatTaker);

        var dto = await ctx.Handler<EnrollStudentCommandHandler>().Handle(Enroll(period.Id, student), default);

        dto.Id.Should().Be(old.Id);
        dto.Status.Should().Be("waitlist");
    }

    [Fact]
    public async Task A_new_enrollment_into_a_full_period_is_still_409()
    {
        using var ctx = new TenantTestContext();
        var (camp, period) = NewPeriod(capacity: 1);
        await ctx.SeedAsync(camp, period, CampEnrollment.Create(TenantA, period.Id, Guid.CreateVersion7()));

        var act = () => ctx.Handler<EnrollStudentCommandHandler>().Handle(Enroll(period.Id, Guid.CreateVersion7()), default);
        (await act.Should().ThrowAsync<BusinessRuleViolationException>()).Which.Code.Should().Be(MsgKeys.Rule.CampPeriodFullUseWaitlist);
    }

    [Theory]
    [InlineData("enrolled")]
    [InlineData("waitlist")]
    [InlineData("completed")]
    public async Task Active_or_completed_enrollment_blocks_a_second_one(string status)
    {
        using var ctx = new TenantTestContext();
        var (camp, period) = NewPeriod();
        var student = Guid.CreateVersion7();
        var existing = CampEnrollment.Create(TenantA, period.Id, student, status == "completed" ? "enrolled" : status);
        if (status == "completed") existing.Complete();
        await ctx.SeedAsync(camp, period, existing);

        var act = () => ctx.Handler<EnrollStudentCommandHandler>().Handle(Enroll(period.Id, student), default);
        (await act.Should().ThrowAsync<BusinessRuleViolationException>()).Which.Code.Should().Be(MsgKeys.Rule.StudentAlreadyInCampPeriod);
        (await ctx.Db.CampEnrollments.CountAsync()).Should().Be(1);
    }

    // ── D4 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Single_completion_before_the_period_starts_is_409()
    {
        using var ctx = new TenantTestContext();
        var (camp, period) = NewPeriod(startsInDays: 3);
        var e = CampEnrollment.Create(TenantA, period.Id, Guid.CreateVersion7());
        await ctx.SeedAsync(camp, period, e);

        var act = () => ctx.Handler<CompleteEnrollmentCommandHandler>().Handle(new CompleteEnrollmentCommand(e.Id), default);
        (await act.Should().ThrowAsync<BusinessRuleViolationException>()).Which.Code.Should().Be(MsgKeys.Rule.CampEnrollmentCompleteNotStarted);
        (await ctx.Db.CampEnrollments.AsNoTracking().SingleAsync()).Status.Should().Be("enrolled");
    }

    [Fact]
    public async Task Single_completion_on_the_first_day_is_allowed()
    {
        using var ctx = new TenantTestContext();
        var (camp, period) = NewPeriod(startsInDays: 0);
        var e = CampEnrollment.Create(TenantA, period.Id, Guid.CreateVersion7());
        await ctx.SeedAsync(camp, period, e);

        await ctx.Handler<CompleteEnrollmentCommandHandler>().Handle(new CompleteEnrollmentCommand(e.Id), default);
        (await ctx.Db.CampEnrollments.AsNoTracking().SingleAsync()).Status.Should().Be("completed");
    }

    // ── D5 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Package_of_another_student_is_rejected_on_enroll_and_on_update()
    {
        using var ctx = new TenantTestContext();
        var (camp, period) = NewPeriod();
        var student = Guid.CreateVersion7();
        var own = StudentPackage.Purchase(TenantA, student, 10, 1000);
        var foreign = StudentPackage.Purchase(TenantA, Guid.CreateVersion7(), 10, 1000);
        await ctx.SeedAsync(camp, period, own, foreign);

        var bad = () => ctx.Handler<EnrollStudentCommandHandler>().Handle(Enroll(period.Id, student, package: foreign.Id), default);
        (await bad.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("studentPackageId");
        (await ctx.Db.CampEnrollments.CountAsync()).Should().Be(0);

        var dto = await ctx.Handler<EnrollStudentCommandHandler>().Handle(Enroll(period.Id, student, package: own.Id), default);
        dto.StudentPackageId.Should().Be(own.Id);

        var badUpdate = () => ctx.Handler<UpdateEnrollmentPackageCommandHandler>()
            .Handle(new UpdateEnrollmentPackageCommand(dto.Id, foreign.Id), default);
        await badUpdate.Should().ThrowAsync<ValidationException>();
        (await ctx.Db.CampEnrollments.AsNoTracking().SingleAsync()).StudentPackageId.Should().Be(own.Id);

        await ctx.Handler<UpdateEnrollmentPackageCommandHandler>().Handle(new UpdateEnrollmentPackageCommand(dto.Id, null), default);
        (await ctx.Db.CampEnrollments.AsNoTracking().SingleAsync()).StudentPackageId.Should().BeNull();
    }
}
