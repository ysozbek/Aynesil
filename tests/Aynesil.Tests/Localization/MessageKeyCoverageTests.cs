using Aynesil.Domain.Common;
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
    public void Every_MsgKeys_constant_is_used_in_source()
    {
        var src = string.Join('\n', SourceFiles("src").Where(f => !f.EndsWith("MsgKeys.cs")).Select(File.ReadAllText));
        var unused = new List<string>();
        foreach (var group in typeof(Aynesil.Domain.Common.MsgKeys).GetNestedTypes())
            foreach (var field in group.GetFields(BindingFlags.Public | BindingFlags.Static))
                if (!src.Contains($"MsgKeys.{group.Name}.{field.Name}"))
                    unused.Add($"{group.Name}.{field.Name}");

        unused.Should().BeEmpty("kaynakta olup kodda kullanılmayan anahtar (ölü çeviri)");
    }

    [Fact]
    public void Business_rule_exceptions_are_always_thrown_with_a_MsgKey()
    {
        // Metin istek dilinde middleware'de çözülür; ham metinle atılan istisna çevrilemez.
        var rx = new Regex(@"throw new (BusinessRuleViolationException|ConflictException|ForbiddenAccessException)\(\s*(?!MsgKeys\.)(?<arg>[^)]{0,80})");
        var offenders = SourceFiles("src")
            .SelectMany(f => rx.Matches(File.ReadAllText(f)).Select(m => $"{Path.GetFileName(f)}: {m.Groups["arg"].Value.Trim()}"))
            .ToList();

        offenders.Should().BeEmpty("BusinessRule/Conflict/Forbidden istisnaları MsgKeys.* ile atılmalı");
    }

    [Fact]
    public void Manual_ValidationException_failures_are_created_through_ValidationFailures_For()
    {
        var rx = new Regex(@"throw new ValidationException\(\s*\[\s*new\s");
        var offenders = SourceFiles("src").Where(f => rx.IsMatch(File.ReadAllText(f))).Select(Path.GetFileName);

        offenders.Should().BeEmpty("elle üretilen hatalar ValidationFailures.For(...) ile kodlu olmalı");
    }

    [Fact]
    public void Every_literal_EnumValue_used_in_source_has_a_label_in_both_languages()
    {
        var tr = ResourceParityTests.Load("EnumLabels", "tr");
        var en = ResourceParityTests.Load("EnumLabels", "en");
        var rx = new Regex(@"new EnumValue\(""(\w+)"",\s*""([^""]+)""\)");
        var missing = SourceFiles("src")
            .SelectMany(f => rx.Matches(File.ReadAllText(f)).Select(m => $"{m.Groups[1].Value}.{m.Groups[2].Value}"))
            .Distinct()
            .Where(k => !tr.ContainsKey(k) || !en.ContainsKey(k))
            .ToList();

        missing.Should().BeEmpty("EnumLabels.resx'te (tr+en) karşılığı olmayan EnumValue");
    }
}
