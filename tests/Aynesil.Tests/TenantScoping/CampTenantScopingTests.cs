using Aynesil.Application.Features.Camps.Commands;
using Aynesil.Domain.Modules.Camps.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

public class CampTenantScopingTests
{
    private static readonly DateOnly Start = new(2026, 7, 1);
    private static readonly DateOnly End = new(2026, 7, 14);

    // ── CreateCampPeriod ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCampPeriod_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var camp = Camp.Create(TenantA, "YAZ", "Yaz Kampı");
        await ctx.SeedAsync(camp);

        var handler = new CreateCampPeriodCommandHandler(ctx.Db, ctx.Tenant.Object);
        var dto = await handler.Handle(
            new CreateCampPeriodCommand(camp.Id, "1. Dönem", Start, End, 10), default);

        dto.CorporationId.Should().Be(TenantA);
        (await ctx.Db.CampPeriods.SingleAsync(p => p.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateCampPeriod_rejects_camp_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignCamp = Camp.Create(TenantB, "YAZ", "Yabancı kamp");
        await ctx.SeedAsync(foreignCamp);

        var handler = new CreateCampPeriodCommandHandler(ctx.Db, ctx.Tenant.Object);
        var act = () => handler.Handle(
            new CreateCampPeriodCommand(foreignCamp.Id, "1. Dönem", Start, End, 10), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Db.CampPeriods.CountAsync()).Should().Be(0);
    }

    // ── EnrollStudent ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EnrollStudent_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var period = CampPeriod.Create(TenantA, Guid.CreateVersion7(), "1. Dönem", Start, End, 10);
        await ctx.SeedAsync(period);

        var handler = new EnrollStudentCommandHandler(ctx.Db, ctx.Tenant.Object);
        var dto = await handler.Handle(
            new EnrollStudentCommand(period.Id, Guid.CreateVersion7()), default);

        dto.CorporationId.Should().Be(TenantA);
        (await ctx.Db.CampEnrollments.SingleAsync(e => e.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task EnrollStudent_rejects_period_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignPeriod = CampPeriod.Create(TenantB, Guid.CreateVersion7(), "1. Dönem", Start, End, 10);
        await ctx.SeedAsync(foreignPeriod);

        var handler = new EnrollStudentCommandHandler(ctx.Db, ctx.Tenant.Object);
        var act = () => handler.Handle(
            new EnrollStudentCommand(foreignPeriod.Id, Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Db.CampEnrollments.CountAsync()).Should().Be(0);
    }
}
