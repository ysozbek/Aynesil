using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Portal.Queries;
using Aynesil.Application.Features.Students.Commands;
using Aynesil.Application.Features.Students.Queries;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Y15: veli portalı yalnızca kesinleşmiş gelişim raporlarını görür; rapor kesinleştirme akışı
/// (taslak → kesinleşmiş), kesinleşmiş rapor düzenlenemez.
/// </summary>
public class DevelopmentReportFinalizationTests
{
    private static DevelopmentReport Report(Guid studentId, string period) => new()
    {
        CorporationId = TenantA, StudentId = studentId, PeriodLabel = period, ReportDate = new DateOnly(2026, 9, 1),
        Content = "İçerik", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static TenantTestContext ContextWithStaffAccess()
    {
        var ctx = new TenantTestContext();
        // Erişim süzgeci (bakım ekibi) bu testin konusu değil: personel bypass'ı ile yalnızca taslak süzgeci ölçülür.
        ctx.CurrentUser.Setup(u => u.HasPermission("care_team:bypass")).Returns(true);
        return ctx;
    }

    /// <summary>Guardian linked to the signed-in user with portal access to the student; no care-team bypass, no assignment.</summary>
    private static async Task SeedGuardianAccessAsync(
        TenantTestContext ctx, Guid studentId, bool canViewReports, params object[] extra)
    {
        var guardian = Guardian.Create(TenantA, "Ayşe", "Kaya");
        guardian.LinkPortalAccount(ctx.UserId);
        var access = new GuardianPortalAccess
        {
            CorporationId = TenantA, GuardianId = guardian.Id, StudentId = studentId, CanViewReports = canViewReports,
        };
        await ctx.SeedAsync([guardian, access, .. extra]);
    }

    [Fact]
    public async Task A_guardian_without_care_team_access_sees_finalized_reports()
    {
        // Regression: the portal used the staff query, whose care-team filter hid every report from guardians.
        using var ctx = new TenantTestContext();   // no care_team:bypass, no care assignment
        var studentId = Guid.CreateVersion7();
        var published = Report(studentId, "Yayımlanan");
        published.Finalize(null);
        await SeedGuardianAccessAsync(ctx, studentId, canViewReports: true, Report(studentId, "Taslak"), published);

        var portal = await ctx.Handler<GetPortalDevelopmentReportsQueryHandler>()
            .Handle(new GetPortalDevelopmentReportsQuery(studentId, ctx.UserId), default);

        portal.Should().ContainSingle().Which.PeriodLabel.Should().Be("Yayımlanan");
    }

    [Fact]
    public async Task Portal_rejects_another_guardians_child_and_respects_CanViewReports()
    {
        using var ctx = new TenantTestContext();
        var studentId = Guid.CreateVersion7();
        var report = Report(studentId, "Yayımlanan");
        report.Finalize(null);
        await SeedGuardianAccessAsync(ctx, studentId, canViewReports: false, report);

        var handler = ctx.Handler<GetPortalDevelopmentReportsQueryHandler>();
        (await handler.Handle(new GetPortalDevelopmentReportsQuery(studentId, ctx.UserId), default)).Should().BeEmpty();
        await handler.Invoking(h => h.Handle(new GetPortalDevelopmentReportsQuery(Guid.CreateVersion7(), ctx.UserId), default))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Draft_report_is_hidden_from_the_portal_but_visible_to_staff()
    {
        using var ctx = ContextWithStaffAccess();
        var studentId = Guid.CreateVersion7();
        var draft = Report(studentId, "Taslak dönem");
        var published = Report(studentId, "Yayımlanan dönem");
        published.Finalize(ctx.UserId);
        await SeedGuardianAccessAsync(ctx, studentId, canViewReports: true, draft, published);

        var staff = await ctx.Handler<GetDevelopmentReportsQueryHandler>().Handle(new GetDevelopmentReportsQuery(studentId), default);
        var portal = await ctx.Handler<GetPortalDevelopmentReportsQueryHandler>()
            .Handle(new GetPortalDevelopmentReportsQuery(studentId, ctx.UserId), default);

        portal.Should().ContainSingle().Which.PeriodLabel.Should().Be("Yayımlanan dönem");
        portal.Single().IsFinalized.Should().BeTrue();
        staff.Select(r => r.PeriodLabel).Should().BeEquivalentTo("Taslak dönem", "Yayımlanan dönem");
        staff.Single(r => r.PeriodLabel == "Taslak dönem").IsFinalized.Should().BeFalse();
    }

    [Fact]
    public async Task Finalizing_makes_the_report_visible_to_the_portal_and_records_who_and_when()
    {
        using var ctx = ContextWithStaffAccess();
        var studentId = Guid.CreateVersion7();
        var report = Report(studentId, "Güz");
        await SeedGuardianAccessAsync(ctx, studentId, canViewReports: true, report);

        var dto = await ctx.Handler<FinalizeDevelopmentReportCommandHandler>()
            .Handle(new FinalizeDevelopmentReportCommand(report.Id, report.RowVersion), default);

        dto.IsFinalized.Should().BeTrue();
        dto.FinalizedAt.Should().NotBeNull();
        var stored = await ctx.Db.DevelopmentReports.SingleAsync();
        stored.FinalizedBy.Should().Be(ctx.UserId);

        var portal = await ctx.Handler<GetPortalDevelopmentReportsQueryHandler>()
            .Handle(new GetPortalDevelopmentReportsQuery(studentId, ctx.UserId), default);
        portal.Should().ContainSingle();
    }

    [Fact]
    public async Task A_finalized_report_cannot_be_edited_or_finalized_again()
    {
        using var ctx = ContextWithStaffAccess();
        var report = Report(Guid.CreateVersion7(), "Güz");
        report.Finalize(ctx.UserId);
        await ctx.SeedAsync(report);

        var edit = await ctx.Handler<UpdateDevelopmentReportCommandHandler>()
            .Invoking(h => h.Handle(new UpdateDevelopmentReportCommand(
                report.Id, "Değişti", null, "Yeni içerik", null, report.RowVersion), default))
            .Should().ThrowAsync<BusinessRuleViolationException>();
        edit.Which.Code.Should().Be(MsgKeys.Rule.DevelopmentReportFinalized);

        var again = await ctx.Handler<FinalizeDevelopmentReportCommandHandler>()
            .Invoking(h => h.Handle(new FinalizeDevelopmentReportCommand(report.Id, report.RowVersion), default))
            .Should().ThrowAsync<BusinessRuleViolationException>();
        again.Which.Code.Should().Be(MsgKeys.Rule.DevelopmentReportAlreadyFinalized);

        (await ctx.Db.DevelopmentReports.SingleAsync()).Content.Should().Be("İçerik");
    }

    [Fact]
    public async Task New_reports_start_as_drafts()
    {
        using var ctx = ContextWithStaffAccess();
        var student = Student.Create(TenantA, "Ali", "Veli");
        await ctx.SeedAsync(student);

        var dto = await ctx.Handler<AddDevelopmentReportCommandHandler>()
            .Handle(new AddDevelopmentReportCommand(student.Id, "Güz", null, "İçerik", null), default);

        dto.IsFinalized.Should().BeFalse();
    }
}
