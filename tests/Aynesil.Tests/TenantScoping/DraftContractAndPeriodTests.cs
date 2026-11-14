using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Features.Legal.Commands;
using Aynesil.Application.Features.Plans.Commands;
using Aynesil.Domain.Modules.Education.Entities;
using Aynesil.Domain.Modules.Legal.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Taslak sözleşme düzenleme/silme (yalnızca draft) ve akademik dönem yönetimi kuralları.</summary>
public class DraftContractAndPeriodTests
{
    private static Student NewStudent() => Student.Create(TenantA, "Ali", "Kaya");

    private static StudentContract Draft(Guid studentId, Guid? guardianId = null) =>
        StudentContract.Generate(TenantA, studentId, null, null, guardianId, new(2026, 1, 1), new(2026, 12, 31));

    private static UpdateStudentContractCommand Edit(StudentContract c, Guid? guardianId = null) =>
        new(c.Id, guardianId, new(2026, 2, 1), new(2026, 11, 30), c.RowVersion);

    // ── Taslak sözleşme ───────────────────────────────────────────────────────

    [Fact]
    public async Task Update_changes_dates_of_a_draft_contract()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent();
        var contract = Draft(student.Id);
        await ctx.SeedAsync(student, contract);

        await ctx.Handler<UpdateStudentContractCommandHandler>().Handle(Edit(contract), default);

        var saved = await ctx.Db.StudentContracts.SingleAsync();
        saved.StartsOn.Should().Be(new DateOnly(2026, 2, 1));
        saved.EndsOn.Should().Be(new DateOnly(2026, 11, 30));
    }

    [Theory]
    [InlineData("sent")]
    [InlineData("signed")]
    [InlineData("active")]
    public async Task Update_is_rejected_unless_the_contract_is_a_draft(string status)
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent();
        var contract = Draft(student.Id);
        Advance(contract, status);
        await ctx.SeedAsync(student, contract);

        var act = () => ctx.Handler<UpdateStudentContractCommandHandler>().Handle(Edit(contract), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.StudentContracts.SingleAsync()).StartsOn.Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public async Task Update_rejects_a_stale_row_version()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent();
        var contract = Draft(student.Id);
        await ctx.SeedAsync(student, contract);

        var act = () => ctx.Handler<UpdateStudentContractCommandHandler>().Handle(
            Edit(contract) with { RowVersion = contract.RowVersion + 3 }, default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Update_rejects_a_guardian_who_is_not_linked_to_the_contract_student()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent();
        var contract = Draft(student.Id);
        var stranger = Guid.CreateVersion7();
        await ctx.SeedAsync(student, contract);

        var act = () => ctx.Handler<UpdateStudentContractCommandHandler>().Handle(Edit(contract, stranger), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_accepts_a_guardian_linked_to_the_student()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent();
        var guardianId = Guid.CreateVersion7();
        var link = new StudentGuardian { CorporationId = TenantA, StudentId = student.Id, GuardianId = guardianId };
        var contract = Draft(student.Id);
        await ctx.SeedAsync(student, link, contract);

        await ctx.Handler<UpdateStudentContractCommandHandler>().Handle(Edit(contract, guardianId), default);

        (await ctx.Db.StudentContracts.SingleAsync()).GuardianId.Should().Be(guardianId);
    }

    [Fact]
    public async Task Delete_soft_deletes_a_draft_contract()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent();
        var contract = Draft(student.Id);
        await ctx.SeedAsync(student, contract);

        await ctx.Handler<DeleteStudentContractCommandHandler>().Handle(new DeleteStudentContractCommand(contract.Id), default);

        (await ctx.Db.StudentContracts.IgnoreQueryFilters().SingleAsync()).DeletedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("sent")]
    [InlineData("signed")]
    public async Task Delete_is_rejected_unless_the_contract_is_a_draft(string status)
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent();
        var contract = Draft(student.Id);
        Advance(contract, status);
        await ctx.SeedAsync(student, contract);

        var act = () => ctx.Handler<DeleteStudentContractCommandHandler>().Handle(new DeleteStudentContractCommand(contract.Id), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.StudentContracts.IgnoreQueryFilters().SingleAsync()).DeletedAt.Should().BeNull();
    }

    private static void Advance(StudentContract c, string status)
    {
        c.Send();
        if (status == "sent") return;
        c.Sign("Veli Adı", "wet");
        if (status == "signed") return;
        c.Activate();
    }

    // ── Akademik dönem ────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_rejects_the_current_academic_period()
    {
        using var ctx = new TenantTestContext();
        var period = AcademicPeriod.Create(TenantA, "2026-2027", new(2026, 9, 1), new(2027, 6, 30), null, isCurrent: true);
        await ctx.SeedAsync(period);

        var act = () => ctx.Handler<DeleteAcademicPeriodCommandHandler>().Handle(new DeleteAcademicPeriodCommand(period.Id), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.AcademicPeriods.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Delete_removes_an_unused_non_current_period()
    {
        using var ctx = new TenantTestContext();
        var period = AcademicPeriod.Create(TenantA, "2025-2026", new(2025, 9, 1), new(2026, 6, 30));
        await ctx.SeedAsync(period);

        await ctx.Handler<DeleteAcademicPeriodCommandHandler>().Handle(new DeleteAcademicPeriodCommand(period.Id), default);

        (await ctx.Db.AcademicPeriods.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SetCurrent_moves_the_current_flag_to_the_chosen_period()
    {
        using var ctx = new TenantTestContext();
        var old = AcademicPeriod.Create(TenantA, "Eski", new(2025, 9, 1), new(2026, 6, 30), null, isCurrent: true);
        var next = AcademicPeriod.Create(TenantA, "Yeni", new(2026, 9, 1), new(2027, 6, 30));
        await ctx.SeedAsync(old, next);

        await ctx.Handler<SetCurrentAcademicPeriodCommandHandler>().Handle(new SetCurrentAcademicPeriodCommand(next.Id), default);

        var rows = await ctx.Db.AcademicPeriods.ToDictionaryAsync(p => p.Name, p => p.IsCurrent);
        rows["Yeni"].Should().BeTrue();
        rows["Eski"].Should().BeFalse();
    }

    [Fact]
    public async Task Update_changes_name_and_dates_and_rejects_a_stale_row_version()
    {
        using var ctx = new TenantTestContext();
        var period = AcademicPeriod.Create(TenantA, "Dönem", new(2026, 9, 1), new(2027, 6, 30));
        await ctx.SeedAsync(period);

        await ctx.Handler<UpdateAcademicPeriodCommandHandler>().Handle(
            new UpdateAcademicPeriodCommand(period.Id, "Dönem (güncel)", new(2026, 9, 7), new(2027, 6, 20), null, period.RowVersion), default);
        (await ctx.Db.AcademicPeriods.SingleAsync()).Name.Should().Be("Dönem (güncel)");

        var stale = () => ctx.Handler<UpdateAcademicPeriodCommandHandler>().Handle(
            new UpdateAcademicPeriodCommand(period.Id, "x", new(2026, 9, 7), new(2027, 6, 20), null, period.RowVersion + 5), default);
        await stale.Should().ThrowAsync<BusinessRuleViolationException>();
    }
}
