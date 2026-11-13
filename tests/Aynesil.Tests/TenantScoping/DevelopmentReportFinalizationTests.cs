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

    [Fact]
    public async Task Draft_report_is_hidden_from_the_portal_but_visible_to_staff()
    {
        using var ctx = ContextWithStaffAccess();
        var studentId = Guid.CreateVersion7();
        var draft = Report(studentId, "Taslak dönem");
        var published = Report(studentId, "Yayımlanan dönem");
        published.Finalize(ctx.UserId);
        await ctx.SeedAsync(draft, published);

        var handler = ctx.Handler<GetDevelopmentReportsQueryHandler>();
        var portal = await handler.Handle(new GetDevelopmentReportsQuery(studentId, FinalizedOnly: true), default);
        var staff = await handler.Handle(new GetDevelopmentReportsQuery(studentId), default);

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
        await ctx.SeedAsync(report);

        var dto = await ctx.Handler<FinalizeDevelopmentReportCommandHandler>()
            .Handle(new FinalizeDevelopmentReportCommand(report.Id, report.RowVersion), default);

        dto.IsFinalized.Should().BeTrue();
        dto.FinalizedAt.Should().NotBeNull();
        var stored = await ctx.Db.DevelopmentReports.SingleAsync();
        stored.FinalizedBy.Should().Be(ctx.UserId);

        var portal = await ctx.Handler<GetDevelopmentReportsQueryHandler>()
            .Handle(new GetDevelopmentReportsQuery(studentId, FinalizedOnly: true), default);
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
