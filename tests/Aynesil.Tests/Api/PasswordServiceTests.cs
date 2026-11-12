using Aynesil.Infrastructure.Services.Auth;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Aynesil.Tests.Api;

public class PasswordServiceTests
{
    private static PasswordService Create() =>
        new(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

    [Fact]
    public void VerifyOrDummy_with_a_real_hash_behaves_like_Verify()
    {
        var svc = Create();
        var hash = svc.Hash("Correct#Pass1");

        svc.VerifyOrDummy("Correct#Pass1", hash).Should().BeTrue();
        svc.VerifyOrDummy("wrong", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void VerifyOrDummy_without_a_hash_is_false_but_still_spends_a_real_bcrypt_verification(string? hash)
    {
        var svc = Create();
        PasswordService.EnsureDummyHash();
        svc.VerifyOrDummy("warmup", null);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = svc.VerifyOrDummy("anything", hash);
        sw.Stop();

        result.Should().BeFalse();
        sw.ElapsedMilliseconds.Should().BeGreaterThan(30, "maliyet faktörü 12'de BCrypt doğrulaması hızlı dönemez");
    }
}
