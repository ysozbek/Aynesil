using FluentAssertions;

namespace Aynesil.IntegrationTests;

/// <summary>
/// V65 (docs/raporlar/2026-11-17_dogrulama.md D10), gerçek Flyway sonrası: kullanımdan kalkmış dört izin hiçbir rolde
/// yok, izin satırları duruyor, Finans menüsü (payments) /finance rotasının istediği finance_payment:read'e bağlı ve
/// new_tenant.py ile açılan kurumun yöneticisi bu izinleri almıyor.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class DeprecatedPermissionsMigrationTests : IDisposable
{
    private const string Codes = "('finance:read', 'finance:write', 'session:write', 'refdata:manage')";
    private readonly TenantApiHarness _h;

    public DeprecatedPermissionsMigrationTests(MigratedDatabaseFixture db) => _h = new TenantApiHarness(db);

    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Legacy_permissions_are_kept_but_granted_to_no_role_and_no_menu_needs_them()
    {
        (await _h.ScalarAsync<long>($"select count(*) from iam.permission where code in {Codes}")).Should().Be(4);
        (await _h.ScalarAsync<long>(
            $"select count(*) from iam.role_permission rp join iam.permission p on p.id = rp.permission_id where p.code in {Codes}"))
            .Should().Be(0);
        (await _h.ScalarAsync<string>(
            "select p.code from iam.menu_item m join iam.permission p on p.id = m.required_permission_id where m.code = 'payments' and m.corporation_id is null"))
            .Should().Be("finance_payment:read");
    }

    [Fact]
    public async Task A_new_tenant_admin_does_not_receive_legacy_permissions()
    {
        var code = "dp_" + Guid.NewGuid().ToString("N")[..8];
        var corp = await _h.NewTenantAsync(code, $"admin-{code}@example.test");
        (await _h.ScalarAsync<long>(
            $"select count(*) from iam.role_permission rp join iam.role r on r.id = rp.role_id join iam.permission p on p.id = rp.permission_id where r.corporation_id = @c and p.code in {Codes}",
            ("c", corp))).Should().Be(0);
        (await _h.ScalarAsync<long>(
            "select count(*) from iam.role_permission rp join iam.role r on r.id = rp.role_id join iam.permission p on p.id = rp.permission_id where r.corporation_id = @c and r.code = 'admin' and p.code = 'finance_payment:read'",
            ("c", corp))).Should().Be(1);
    }
}
