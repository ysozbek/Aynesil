using Aynesil.Api.Controllers;
using Aynesil.Application.Common.TenantModules;
using Aynesil.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>GET /api/modules/me: oturumdaki kurumun açık modülleri (yalnızca true olanlar, sıralı).</summary>
public class EnabledModulesEndpointTests
{
    private static async Task<IReadOnlyList<string>> CallAsync(TenantTestContext ctx)
    {
        var result = await new ModulesController(new ModuleAccess(ctx.Db, ctx.Tenant.Object)).GetMine(default);
        var body = (ApiResponse<EnabledModulesDto>)((ObjectResult)result).Value!;
        return body.Data!.EnabledModules;
    }

    [Fact]
    public async Task Lists_only_enabled_modules()
    {
        using var ctx = new TenantTestContext();
        var corp = NewCorporation(TenantA);
        corp.UpdateSettings("{\"modules\":{\"camps\":false,\"care_team\":true,\"kpi\":false,\"surveys\":false}}");
        await ctx.SeedAsync(corp);

        (await CallAsync(ctx)).Should().Equal("camera", "care_team", "consultancy");
    }

    [Fact]
    public async Task Without_a_modules_key_every_module_counts_as_enabled()
    {
        using var ctx = new TenantTestContext();
        await ctx.SeedAsync(NewCorporation(TenantA));

        (await CallAsync(ctx)).Should().HaveCount(6);
    }
}
