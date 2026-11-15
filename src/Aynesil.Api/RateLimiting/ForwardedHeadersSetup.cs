using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.HttpOverrides;

namespace Aynesil.Api.RateLimiting;

/// <summary>
/// "ForwardedHeaders" yapılandırması: ters vekil (nginx, yük dengeleyici) arkasında gerçek istemci IP'sini
/// <c>X-Forwarded-For</c>'dan almak için güvenilen vekiller. Hız sınırı (<see cref="AuthRateLimiter"/>),
/// etkinlik günlüğü ve giriş kayıtları <c>Connection.RemoteIpAddress</c>'e bakar; bu yüzden middleware
/// hız sınırından ÖNCE çalışır (Program.cs'te ilk sırada).
///
/// Güvenlik: başlık yalnızca <see cref="KnownProxies"/>/<see cref="KnownNetworks"/>'ten gelen bağlantılarda
/// dikkate alınır; bilinmeyen kaynaktan gelen <c>X-Forwarded-For</c> yok sayılır (istemci kendi IP'sini
/// sahteleyip hız sınırından kaçamaz). Hiçbiri tanımlı değilse (Development) ASP.NET varsayılanı kalır:
/// yalnızca loopback güvenilir. docker-compose.yml compose iç ağını (<c>AYNESIL_NETWORK_SUBNET</c>, vars.
/// 172.30.0.0/24) varsayılan olarak verir: ön yüz nginx'i ve sunucudaki ters vekil (Docker ağ geçidi üzerinden)
/// bu ağdan bağlanır. Başka bir vekil için <c>.env</c>'e ör.
/// <c>AYNESIL_ForwardedHeaders__KnownProxies__0=10.0.0.5</c> yazılır.
/// IPv4 girişleri, Kestrel'in çift yığın soketinde görülen IPv4-eşlemeli IPv6 biçimiyle (::ffff:a.b.c.d) de eklenir.
/// </summary>
public sealed class ForwardedHeadersSettings
{
    public const string Section = "ForwardedHeaders";

    /// <summary>Güvenilen vekil IP adresleri.</summary>
    public string[] KnownProxies { get; set; } = [];

    /// <summary>Güvenilen vekil ağları, CIDR gösterimiyle (ör. 172.18.0.0/16).</summary>
    public string[] KnownNetworks { get; set; } = [];
}

public static class ForwardedHeadersSetup
{
    public static IServiceCollection AddAynesilForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(ForwardedHeadersSettings.Section).Get<ForwardedHeadersSettings>()
                       ?? new ForwardedHeadersSettings();
        services.Configure<ForwardedHeadersOptions>(o => Apply(o, settings));
        return services;
    }

    public static void Apply(ForwardedHeadersOptions options, ForwardedHeadersSettings settings)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        var proxies = settings.KnownProxies.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        var networks = settings.KnownNetworks.Where(n => !string.IsNullOrWhiteSpace(n)).ToArray();
        if (proxies.Length == 0 && networks.Length == 0) return; // varsayılan: yalnızca loopback

        // Yapılandırma verildiyse varsayılan loopback güveni kalkar; yalnızca listelenenler güvenilir.
        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();
        foreach (var proxy in proxies)
        {
            var address = IPAddress.Parse(proxy.Trim());
            options.KnownProxies.Add(address);
            // Kestrel çift yığın (http://+:5000) dinlediğinde IPv4 bağlantılar ::ffff:a.b.c.d olarak görünür.
            if (address.AddressFamily == AddressFamily.InterNetwork)
                options.KnownProxies.Add(address.MapToIPv6());
        }
        foreach (var network in networks)
        {
            var parts = network.Trim().Split('/');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var prefix) || !int.TryParse(parts[1], out var length)
                || length < 0 || length > (prefix.AddressFamily == AddressFamily.InterNetwork ? 32 : 128))
                throw new InvalidOperationException(
                    $"ForwardedHeaders:KnownNetworks entry '{network}' must be in CIDR form (e.g. 172.18.0.0/16).");
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, length));
            if (prefix.AddressFamily == AddressFamily.InterNetwork)
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix.MapToIPv6(), 96 + length));
        }
    }
}
