using System.Reflection;
using Aynesil.Application.Common.Interfaces;
using FluentAssertions;
using Aynesil.Application.Features.Camps.Commands;
using Aynesil.Application.Features.Consultancy.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// İyimser eşzamanlılık sözleşmesi (docs/raporlar/2026-11-17_dogrulama.md D2): istekte <c>RowVersion</c> taşıyan her
/// MediatR komutunun handler'ı bu değeri okumalıdır (projenin deseni: <c>entity.RowVersion != req.RowVersion</c> →
/// 422 <c>Error.Concurrency</c>). Önceden 20 komut alanı alıp doğrulayıcıda <c>&gt; 0</c> istiyor ama hiç
/// karşılaştırmıyordu; eşzamanlı düzenlemede son yazan uyarısız kazanıyordu.
/// <para>
/// Denetim IL düzeyindedir: handler sınıfının ve derleyicinin ürettiği async durum makinesinin (Handle → MoveNext)
/// gövdesinde isteğin <c>get_RowVersion</c> çağrısı aranır. Okumak karşılaştırmanın ön koşuludur; yanlış
/// karşılaştırma ise handler'a özgü testlerin işidir.
/// </para>
/// </summary>
public class RowVersionContractTests
{
    private static readonly Assembly ApplicationAssembly = typeof(ITenantContext).Assembly;

    /// <summary>Bilerek RowVersion'ı okumayan komutlar (gerekçesiyle). Şu an boş.</summary>
    private static readonly HashSet<string> Exempt = [];

    private static IEnumerable<(Type Request, Type Handler)> RequestsWithRowVersion()
    {
        var handlers = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType
                            && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                                || i.GetGenericTypeDefinition() == typeof(IRequestHandler<>)))
                .Select(i => (Request: i.GetGenericArguments()[0], Handler: t)));

        return handlers.Where(x => x.Request.GetProperty("RowVersion") is not null && !Exempt.Contains(x.Request.Name));
    }

    [Fact]
    public void There_are_commands_carrying_RowVersion()
        => RequestsWithRowVersion().Should().HaveCountGreaterThan(40);

    [Fact]
    public void Every_handler_reads_the_RowVersion_of_its_request()
    {
        var offenders = RequestsWithRowVersion()
            .Where(x => !ReadsRowVersion(x.Handler, x.Request))
            .Select(x => $"{x.Request.Name} → {x.Handler.Name}")
            .OrderBy(x => x)
            .ToList();

        offenders.Should().BeEmpty(
            "RowVersion alan komutun handler'ı onu entity.RowVersion ile karşılaştırmalı " +
            "(ValidationException + MsgKeys.Error.Concurrency); alan gerçekten gereksizse komuttan kaldırın");
    }

    [Fact]
    public void The_IL_scan_detects_a_known_good_and_a_known_bad_handler()
    {
        // Kendi kendini doğrulama: tarama, bilinen iyi örneği bulur, RowVersion'ı okumayan bir handler'ı kaçırmaz.
        ReadsRowVersion(typeof(GoodHandler), typeof(Probe)).Should().BeTrue();
        ReadsRowVersion(typeof(BadHandler), typeof(Probe)).Should().BeFalse();
    }

    private static bool ReadsRowVersion(Type handler, Type request)
    {
        var getter = request.GetProperty("RowVersion")!.GetMethod!;
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
                                 | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var methods = handler.GetMethods(all)
            .Concat(handler.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public).SelectMany(n => n.GetMethods(all)));

        foreach (var method in methods)
        {
            var il = method.GetMethodBody()?.GetILAsByteArray();
            if (il is null) continue;
            for (var i = 0; i + 4 < il.Length; i++)
            {
                if (il[i] is not (0x28 or 0x6F)) continue;   // call / callvirt
                var token = BitConverter.ToInt32(il, i + 1);
                try
                {
                    if (method.Module.ResolveMethod(token) is MethodInfo called
                        && called.Name == getter.Name && called.DeclaringType == getter.DeclaringType)
                        return true;
                }
                catch (ArgumentException) { /* işlenen değil, rastgele bayt dizisi */ }
                catch (BadImageFormatException) { }
            }
        }
        return false;
    }

    // ── Davranış: bu turda ekranı yapılan güncellemeler bayat sürümü reddeder ──

    [Fact]
    public async Task Stale_RowVersion_on_camp_update_is_rejected_and_nothing_changes()
    {
        using var ctx = new TenantTestContext();
        var camp = Aynesil.Domain.Modules.Camps.Entities.Camp.Create(TenantTestContext.TenantA, "yaz", "Yaz Kampı");
        await ctx.SeedAsync(camp);

        var stale = () => ctx.Handler<UpdateCampCommandHandler>().Handle(
            new UpdateCampCommand(camp.Id, "Başkası", null, null, null, null, null, camp.RowVersion + 1), default);
        var ex = await stale.Should().ThrowAsync<Aynesil.Application.Common.Exceptions.ValidationException>();
        ex.Which.Errors.Should().ContainKey("RowVersion");
        (await ctx.Db.Camps.AsNoTracking().SingleAsync()).Name.Should().Be("Yaz Kampı");

        await ctx.Handler<UpdateCampCommandHandler>().Handle(
            new UpdateCampCommand(camp.Id, "Güncel", null, null, null, null, null, camp.RowVersion), default);
        (await ctx.Db.Camps.AsNoTracking().SingleAsync()).Name.Should().Be("Güncel");
    }

    [Fact]
    public async Task Stale_RowVersion_on_institution_update_is_rejected()
    {
        using var ctx = new TenantTestContext();
        var school = Aynesil.Domain.Modules.Consultancy.Entities.Institution.Create(TenantTestContext.TenantA, "Okul");
        await ctx.SeedAsync(school);

        var stale = () => ctx.Handler<UpdateInstitutionCommandHandler>().Handle(
            new UpdateInstitutionCommand(school.Id, "Başka Okul", null, null, null, null, null, null, school.RowVersion + 1), default);
        await stale.Should().ThrowAsync<Aynesil.Application.Common.Exceptions.ValidationException>();
    }

    // ── Kendi kendini doğrulama için örnekler ──
    public sealed record Probe(Guid Id, int RowVersion) : IRequest;
    private sealed class Entity { public int RowVersion { get; init; } = 1; }

    private sealed class GoodHandler : IRequestHandler<Probe>
    {
        public async Task Handle(Probe req, CancellationToken ct)
        {
            await Task.Yield();
            var e = new Entity();
            if (e.RowVersion != req.RowVersion) throw new InvalidOperationException();
        }
    }

    private sealed class BadHandler : IRequestHandler<Probe>
    {
        public async Task Handle(Probe req, CancellationToken ct)
        {
            await Task.Yield();
            _ = req.Id;
        }
    }
}
