using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// docs/raporlar/2026-11-17_dogrulama.md D9: devops/set_modules.py, kurumda öğrenci bazlı erişim kısıtı
/// (core.setting_value care_team_abac_enabled) açıkken care_team modülünü --force olmadan kapatmaz; --force ile kapatır;
/// --list kısıtın durumunu ve "kısıt açık, modül kapalı" uyarısını gösterir. Betiğin --dry-run SQL'i gerçek şemada
/// (sahip rolüyle, betiğin kendisi gibi) çalıştırılır.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class SetModulesCareTeamGuardTests : IDisposable
{
    private readonly TenantApiHarness _h;
    private readonly MigratedDatabaseFixture _db;
    private readonly string _code = "sm_" + Guid.NewGuid().ToString("N")[..8];

    public SetModulesCareTeamGuardTests(MigratedDatabaseFixture db)
    {
        _db = db;
        _h = new TenantApiHarness(db);
    }

    public void Dispose() => _h.Dispose();

    private async Task<Guid> TenantAsync(bool abac, bool careTeamOn = true)
    {
        var corp = await _h.NewTenantAsync(_code, $"admin-{_code}@example.test");
        await _h.ExecAsync(
            "update core.corporation set settings = jsonb_set(coalesce(settings, '{}'::jsonb), '{modules}', @m::jsonb, true) where id = @c",
            ("m", careTeamOn ? "{\"care_team\": true}" : "{\"care_team\": false}"), ("c", corp));
        if (abac)
            await _h.ExecAsync(
                "insert into core.setting_value (setting_key, scope_level, corporation_id, value) values ('care_team_abac_enabled', 'corporation', @c, 'true'::jsonb)",
                ("c", corp));
        return corp;
    }

    private static async Task<string> DryRunSqlAsync(params string[] args)
    {
        var psi = new ProcessStartInfo("python3")
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add(Path.Combine(RepoRoot(), "devops", "set_modules.py"));
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.ArgumentList.Add("--dry-run");
        using var p = Process.Start(psi)!;
        var sql = await p.StandardOutput.ReadToEndAsync();
        await p.WaitForExitAsync();
        p.ExitCode.Should().Be(0, await p.StandardError.ReadToEndAsync());
        return sql;
    }

    /// <summary>SQL'i sahip bağlantısında çalıştırır; NOTICE satırlarını döndürür.</summary>
    private async Task<List<string>> RunAsync(string sql)
    {
        var notices = new List<string>();
        await using var conn = await _db.OpenAsync();
        conn.Notice += (_, e) => notices.Add(e.Notice.MessageText);
        await using var cmd = new NpgsqlCommand(sql, conn);
        try { await cmd.ExecuteNonQueryAsync(); }
        catch { await using var rb = new NpgsqlCommand("rollback", conn); await rb.ExecuteNonQueryAsync(); throw; }
        return notices;
    }

    private Task<bool> CareTeamEnabledAsync(Guid corp) => _h.ScalarAsync<bool>(
        "select coalesce((settings -> 'modules' ->> 'care_team')::boolean, true) from core.corporation where id = @c", ("c", corp));

    [Fact]
    public async Task Disabling_care_team_while_the_access_restriction_is_on_stops_without_force()
    {
        var corp = await TenantAsync(abac: true);

        var act = async () => await RunAsync(await DryRunSqlAsync("--code", _code, "--disable", "care_team"));
        (await act.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().StartWith("CARE_TEAM_ABAC:");
        (await CareTeamEnabledAsync(corp)).Should().BeTrue();

        // Başka modüller kısıttan etkilenmez.
        await RunAsync(await DryRunSqlAsync("--code", _code, "--disable", "kpi"));
    }

    [Fact]
    public async Task Force_disables_care_team_and_list_warns_about_the_combination()
    {
        var corp = await TenantAsync(abac: true);

        await RunAsync(await DryRunSqlAsync("--code", _code, "--disable", "care_team", "--force"));
        (await CareTeamEnabledAsync(corp)).Should().BeFalse();

        var notices = await RunAsync(await DryRunSqlAsync("--code", _code, "--list"));
        notices.Should().Contain(n => n.StartsWith("MODULE erişim kısıtı (bakım ekibi) = AÇIK"));
        notices.Should().Contain(n => n.StartsWith("MODULE UYARI:"));
    }

    [Fact]
    public async Task Without_the_restriction_care_team_is_disabled_normally_and_list_says_off()
    {
        var corp = await TenantAsync(abac: false);

        var notices = await RunAsync(await DryRunSqlAsync("--code", _code, "--disable", "care_team"));
        (await CareTeamEnabledAsync(corp)).Should().BeFalse();
        notices.Should().Contain(n => n.StartsWith("MODULE erişim kısıtı (bakım ekibi) = kapalı"));
        notices.Should().NotContain(n => n.StartsWith("MODULE UYARI:"));
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Aynesil.sln"))) return dir.FullName;
        throw new InvalidOperationException("Repository root (Aynesil.sln) not found.");
    }
}
