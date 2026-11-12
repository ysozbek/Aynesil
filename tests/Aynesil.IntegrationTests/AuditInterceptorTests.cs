using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Finance.Entities;
using Aynesil.Infrastructure.Persistence;
using Aynesil.Infrastructure.Persistence.Interceptors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// AuditSaveChangesInterceptor against the real model and a real PostgreSQL 17.
/// Regression: the interceptor called entry.Property("CreatedBy") for every modified entity, which
/// throws InvalidOperationException for tables without created_by (invoice, payment,
/// assessment_report, consultancy_plan, ...), whose configurations Ignore() the property.
/// The in-memory provider tests never exercised this path against a model with ignored audit columns.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuditInterceptorTests(MigratedDatabaseFixture db)
{
    private static readonly Guid Actor = Guid.CreateVersion7();

    static AuditInterceptorTests() =>
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

    private AynesilDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AynesilDbContext>()
            .UseNpgsql(db.ConnectionString)
            .AddInterceptors(new AuditSaveChangesInterceptor(new FakeUser(), new FakeClock()))
            .Options;
        return new AynesilDbContext(options);
    }

    [Fact]
    public async Task Updating_an_invoice_succeeds_although_the_table_has_no_created_by()
    {
        Guid corporationId;
        await using (var conn = await db.OpenAsync())
        {
            await using var cmd = new NpgsqlCommand(
                "insert into core.corporation (code, legal_name, display_name) values (@c, 'Audit Test', 'Audit') returning id", conn);
            cmd.Parameters.AddWithValue("c", "audit-" + Guid.NewGuid().ToString("N")[..12]);
            corporationId = (Guid)(await cmd.ExecuteScalarAsync())!;
        }

        var invoice = Invoice.Create(corporationId, DateOnly.FromDateTime(DateTime.UtcNow));
        await using (var ctx = NewContext())
        {
            ctx.Invoices.Add(invoice);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var loaded = await ctx.Invoices.SingleAsync(i => i.Id == invoice.Id);
            loaded.Issue();

            var act = () => ctx.SaveChangesAsync();
            await act.Should().NotThrowAsync();
        }

        await using (var ctx = NewContext())
        {
            var reloaded = await ctx.Invoices.AsNoTracking().SingleAsync(i => i.Id == invoice.Id);
            reloaded.Status.Should().Be("issued");
        }
    }

    /// <summary>
    /// Model-wide scan: every auditable entity type, in the Modified state, must get past the
    /// interceptor. Each row is a phantom (random id), so the UPDATE matches no row and EF raises
    /// DbUpdateConcurrencyException — that is the "reached the database" outcome. Anything else
    /// (notably InvalidOperationException from the interceptor) is a bug.
    /// </summary>
    [Fact]
    public async Task Every_auditable_entity_type_survives_the_modified_path()
    {
        await using var probe = NewContext();
        var types = probe.Model.GetEntityTypes()
            .Where(t => !t.IsOwned() && !t.ClrType.IsAbstract
                        && typeof(AuditableEntity).IsAssignableFrom(t.ClrType)
                        && t.FindPrimaryKey()?.Properties.Count == 1
                        && t.FindPrimaryKey()!.Properties[0].Name == nameof(BaseEntity.Id))
            .Select(t => t.ClrType)
            .ToList();

        types.Should().NotBeEmpty();
        types.Should().Contain(typeof(Invoice));

        var failures = new List<string>();
        foreach (var type in types)
        {
            await using var ctx = NewContext();
            var entity = (AuditableEntity)Activator.CreateInstance(type, nonPublic: true)!;
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(entity, Guid.CreateVersion7());

            ctx.Attach(entity);
            ctx.Entry(entity).State = EntityState.Modified;

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // expected: the phantom row does not exist
            }
            catch (Exception ex)
            {
                failures.Add($"{type.Name}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        failures.Should().BeEmpty("no entity type may break the audit interceptor on update");
    }

    private sealed class FakeUser : ICurrentUserService
    {
        public Guid? UserId => Actor;
        public string? UserName => "audit-test";
        public bool IsAuthenticated => true;
        public IReadOnlyDictionary<string, string> Claims { get; } = new Dictionary<string, string>();
        public bool HasPermission(string permissionCode) => true;
    }

    private sealed class FakeClock : IDateTimeService
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public DateTimeOffset ConvertToTenantTime(DateTimeOffset utcTime, string timezone) => utcTime;
        public DateOnly TodayInTimezone(string timezone) => DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
