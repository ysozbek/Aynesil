using System.Reflection;
using System.Text.RegularExpressions;
using Aynesil.Application.Common.Validation;
using FluentAssertions;

namespace Aynesil.Tests.Localization;

/// <summary>Kodda kalan sabit metin ve eksik kaynak karşılıklarını yakalayan kaynak/yansıma testleri.</summary>
public class MessageKeyCoverageTests
{
    internal static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Aynesil.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Aynesil.sln bulunamadı.");
    }

    internal static IEnumerable<string> SourceFiles(string relative) =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), relative), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    [Fact]
    public void No_validator_uses_a_hardcoded_WithMessage()
    {
        var offenders = SourceFiles("src/Aynesil.Application/Features")
            .Where(f => File.ReadAllText(f).Contains(".WithMessage("))
            .Select(Path.GetFileName);

        offenders.Should().BeEmpty("doğrulama mesajları WithKey(MsgKeys.…) ile kaynak anahtarı kullanmalı");
    }

    [Fact]
    public void Every_EnumSets_code_has_a_label_in_both_languages()
    {
        var tr = ResourceParityTests.Load("EnumLabels", "tr");
        var en = ResourceParityTests.Load("EnumLabels", "en");
        var sets = typeof(EnumSets).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (Msg: f.Name, Set: (Aynesil.Application.Common.Localization.Msg.EnumSet)f.GetValue(null)!))
            .ToList();

        sets.Should().NotBeEmpty();
        foreach (var (name, set) in sets)
        {
            set.Group.Should().Be(name, "alan adı grup adıyla aynı olmalı");
            foreach (var code in set.Codes)
            {
                tr.Should().ContainKey($"{set.Group}.{code}");
                en.Should().ContainKey($"{set.Group}.{code}");
            }
        }
    }

    [Fact]
    public void Every_FieldArg_and_FieldList_name_used_in_validators_has_a_FieldNames_entry()
    {
        var tr = ResourceParityTests.Load("FieldNames", "tr");
        var names = SourceFiles("src/Aynesil.Application/Features")
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"Msg\.(?:FieldArg|Field)\(""(\w+)""\)").Select(m => m.Groups[1].Value)
                .Concat(Regex.Matches(File.ReadAllText(f), @"Msg\.FieldList\(([^)]*)\)")
                    .SelectMany(m => Regex.Matches(m.Groups[1].Value, @"""(\w+)""").Select(x => x.Groups[1].Value))))
            .Distinct()
            .ToList();

        names.Should().NotBeEmpty();
        names.Where(n => !tr.ContainsKey(n)).Should().BeEmpty("FieldNames.resx'te karşılığı olmayan alan etiketi");
    }

    [Fact]
    public void Every_Validation_key_is_used_by_some_validator()
    {
        var used = string.Join('\n', SourceFiles("src/Aynesil.Application/Features").Select(File.ReadAllText));
        var keys = ResourceParityTests.AllKeys(typeof(Aynesil.Application.Common.Localization.MsgKeys.Validation));

        keys.Select(k => k.Replace("Validation.", "")).Where(k => !used.Contains($"MsgKeys.Validation.{k}"))
            .Should().BeEmpty("kullanılmayan doğrulama anahtarı");
    }
}
