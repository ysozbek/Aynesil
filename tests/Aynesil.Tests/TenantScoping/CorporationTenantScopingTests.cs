using System.Reflection;
using Aynesil.Api.Authorization;
using Aynesil.Api.Controllers;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Corporations.Commands;
using Aynesil.Application.Features.Corporations.Queries;
using Aynesil.Application.Features.Roles.Commands;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Shared.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Y1: kurum uçları yalnızca oturumdaki kurumda çalışır (başka kurum → 404); kurum oluşturma ve
/// etkinleştirme/pasifleştirme platform işlemidir ve izinleri kurum rollerine atanamaz.
/// core.corporation RLS'i (V43) entegrasyon testlerinde ayrıca doğrulanır.
/// </summary>
public class CorporationTenantScopingTests
{
    private static async Task<TenantTestContext> TwoCorporationsAsync()
    {
        var ctx = new TenantTestContext();
        await ctx.SeedAsync(NewCorporation(TenantA), NewCorporation(TenantB));
        return ctx;
    }

    [Fact]
    public async Task List_returns_only_the_callers_corporation()
    {
        using var ctx = await TwoCorporationsAsync();

        var result = await ctx.Handler<GetCorporationsQueryHandler>().Handle(new GetCorporationsQuery(), default);

        result.Items.Select(c => c.Id).Should().Equal(TenantA);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Own_corporation_can_be_read_and_updated()
    {
        using var ctx = await TwoCorporationsAsync();

        var dto = await ctx.Handler<GetCorporationQueryHandler>().Handle(new GetCorporationQuery(TenantA), default);
        await ctx.Handler<UpdateCorporationCommandHandler>().Handle(new UpdateCorporationCommand(
            TenantA, "Yeni Ünvan", "Yeni Ad", "tr", "TRY", "Europe/Istanbul", null, null, dto.RowVersion), default);

        (await ctx.Db.Corporations.SingleAsync(c => c.Id == TenantA)).DisplayName.Should().Be("Yeni Ad");
    }

    public static TheoryData<string> OtherCorporationOperations() =>
        ["get", "settings", "update", "update-settings", "activate", "deactivate"];

    [Theory]
    [MemberData(nameof(OtherCorporationOperations))]
    public async Task Another_corporation_is_not_found_and_unchanged(string op)
    {
        using var ctx = await TwoCorporationsAsync();
        var before = await ctx.Db.Corporations.AsNoTracking().SingleAsync(c => c.Id == TenantB);

        Func<Task> act = op switch
        {
            "get" => () => ctx.Handler<GetCorporationQueryHandler>().Handle(new GetCorporationQuery(TenantB), default),
            "settings" => () => ctx.Handler<GetCorporationSettingsQueryHandler>()
                .Handle(new GetCorporationSettingsQuery(TenantB), default),
            "update" => () => ctx.Handler<UpdateCorporationCommandHandler>().Handle(new UpdateCorporationCommand(
                TenantB, "X", "Ele geçirildi", "tr", "TRY", "Europe/Istanbul", null, null, before.RowVersion), default),
            "update-settings" => () => ctx.Handler<UpdateCorporationSettingsCommandHandler>().Handle(
                new UpdateCorporationSettingsCommand(TenantB, "en", "USD", "UTC", null, null, "{}", before.RowVersion), default),
            "activate" => () => ctx.Handler<ActivateCorporationCommandHandler>()
                .Handle(new ActivateCorporationCommand(TenantB), default),
            "deactivate" => () => ctx.Handler<DeactivateCorporationCommandHandler>()
                .Handle(new DeactivateCorporationCommand(TenantB), default),
            _ => throw new InvalidOperationException(op),
        };

        await act.Should().ThrowAsync<NotFoundException>();

        var after = await ctx.Db.Corporations.AsNoTracking().SingleAsync(c => c.Id == TenantB);
        (after.DisplayName, after.Status, after.DefaultLocale, after.RowVersion)
            .Should().Be((before.DisplayName, before.Status, before.DefaultLocale, before.RowVersion));
    }

    [Fact]
    public void Platform_only_permissions_are_corporation_create_and_delete()
    {
        Permissions.PlatformOnly.Should().BeEquivalentTo([Permissions.Corporation.Create, Permissions.Corporation.Delete]);
        Permissions.PlatformOnly.Should().NotContain([Permissions.Corporation.Read, Permissions.Corporation.Update]);
    }

    [Theory]
    [InlineData(nameof(CorporationsController.Create), Permissions.Corporation.Create)]
    [InlineData(nameof(CorporationsController.Activate), Permissions.Corporation.Delete)]
    [InlineData(nameof(CorporationsController.Deactivate), Permissions.Corporation.Delete)]
    [InlineData(nameof(CorporationsController.Update), Permissions.Corporation.Update)]
    [InlineData(nameof(CorporationsController.UpdateSettings), Permissions.Corporation.Update)]
    public void Corporation_endpoints_require_the_expected_permission(string action, string permission)
    {
        var method = typeof(CorporationsController).GetMethod(action)!;
        method.GetCustomAttribute<HasPermissionAttribute>()!.Policy.Should().Be(permission);
    }

    [Theory]
    [InlineData(Permissions.Corporation.Create)]
    [InlineData(Permissions.Corporation.Delete)]
    public async Task Platform_permissions_cannot_be_assigned_to_a_tenant_role(string code)
    {
        using var ctx = new TenantTestContext();
        var role = Role.Create(TenantA, "admin", "Admin");
        var permission = new Permission { Code = code, Resource = "corporation", Action = code.Split(':')[1] };
        await ctx.SeedAsync(role, permission);

        var act = () => ctx.Handler<AssignRolePermissionCommandHandler>()
            .Handle(new AssignRolePermissionCommand(role.Id, permission.Id), default);

        var ex = await act.Should().ThrowAsync<ForbiddenAccessException>();
        ex.Which.Code.Should().Be(MsgKeys.Rule.PlatformPermissionNotAssignable);
        (await ctx.Db.RolePermissions.CountAsync()).Should().Be(0);
    }
}
