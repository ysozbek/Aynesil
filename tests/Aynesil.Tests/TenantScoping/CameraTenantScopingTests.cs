using Aynesil.Application.Features.Cameras.Commands;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Legal.Entities;
using Aynesil.Domain.Modules.Media.Entities;
using Aynesil.Domain.Modules.Scheduling.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

public class CameraTenantScopingTests
{
    // ── AssignCameraToRoom ────────────────────────────────────────────────────

    [Fact]
    public async Task AssignCameraToRoom_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var camera = Camera.Register(TenantA, "CAM-1", "Kamera 1");
        var room = Room.Create(TenantA, "R-1", "Derslik 1", 4, false);
        await ctx.SeedAsync(camera, room);

        var handler = new AssignCameraToRoomCommandHandler(ctx.Db, ctx.Tenant.Object);
        var id = await handler.Handle(new AssignCameraToRoomCommand(camera.Id, room.Id), default);

        var saved = await ctx.Db.RoomCameras.SingleAsync(rc => rc.Id == id);
        saved.CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task AssignCameraToRoom_rejects_camera_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignCamera = Camera.Register(TenantB, "CAM-B", "Yabancı kamera");
        var room = Room.Create(TenantA, "R-1", "Derslik 1", 4, false);
        await ctx.SeedAsync(foreignCamera, room);

        var handler = new AssignCameraToRoomCommandHandler(ctx.Db, ctx.Tenant.Object);
        var act = () => handler.Handle(new AssignCameraToRoomCommand(foreignCamera.Id, room.Id), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Db.RoomCameras.CountAsync()).Should().Be(0);
    }

    // ── AssignCameraToSession ─────────────────────────────────────────────────

    [Fact]
    public async Task AssignCameraToSession_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var camera = Camera.Register(TenantA, "CAM-1", "Kamera 1");
        var session = NewSession(TenantA);
        await ctx.SeedAsync(camera, session);

        var handler = new AssignCameraToSessionCommandHandler(ctx.Db, ctx.Tenant.Object);
        var id = await handler.Handle(new AssignCameraToSessionCommand(camera.Id, session.Id), default);

        var saved = await ctx.Db.SessionCameras.SingleAsync(sc => sc.Id == id);
        saved.CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task AssignCameraToSession_rejects_session_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var camera = Camera.Register(TenantA, "CAM-1", "Kamera 1");
        var foreignSession = NewSession(TenantB);
        await ctx.SeedAsync(camera, foreignSession);

        var handler = new AssignCameraToSessionCommandHandler(ctx.Db, ctx.Tenant.Object);
        var act = () => handler.Handle(
            new AssignCameraToSessionCommand(camera.Id, foreignSession.Id), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Db.SessionCameras.CountAsync()).Should().Be(0);
    }

    // ── GrantViewingAuthorization ─────────────────────────────────────────────

    [Fact]
    public async Task GrantViewingAuthorization_uses_tenant_and_acting_user_from_context()
    {
        using var ctx = new TenantTestContext();
        var (guardianId, studentId) = (Guid.CreateVersion7(), Guid.CreateVersion7());
        await ctx.SeedAsync(
            PortalAccess(TenantA, guardianId, studentId),
            StudentConsent.Grant(TenantA, studentId, guardianId));

        var handler = new GrantViewingAuthorizationCommandHandler(
            ctx.Db, ctx.Tenant.Object, ctx.CurrentUser.Object,
            NullLogger<GrantViewingAuthorizationCommandHandler>.Instance);
        var id = await handler.Handle(GrantCommand(guardianId, studentId), default);

        var saved = await ctx.Db.ViewingAuthorizations.SingleAsync(a => a.Id == id);
        saved.CorporationId.Should().Be(TenantA);
        saved.GrantedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task GrantViewingAuthorization_rejects_portal_access_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var (guardianId, studentId) = (Guid.CreateVersion7(), Guid.CreateVersion7());
        await ctx.SeedAsync(
            PortalAccess(TenantB, guardianId, studentId),
            StudentConsent.Grant(TenantB, studentId, guardianId));

        var handler = new GrantViewingAuthorizationCommandHandler(
            ctx.Db, ctx.Tenant.Object, ctx.CurrentUser.Object,
            NullLogger<GrantViewingAuthorizationCommandHandler>.Instance);
        var act = () => handler.Handle(GrantCommand(guardianId, studentId), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.ViewingAuthorizations.CountAsync()).Should().Be(0);
    }

    // ── StartViewingSession ───────────────────────────────────────────────────

    [Fact]
    public async Task StartViewingSession_uses_tenant_and_acting_user_from_context()
    {
        using var ctx = new TenantTestContext();
        var auth = ValidAuthorization(TenantA, out var studentId);
        await ctx.SeedAsync(auth, StudentConsent.Grant(TenantA, studentId, auth.GuardianId));

        var handler = new StartViewingSessionCommandHandler(
            ctx.Db, ctx.Tenant.Object, ctx.CurrentUser.Object,
            NullLogger<StartViewingSessionCommandHandler>.Instance);
        var result = await handler.Handle(
            new StartViewingSessionCommand(auth.Id, null, null), default);

        var log = await ctx.Db.ViewingLogs.SingleAsync(l => l.Id == result.LogId);
        log.CorporationId.Should().Be(TenantA);
        log.UserId.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task StartViewingSession_rejects_authorization_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignAuth = ValidAuthorization(TenantB, out var studentId);
        await ctx.SeedAsync(foreignAuth, StudentConsent.Grant(TenantB, studentId, foreignAuth.GuardianId));

        var handler = new StartViewingSessionCommandHandler(
            ctx.Db, ctx.Tenant.Object, ctx.CurrentUser.Object,
            NullLogger<StartViewingSessionCommandHandler>.Instance);
        var act = () => handler.Handle(
            new StartViewingSessionCommand(foreignAuth.Id, null, null), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Db.ViewingLogs.CountAsync()).Should().Be(0);
    }

    // ── EndViewingSession ─────────────────────────────────────────────────────

    [Fact]
    public async Task EndViewingSession_rejects_log_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignLog = ViewingLog.Start(TenantB, null, null, null, null, null);
        await ctx.SeedAsync(foreignLog);

        var handler = new EndViewingSessionCommandHandler(
            ctx.Db, ctx.Tenant.Object, ctx.CurrentUser.Object,
            NullLogger<EndViewingSessionCommandHandler>.Instance);
        var act = () => handler.Handle(
            new EndViewingSessionCommand(foreignLog.Id, foreignLog.StartedAt), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Session NewSession(Guid corporationId)
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);
        return Session.Schedule(corporationId, Guid.CreateVersion7(), start, start.AddHours(1));
    }

    private static GuardianPortalAccess PortalAccess(Guid corporationId, Guid guardianId, Guid studentId) =>
        new()
        {
            CorporationId = corporationId,
            GuardianId = guardianId,
            StudentId = studentId,
            CanViewCamera = true,
        };

    private static GrantViewingAuthorizationCommand GrantCommand(Guid guardianId, Guid studentId)
    {
        var from = DateTimeOffset.UtcNow;
        return new GrantViewingAuthorizationCommand(guardianId, studentId, from, from.AddDays(7));
    }

    private static ViewingAuthorization ValidAuthorization(Guid corporationId, out Guid studentId)
    {
        studentId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        return ViewingAuthorization.Grant(
            corporationId, Guid.CreateVersion7(), studentId, now.AddHours(-1), now.AddDays(1));
    }
}
