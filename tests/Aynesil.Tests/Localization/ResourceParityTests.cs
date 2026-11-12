using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using Aynesil.Application.Common.Localization;
using Aynesil.Domain.Common;
using FluentAssertions;

namespace Aynesil.Tests.Localization;

/// <summary>tr ve en kaynak dosyalarının eşliği ve MsgKeys ↔ kaynak uyumu.</summary>
public class ResourceParityTests
{
    private static readonly Assembly AppAssembly = typeof(Msg).Assembly;

    public static IEnumerable<object[]> Resources =>
        new[] { "Messages", "FieldNames", "EnumLabels" }.Select(n => new object[] { n });

    internal static Dictionary<string, string> Load(string name, string culture)
    {
        var rm = new ResourceManager($"Aynesil.Application.Resources.{name}", AppAssembly);
        // tr nötr (ana assembly); en uydu assembly — tryParents=false: yalnızca o dilin kendi girdileri.
        var set = rm.GetResourceSet(CultureInfo.GetCultureInfo(culture), createIfNotExists: true, tryParents: culture == "tr");
        set.Should().NotBeNull($"{name} ({culture}) kaynağı bulunmalı");
        return set!.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
    }

    private static IEnumerable<string> Placeholders(string text) =>
        Regex.Matches(text, @"\{(\d+)\}").Select(m => m.Value).Distinct().OrderBy(x => x);

    [Theory]
    [MemberData(nameof(Resources))]
    public void Tr_and_en_have_the_same_keys(string name)
    {
        var tr = Load(name, "tr");
        var en = Load(name, "en");

        en.Keys.Except(tr.Keys).Should().BeEmpty($"{name}: yalnızca en'de olan anahtarlar");
        tr.Keys.Except(en.Keys).Should().BeEmpty($"{name}: en çevirisi eksik anahtarlar");
        tr.Values.Concat(en.Values).Should().OnlyContain(v => !string.IsNullOrWhiteSpace(v), "boş çeviri olmamalı");
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public void Tr_and_en_use_the_same_placeholders(string name)
    {
        var tr = Load(name, "tr");
        var en = Load(name, "en");
        foreach (var key in tr.Keys.Intersect(en.Keys))
            Placeholders(en[key]).Should().Equal(Placeholders(tr[key]), $"{name}:{key} yer tutucuları");
    }

    [Fact]
    public void Every_MsgKeys_constant_exists_in_both_languages()
    {
        var tr = Load("Messages", "tr");
        var en = Load("Messages", "en");
        var keys = AllKeys(typeof(MsgKeys)).ToList();

        keys.Should().NotBeEmpty();
        keys.Should().OnlyHaveUniqueItems();
        keys.Except(tr.Keys).Should().BeEmpty("MsgKeys sabiti tr kaynağında yok");
        keys.Except(en.Keys).Should().BeEmpty("MsgKeys sabiti en kaynağında yok");
    }

    [Fact]
    public void Every_Messages_resource_key_is_declared_in_MsgKeys()
    {
        var keys = AllKeys(typeof(MsgKeys)).ToHashSet();
        Load("Messages", "tr").Keys.Except(keys).Should().BeEmpty("kaynakta olup MsgKeys'te olmayan (kullanılmayan) anahtar");
    }

    internal static IEnumerable<string> AllKeys(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(MsgKey))
            .Select(f => ((MsgKey)f.GetValue(null)!).Value)
            .Concat(type.GetNestedTypes(BindingFlags.Public).SelectMany(AllKeys));

    [Fact]
    public void Msg_resolves_per_culture_and_substitutes_arguments()
    {
        var tr = CultureInfo.GetCultureInfo("tr");
        var en = CultureInfo.GetCultureInfo("en");

        Msg.Get(MsgKeys.Error.Forbidden, tr).Should().Be("Bu işlem için yetkiniz yok.");
        Msg.Get(MsgKeys.Error.Forbidden, en).Should().Contain("permission");
        Msg.Get(MsgKeys.Error.NotFound, en, "Student").Should().Be("Student was not found.");
        Msg.Field("new_start_at", en).Should().Be("New start time");            // snake_case → kayıtlı etiket
        Msg.Field("some_unknown_field", en).Should().Be("Some Unknown Field"); // kayıtsız → okunur hale getirilir
        Msg.Field("Email", tr).Should().Be("E-posta");
        Msg.Enum("Outcome", "on_track", en).Should().Be("on track");
        Msg.Enum("Outcome", "unknown_code", en).Should().Be("unknown_code");
    }
}
