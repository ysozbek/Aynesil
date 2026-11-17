using Aynesil.Application.Features.Permissions.Queries;
using Aynesil.Application.Features.Roles.Commands;
using Aynesil.Domain.Modules.Iam.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// docs/raporlar/2026-11-17_dogrulama.md D10: kullanımdan kalkmış izinler (Permissions.Deprecated) izin listesinde
/// görünmez ve rollere atanamaz (422 Rule.PermissionDeprecated). Güncel izinler etkilenmez.
/// </summary>
public class DeprecatedPermissionTests
{
    private static Permission Perm(string code)
        => new() { Code = code, Resource = code.Split(':')[0], Action = code.Split(':')[1] };

    [Fact]
    public void The_four_legacy_permissions_are_deprecated_and_none_is_an_endpoint_constant()
    {
        Aynesil.Shared.Constants.Permissions.Deprecated.Should()
            .BeEquivalentTo("finance:read", "finance:write", "session:write", "refdata:manage");

        var constants = typeof(Aynesil.Shared.Constants.Permissions).GetNestedTypes()
            .SelectMany(t => t.GetFields().Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()!));
        constants.Should().NotIntersectWith(Aynesil.Shared.Constants.Permissions.Deprecated);
    }

    [Fact]
    public async Task Permission_list_hides_deprecated_codes()
    {
        using var ctx = new TenantTestContext();
        await ctx.SeedAsync(Perm("finance:read"), Perm("session:write"), Perm("invoice:read"), Perm("session:read"));

        var page = await ctx.Handler<GetPermissionsQueryHandler>()
            .Handle(new GetPermissionsQuery { Page = 1, PageSize = 50 }, default);

        page.Items.Select(p => p.Code).Should().BeEquivalentTo("invoice:read", "session:read");
    }

    [Fact]
    public async Task Deprecated_permission_cannot_be_assigned_to_a_role()
    {
        using var ctx = new TenantTestContext();
        var role = Role.Create(TenantA, "ozel", "Özel");
        var legacy = Perm("finance:write");
        await ctx.SeedAsync(role, legacy);

        var act = () => ctx.Handler<AssignRolePermissionCommandHandler>()
            .Handle(new AssignRolePermissionCommand(role.Id, legacy.Id), default);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("permissionId");
        (await ctx.Db.RolePermissions.CountAsync()).Should().Be(0);
    }
}
