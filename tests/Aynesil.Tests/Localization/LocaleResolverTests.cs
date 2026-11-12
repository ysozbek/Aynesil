using Aynesil.Application.Common.Localization;
using FluentAssertions;

namespace Aynesil.Tests.Localization;

public class LocaleResolverTests
{
    [Theory]
    [InlineData("en", null, "en")]
    [InlineData("tr-TR,tr;q=0.9,en;q=0.8", null, "tr")]
    [InlineData("en-US,en;q=0.9", "tr", "en")]          // header, JWT claim'inden önce gelir
    [InlineData("de,fr;q=0.8", "en", "en")]             // desteklenmeyen header → claim
    [InlineData(null, "en", "en")]
    [InlineData("", null, "tr")]                         // hiçbiri → varsayılan
    [InlineData("*", "xx", "tr")]
    [InlineData("tr;q=0.5,en;q=0.9", null, "en")]       // q-değeri sıralaması
    [InlineData("en;q=0,tr", null, "tr")]               // q=0 reddedilir
    public void Resolve_follows_header_then_claim_then_default(string? header, string? claim, string expected) =>
        LocaleResolver.Resolve(header, claim).Should().Be(expected);
}
