using Aynesil.Api.RateLimiting;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Aynesil.Tests.Api;

/// <summary>Y7: IP başına ve hedef kimlik başına sabit pencereli sınır.</summary>
public class AuthRateLimiterTests
{
    private static AuthRateLimiter Limiter(int login = 10, int resetRequest = 3) => new(Options.Create(new AuthRateLimitOptions
    {
        Login = new() { PermitLimit = login, Window = TimeSpan.FromMinutes(1) },
        PasswordResetRequest = new() { PermitLimit = resetRequest, Window = TimeSpan.FromHours(1) },
    }));

    [Fact]
    public void Defaults_match_the_agreed_limits()
    {
        var o = new AuthRateLimitOptions();
        (o.Login.PermitLimit, o.Login.Window).Should().Be((10, TimeSpan.FromMinutes(1)));
        (o.PasswordResetRequest.PermitLimit, o.PasswordResetRequest.Window).Should().Be((3, TimeSpan.FromHours(1)));
        (o.PasswordResetConfirm.PermitLimit, o.PasswordResetConfirm.Window).Should().Be((10, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Same_identity_from_one_ip_is_limited_after_the_permit_count()
    {
        using var limiter = Limiter();
        for (var i = 0; i < 10; i++)
            limiter.TryAcquire(AuthRateLimitPolicy.Login, "10.0.0.1", "ayse", out _).Should().BeTrue();

        limiter.TryAcquire(AuthRateLimitPolicy.Login, "10.0.0.1", "ayse", out var retryAfter).Should().BeFalse();
        retryAfter.Should().NotBeNull().And.BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void One_ip_trying_many_accounts_is_limited_password_spraying()
    {
        using var limiter = Limiter();
        for (var i = 0; i < 10; i++)
            limiter.TryAcquire(AuthRateLimitPolicy.Login, "10.0.0.1", $"user{i}", out _).Should().BeTrue();

        limiter.TryAcquire(AuthRateLimitPolicy.Login, "10.0.0.1", "user-new", out _).Should().BeFalse();
        limiter.TryAcquire(AuthRateLimitPolicy.Login, "10.0.0.2", "user-new", out _).Should().BeTrue("başka IP'nin sınırı ayrı");
    }

    [Fact]
    public void One_account_tried_from_many_ips_is_limited()
    {
        using var limiter = Limiter();
        for (var i = 0; i < 10; i++)
            limiter.TryAcquire(AuthRateLimitPolicy.Login, $"10.0.0.{i}", "ayse", out _).Should().BeTrue();

        limiter.TryAcquire(AuthRateLimitPolicy.Login, "10.0.1.1", "ayse", out _).Should().BeFalse();
    }

    [Fact]
    public void Identity_is_case_and_whitespace_insensitive()
    {
        using var limiter = Limiter(login: 2);
        limiter.TryAcquire(AuthRateLimitPolicy.Login, "10.0.0.1", "Ayse@Example.com", out _).Should().BeTrue();
        limiter.TryAcquire(AuthRateLimitPolicy.Login, "10.0.0.2", " ayse@example.com ", out _).Should().BeTrue();
        limiter.TryAcquire(AuthRateLimitPolicy.Login, "10.0.0.3", "AYSE@EXAMPLE.COM", out _).Should().BeFalse();
    }

    [Fact]
    public void Policies_are_counted_independently()
    {
        using var limiter = Limiter();
        for (var i = 0; i < 3; i++)
            limiter.TryAcquire(AuthRateLimitPolicy.PasswordResetRequest, "10.0.0.1", "a@b.c", out _).Should().BeTrue();

        limiter.TryAcquire(AuthRateLimitPolicy.PasswordResetRequest, "10.0.0.1", "a@b.c", out _).Should().BeFalse();
        limiter.TryAcquire(AuthRateLimitPolicy.Login, "10.0.0.1", "a@b.c", out _).Should().BeTrue();
        limiter.TryAcquire(AuthRateLimitPolicy.PasswordResetConfirm, "10.0.0.1", "token", out _).Should().BeTrue();
    }

    [Fact]
    public void Missing_ip_and_identity_still_share_a_limited_partition()
    {
        using var limiter = Limiter(login: 1);
        limiter.TryAcquire(AuthRateLimitPolicy.Login, null, null, out _).Should().BeTrue();
        limiter.TryAcquire(AuthRateLimitPolicy.Login, null, null, out _).Should().BeFalse();
    }
}
