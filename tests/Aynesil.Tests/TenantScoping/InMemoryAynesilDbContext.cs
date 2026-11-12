using System.Net;
using System.Text.Json;
using Aynesil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// AynesilDbContext variant for the EF Core InMemory provider. The production model uses a few
/// Npgsql-native CLR types (JsonDocument, IPAddress) that InMemory cannot map; they are converted
/// to strings here. The rest of the model (configurations, query filters) is unchanged.
/// </summary>
public sealed class InMemoryAynesilDbContext : AynesilDbContext
{
    public InMemoryAynesilDbContext(DbContextOptions<AynesilDbContext> options) : base(options) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<JsonDocument>().HaveConversion<JsonDocumentToStringConverter>();
        configurationBuilder.Properties<IPAddress>().HaveConversion<IpAddressToStringConverter>();
    }

    private sealed class JsonDocumentToStringConverter() : ValueConverter<JsonDocument, string>(
        v => v.RootElement.GetRawText(),
        v => JsonDocument.Parse(v, default));

    private sealed class IpAddressToStringConverter() : ValueConverter<IPAddress, string>(
        v => v.ToString(),
        v => IPAddress.Parse(v));
}
