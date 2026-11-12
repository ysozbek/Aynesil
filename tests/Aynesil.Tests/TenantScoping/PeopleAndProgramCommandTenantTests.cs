using Aynesil.Application.Features.Cameras.Commands;
using Aynesil.Application.Features.Campuses.Commands;
using Aynesil.Application.Features.CareTeam.Commands;
using Aynesil.Application.Features.Educators.Commands;
using Aynesil.Application.Features.Leads.Commands;
using Aynesil.Application.Features.Programs.Commands;
using Aynesil.Application.Features.Students.Commands;
using Aynesil.Domain.Modules.Education.Entities;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Students, guardians, educators, campuses, cameras, leads, programs and care team.</summary>
public class PeopleAndProgramCommandTenantTests
{
    private static Student NewStudent(Guid corp) => Student.Create(corp, "Ali", "Kaya");

    // ── No-parent creates ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateStudent_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var dto = await ctx.Handler<CreateStudentCommandHandler>().Handle(
            new CreateStudentCommand("Ali", "Kaya", null, null, null, null, null, null, null, null), default);
        (await ctx.Db.Students.SingleAsync(s => s.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateGuardian_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var dto = await ctx.Handler<CreateGuardianCommandHandler>().Handle(
            new CreateGuardianCommand("Ayşe", "Kaya", null, null, null, null, null), default);
        (await ctx.Db.Guardians.SingleAsync(g => g.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateEducator_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var dto = await ctx.Handler<CreateEducatorCommandHandler>().Handle(
            new CreateEducatorCommand("Zeynep", "Ak", null, null, null, null, null, null), default);
        (await ctx.Db.Educators.SingleAsync(e => e.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateCampus_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.SeedAsync(NewCorporation(TenantA));

        var dto = await ctx.Handler<CreateCampusCommandHandler>().Handle(
            new CreateCampusCommand("MRK", "Merkez", null, null, null, null, null, null, null, null), default);
        (await ctx.Db.Campuses.SingleAsync(c => c.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task RegisterCamera_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var id = await ctx.Handler<RegisterCameraCommandHandler>().Handle(
            new RegisterCameraCommand("CAM-1", "Kamera", null, null, null, null), default);
        var saved = await ctx.Db.Cameras.SingleAsync(c => c.Id == id);
        saved.CorporationId.Should().Be(TenantA);
        saved.CreatedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task CreateLead_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var dto = await ctx.Handler<CreateLeadCommandHandler>().Handle(
            new CreateLeadCommand("Veli Adı", null, null, null, null, null, null, null, null, null, null, null, null), default);
        (await ctx.Db.Leads.SingleAsync(l => l.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateProgram_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var dto = await ctx.Handler<CreateProgramCommandHandler>().Handle(
            new CreateProgramCommand("DKT", "Dil ve Konuşma", null, null), default);
        (await ctx.Db.EducationPrograms.SingleAsync(p => p.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    // ── CreateEnrollment ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateEnrollment_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        await ctx.SeedAsync(student);

        await ctx.Handler<CreateEnrollmentCommandHandler>().Handle(
            new CreateEnrollmentCommand(student.Id, null, null, null), default);

        (await ctx.Db.Enrollments.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateEnrollment_rejects_student_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantB);
        await ctx.SeedAsync(student);

        await ShouldRejectAsync(() => ctx.Handler<CreateEnrollmentCommandHandler>().Handle(
            new CreateEnrollmentCommand(student.Id, null, null, null), default));
        (await ctx.Db.Enrollments.CountAsync()).Should().Be(0);
    }

    // ── AssignStudentToProgram ────────────────────────────────────────────────

    [Fact]
    public async Task AssignStudentToProgram_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        var program = EducationProgram.Create(TenantA, "DKT", "Dil");
        await ctx.SeedAsync(student, program);

        await ctx.Handler<AssignStudentToProgramCommandHandler>().Handle(
            new AssignStudentToProgramCommand(student.Id, program.Id, null, null, null, null), default);

        (await ctx.Db.StudentPrograms.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task AssignStudentToProgram_rejects_program_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        var program = EducationProgram.Create(TenantB, "DKT", "Dil");
        await ctx.SeedAsync(student, program);

        await ShouldRejectAsync(() => ctx.Handler<AssignStudentToProgramCommandHandler>().Handle(
            new AssignStudentToProgramCommand(student.Id, program.Id, null, null, null, null), default));
        (await ctx.Db.StudentPrograms.CountAsync()).Should().Be(0);
    }

    // ── AssignCareTeamMember ──────────────────────────────────────────────────

    [Fact]
    public async Task AssignCareTeamMember_rejects_student_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantB);
        var educator = Educator.Create(TenantA, "Zeynep", "Ak");
        await ctx.SeedAsync(student, educator);

        await ShouldRejectAsync(() => ctx.Handler<AssignCareTeamMemberCommandHandler>().Handle(
            new AssignCareTeamMemberCommand(student.Id, educator.Id, Guid.CreateVersion7(), true,
                new DateOnly(2026, 11, 1), null, null, null, null, null), default));
        (await ctx.Db.StudentCareAssignments.CountAsync()).Should().Be(0);
    }
}
