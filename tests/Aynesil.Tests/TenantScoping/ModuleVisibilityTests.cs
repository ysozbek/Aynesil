using Aynesil.Api.Authorization;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.TenantModules;
using Aynesil.Application.Features.Corporations.Commands;
using Aynesil.Application.Features.Menus.Queries;
using Aynesil.Domain.Common;
using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Core.Entities;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Shared.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Kurum bazında modül görünürlüğü (V53): settings.modules ayrıştırma, kurum yöneticisinin
/// `modules`'ü değiştirememesi, kapalı modülün uçlarının 403 ve menüsünün gizlenmesi.
/// </summary>
public class ModuleVisibilityTests
{
    /// <summary>Önbelleği atlayıp fabrikayı çalıştıran sahte önbellek.</summary>
    private sealed class PassThroughCache : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default) => factory(cancellationToken);
        public Task InvalidateTenantAsync(Guid corporationId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateMenuTreeAsync(Guid corporationId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    // ── Ayrıştırma ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_defaults_every_module_to_enabled_when_the_key_is_missing_or_settings_are_broken()
    {
        foreach (var json in new[] { "{}", "", "not json", "{\"modules\":5}", "{\"modules\":[]}" })
            CorporationModules.Parse(json).Should().OnlyContain(kv => kv.Value, $"'{json}'");
        CorporationModules.Parse(null).Keys.Should().BeEquivalentTo(Modules.All);
    }

    [Fact]
    public void Parse_reads_explicit_false_and_ignores_unknown_or_non_boolean_values()
    {
        var m = CorporationModules.Parse("{\"modules\":{\"camps\":false,\"kpi\":\"no\",\"surveys\":true,\"bogus\":false}}");
        m[Modules.Camps].Should().BeFalse();
        m[Modules.Kpi].Should().BeTrue("boolean olmayan değer yok sayılır → varsayılan açık");
        m[Modules.Surveys].Should().BeTrue();
        m.Should().NotContainKey("bogus");
    }

    // ── Kurum yöneticisi `modules`'ü değiştiremez ─────────────────────────────

    [Fact]
    public void PreserveModules_drops_the_incoming_modules_and_keeps_the_platform_value()
    {
        var current = "{\"theme\":\"x\",\"modules\":{\"camps\":false,\"kpi\":true}}";
        var incoming = "{\"theme\":\"y\",\"modules\":{\"camps\":true,\"surveys\":true},\"Modules\":{\"x\":1}}";

        var merged = CorporationModules.PreserveModules(incoming, current);

        using var doc = JsonDocument.Parse(merged);
        doc.RootElement.GetProperty("theme").GetString().Should().Be("y", "diğer ayarlar güncellenir");
        var mods = doc.RootElement.GetProperty("modules");
        mods.GetProperty("camps").GetBoolean().Should().BeFalse();
        mods.GetProperty("kpi").GetBoolean().Should().BeTrue();
        mods.TryGetProperty("surveys", out _).Should().BeFalse();
        doc.RootElement.EnumerateObject().Count(p => p.Name.Equals("modules", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
    }

    [Fact]
    public void PreserveModules_without_a_current_value_writes_no_modules_key()
    {
        var merged = CorporationModules.PreserveModules("{\"modules\":{\"camps\":true},\"a\":1}", "{}");
        using var doc = JsonDocument.Parse(merged);
        doc.RootElement.TryGetProperty("modules", out _).Should().BeFalse();
        doc.RootElement.GetProperty("a").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task UpdateCorporationSettings_ignores_a_modules_payload_from_the_tenant_admin()
    {
        using var ctx = new TenantTestContext();
        var corp = NewCorporation(TenantA);
        corp.UpdateSettings("{\"modules\":{\"camps\":false,\"surveys\":false}}");
        await ctx.SeedAsync(corp);

        var dto = await ctx.Handler<UpdateCorporationSettingsCommandHandler>().Handle(
            new UpdateCorporationSettingsCommand(TenantA, corp.DefaultLocale, corp.DefaultCurrency, corp.Timezone, null, null,
                "{\"modules\":{\"camps\":true,\"surveys\":true},\"k\":1}", corp.RowVersion), default);

        dto.Modules[Modules.Camps].Should().BeFalse();
        dto.Modules[Modules.Surveys].Should().BeFalse();
        dto.Settings.Should().Contain("\"k\"");
    }

    // ── Uç filtresi ───────────────────────────────────────────────────────────

    private static ActionExecutingContext FilterContext(IModuleAccess modules)
    {
        var http = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(modules).BuildServiceProvider()
        };
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
    }

    [Fact]
    public async Task RequireModule_rejects_a_disabled_module_with_forbidden_and_does_not_run_the_action()
    {
        var modules = new Mock<IModuleAccess>();
        modules.Setup(m => m.IsEnabledAsync(Modules.Camps, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var called = false;

        var act = () => new RequireModuleAttribute(Modules.Camps).OnActionExecutionAsync(
            FilterContext(modules.Object),
            () => { called = true; return Task.FromResult<ActionExecutedContext>(null!); });

        var ex = await act.Should().ThrowAsync<ForbiddenAccessException>();
        ex.Which.Code.Should().Be(MsgKeys.Error.ModuleDisabled);
        called.Should().BeFalse();
    }

    [Fact]
    public async Task RequireModule_lets_an_enabled_module_through()
    {
        var modules = new Mock<IModuleAccess>();
        modules.Setup(m => m.IsEnabledAsync(Modules.Surveys, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var called = false;

        await new RequireModuleAttribute(Modules.Surveys).OnActionExecutionAsync(
            FilterContext(modules.Object),
            () => { called = true; return Task.FromResult<ActionExecutedContext>(null!); });

        called.Should().BeTrue();
    }

    [Theory]
    [InlineData("CampsController", Modules.Camps)]
    [InlineData("ConsultancyController", Modules.Consultancy)]
    [InlineData("CameraController", Modules.Camera)]
    [InlineData("SurveysController", Modules.Surveys)]
    [InlineData("CareTeamController", Modules.CareTeam)]
    [InlineData("PerformanceKpiController", Modules.Kpi)]
    public void Module_controllers_carry_the_RequireModule_attribute(string controller, string module)
    {
        var type = typeof(RequireModuleAttribute).Assembly.GetTypes().Single(t => t.Name == controller);
        var attr = type.GetCustomAttributes(typeof(RequireModuleAttribute), inherit: false)
            .Cast<RequireModuleAttribute>().Should().ContainSingle().Subject;
        attr.Module.Should().Be(module);
    }

    // ── ModuleAccess (tenant ayarından okur) ──────────────────────────────────

    [Fact]
    public async Task ModuleAccess_reads_the_current_corporations_settings()
    {
        using var ctx = new TenantTestContext();
        var corp = NewCorporation(TenantA);
        corp.UpdateSettings("{\"modules\":{\"kpi\":false}}");
        await ctx.SeedAsync(corp);

        var access = new ModuleAccess(ctx.Db, ctx.Tenant.Object);

        (await access.IsEnabledAsync(Modules.Kpi)).Should().BeFalse();
        (await access.IsEnabledAsync(Modules.Camps)).Should().BeTrue("anahtar yok → açık");
    }

    // ── Menü ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserMenu_hides_items_of_a_disabled_module_including_their_children()
    {
        var corp = TenantA;
        var camps = MenuItem.Create(null, null, "camps", "/camps", "tent", 1, null, Modules.Camps);
        var campList = MenuItem.Create(null, camps.Id, "camps.list", "/camps/list", "list", 1, null, Modules.Camps);
        var students = MenuItem.Create(null, null, "students", "/students", "users", 2);
        var kpi = MenuItem.Create(null, null, "performance", "/kpi", "chart", 3, null, Modules.Kpi);
        foreach (var m in new[] { camps, campList, students, kpi })
            m.Translations = [new MenuItemTranslation { Locale = "tr", Label = m.Code }];

        var repo = new Mock<IMenuRepository>();
        repo.Setup(r => r.GetActiveFlatListAsync(corp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { camps, campList, students, kpi });
        var cache = new PassThroughCache();
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(t => t.CorporationId).Returns(corp);
        tenant.SetupGet(t => t.Locale).Returns("tr");
        var user = new Mock<ICurrentUserService>();
        var modules = new Mock<IModuleAccess>();
        modules.Setup(m => m.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, bool> { [Modules.Camps] = false, [Modules.Kpi] = true });

        var handler = new GetUserMenuQueryHandler(repo.Object, tenant.Object, user.Object, cache, modules.Object);
        var tree = await handler.Handle(new GetUserMenuQuery(), default);

        tree.Select(n => n.Code).Should().BeEquivalentTo(["students", "performance"]);
    }
}
