using System.Net;
using Aynesil.Api.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aynesil.Tests.Api;

/// <summary>
/// Hız sınırı IP'ye göre çalıştığı için gerçek istemci IP'si yalnızca güvenilen vekilden gelen
/// X-Forwarded-For ile alınır; bilinmeyen kaynaktan gelen başlık yok sayılır.
/// </summary>
public class ForwardedHeadersTests
{
    private static ForwardedHeadersOptions OptionsFrom(Dictionary<string, string?> config)
    {
        var services = new ServiceCollection();
        services.AddAynesilForwardedHeaders(new ConfigurationBuilder().AddInMemoryCollection(config).Build());
        return services.BuildServiceProvider().GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    /// <summary>Runs the real middleware; returns the RemoteIpAddress the next stage (rate limiter) sees.</summary>
    private static async Task<IPAddress?> ClientIpSeenAsync(ForwardedHeadersOptions options, string remote, string? xff)
    {
        IPAddress? seen = null;
        var middleware = new ForwardedHeadersMiddleware(
            ctx => { seen = ctx.Connection.RemoteIpAddress; return Task.CompletedTask; },
            NullLoggerFactory.Instance, Options.Create(options));
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        if (xff is not null) context.Request.Headers["X-Forwarded-For"] = xff;
        await middleware.Invoke(context);
        return seen;
    }

    [Fact]
    public async Task Header_from_a_known_proxy_gives_the_real_client_ip()
    {
        var options = OptionsFrom(new() { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5" });
        (await ClientIpSeenAsync(options, "10.0.0.5", "203.0.113.7")).Should().Be(IPAddress.Parse("203.0.113.7"));
    }

    [Fact]
    public async Task Header_from_a_known_network_gives_the_real_client_ip()
    {
        var options = OptionsFrom(new() { ["ForwardedHeaders:KnownNetworks:0"] = "172.18.0.0/16" });
        (await ClientIpSeenAsync(options, "172.18.4.2", "203.0.113.8")).Should().Be(IPAddress.Parse("203.0.113.8"));
    }

    [Fact]
    public async Task Header_from_an_unknown_source_is_ignored()
    {
        var options = OptionsFrom(new() { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5" });
        (await ClientIpSeenAsync(options, "198.51.100.9", "203.0.113.7")).Should().Be(IPAddress.Parse("198.51.100.9"));
    }

    [Fact]
    public async Task Configured_proxies_replace_the_default_loopback_trust()
    {
        var options = OptionsFrom(new() { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5" });
        (await ClientIpSeenAsync(options, "127.0.0.1", "203.0.113.7")).Should().Be(IPAddress.Loopback);
    }

    [Fact]
    public async Task Empty_configuration_trusts_loopback_only()
    {
        var options = OptionsFrom([]);
        (await ClientIpSeenAsync(options, "127.0.0.1", "203.0.113.7")).Should().Be(IPAddress.Parse("203.0.113.7"));
        (await ClientIpSeenAsync(options, "198.51.100.9", "203.0.113.7")).Should().Be(IPAddress.Parse("198.51.100.9"));
    }

    [Fact]
    public void Malformed_network_fails_fast_at_startup()
    {
        var act = () => OptionsFrom(new() { ["ForwardedHeaders:KnownNetworks:0"] = "172.18.0.0" });
        act.Should().Throw<InvalidOperationException>().WithMessage("*CIDR*");
    }
}
