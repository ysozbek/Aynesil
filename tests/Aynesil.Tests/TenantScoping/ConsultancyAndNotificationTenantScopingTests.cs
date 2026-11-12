using Aynesil.Application.Features.Consultancy.Commands;
using Aynesil.Application.Features.Notifications.Commands;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

public class ConsultancyAndNotificationTenantScopingTests
{
    // ── CreateObservation ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateObservation_uses_tenant_and_acting_user_from_context()
    {
        using var ctx = new TenantTestContext();
        var visit = SchoolVisit.Schedule(TenantA, Guid.CreateVersion7(), new DateOnly(2026, 11, 20));
        await ctx.SeedAsync(visit);

        var handler = new CreateObservationCommandHandler(
            ctx.Db, ctx.Tenant.Object, ctx.CurrentUser.Object);
        var dto = await handler.Handle(
            new CreateObservationCommand(visit.Id, "Gözlem", null, null, null), default);

        dto.CorporationId.Should().Be(TenantA);
        dto.CreatedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task CreateObservation_rejects_visit_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignVisit = SchoolVisit.Schedule(TenantB, Guid.CreateVersion7(), new DateOnly(2026, 11, 20));
        await ctx.SeedAsync(foreignVisit);

        var handler = new CreateObservationCommandHandler(
            ctx.Db, ctx.Tenant.Object, ctx.CurrentUser.Object);
        var act = () => handler.Handle(
            new CreateObservationCommand(foreignVisit.Id, "Gözlem", null, null, null), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Db.ObservationRecords.CountAsync()).Should().Be(0);
    }

    // ── UpdateNotificationPreferences (no parent record) ──────────────────────

    [Fact]
    public async Task UpdateNotificationPreferences_uses_tenant_and_user_from_context()
    {
        using var ctx = new TenantTestContext();

        var handler = new UpdateNotificationPreferencesCommandHandler(
            ctx.Db, ctx.Tenant.Object, ctx.CurrentUser.Object);
        await handler.Handle(new UpdateNotificationPreferencesCommand(
            [new PreferenceInput(Guid.CreateVersion7(), Guid.CreateVersion7(), false)]), default);

        var saved = await ctx.Db.NotificationPreferences.SingleAsync();
        saved.CorporationId.Should().Be(TenantA);
        saved.UserId.Should().Be(ctx.UserId);
    }
}
