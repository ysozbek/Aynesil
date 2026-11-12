using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Portal.Queries;
using Aynesil.Application.Features.Students.Queries;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// A signed-in guardian asking for a student they have no portal access to (another guardian's
/// child, another tenant's student, revoked access) is an authorization denial: 403, not 401.
/// 401 is reserved for "no/invalid session" and makes the frontend drop the login.
/// </summary>
public class PortalAccessDenialTests
{
    private static (Guardian Guardian, GuardianPortalAccess Access, Guid StudentId) OwnChild(
        TenantTestContext ctx, Guid corporationId = default)
    {
        var corp = corporationId == default ? TenantA : corporationId;
        var guardian = Guardian.Create(corp, "Ayşe", "Kaya");
        guardian.LinkPortalAccount(ctx.UserId);
        var studentId = Guid.CreateVersion7();
        var access = new GuardianPortalAccess { CorporationId = corp, GuardianId = guardian.Id, StudentId = studentId };
        return (guardian, access, studentId);
    }

    [Fact]
    public async Task Student_summary_of_another_guardians_child_is_forbidden()
    {
        using var ctx = new TenantTestContext();
        var (guardian, access, _) = OwnChild(ctx);
        var other = Guardian.Create(TenantA, "Başka", "Veli");
        var othersStudentId = Guid.CreateVersion7();
        var othersAccess = new GuardianPortalAccess { CorporationId = TenantA, GuardianId = other.Id, StudentId = othersStudentId };
        await ctx.SeedAsync(guardian, access, other, othersAccess);

        var act = () => ctx.Handler<GetPortalStudentSummaryQueryHandler>().Handle(
            new GetPortalStudentSummaryQuery(othersStudentId, ctx.UserId), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Student_summary_of_another_tenants_student_is_forbidden()
    {
        using var ctx = new TenantTestContext();
        var (guardian, access, _) = OwnChild(ctx);
        var foreignStudentId = Guid.CreateVersion7();
        var foreignGuardian = Guardian.Create(TenantB, "Yabancı", "Veli");
        var foreignAccess = new GuardianPortalAccess { CorporationId = TenantB, GuardianId = foreignGuardian.Id, StudentId = foreignStudentId };
        await ctx.SeedAsync(guardian, access, foreignGuardian, foreignAccess);

        var act = () => ctx.Handler<GetPortalStudentSummaryQueryHandler>().Handle(
            new GetPortalStudentSummaryQuery(foreignStudentId, ctx.UserId), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Extended_portal_queries_forbid_a_student_without_access()
    {
        using var ctx = new TenantTestContext();
        var (guardian, access, _) = OwnChild(ctx);
        await ctx.SeedAsync(guardian, access);

        var act = () => ctx.Handler<GetPortalDashboardQueryHandler>().Handle(
            new GetPortalDashboardQuery(Guid.CreateVersion7(), ctx.UserId), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Own_child_is_still_served()
    {
        using var ctx = new TenantTestContext();
        var (guardian, access, studentId) = OwnChild(ctx);
        var student = Student.Create(TenantA, "Çocuk", "Kaya");
        typeof(Student).GetProperty(nameof(student.Id))!.SetValue(student, studentId);
        await ctx.SeedAsync(guardian, access, student);

        var dto = await ctx.Handler<GetPortalStudentSummaryQueryHandler>().Handle(
            new GetPortalStudentSummaryQuery(studentId, ctx.UserId), default);

        dto.StudentId.Should().Be(studentId);
    }
}
