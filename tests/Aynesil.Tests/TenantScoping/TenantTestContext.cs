using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Shared setup for handler tests that verify CorporationId / actor ids are taken from the
/// request context (JWT) rather than the client payload.
///
/// Uses the EF Core InMemory provider, which has no RLS. Cross-tenant rejection therefore
/// proves the handler's own explicit CorporationId filter, independent of the DB policies.
/// </summary>
public sealed class TenantTestContext : IDisposable
{
    public static readonly Guid TenantA = Guid.CreateVersion7();
    public static readonly Guid TenantB = Guid.CreateVersion7();

    public Guid UserId { get; } = Guid.CreateVersion7();

    public AynesilDbContext Db { get; }
    public Mock<ITenantContext> Tenant { get; } = new();
    public Mock<ICurrentUserService> CurrentUser { get; } = new();

    public TenantTestContext(Guid? corporationId = null)
    {
        var options = new DbContextOptionsBuilder<AynesilDbContext>()
            .UseInMemoryDatabase($"tenant-scoping-{Guid.NewGuid()}")
            .Options;
        Db = new InMemoryAynesilDbContext(options);

        Tenant.SetupGet(t => t.CorporationId).Returns(corporationId ?? TenantA);
        CurrentUser.SetupGet(u => u.UserId).Returns(UserId);
        CurrentUser.SetupGet(u => u.IsAuthenticated).Returns(true);
        Tenant.SetupGet(t => t.Locale).Returns("tr");
        Tenant.SetupGet(t => t.Timezone).Returns("Europe/Istanbul");
    }

    /// <summary>
    /// Builds a handler with this context's DbContext, tenant and current user; any other
    /// dependency (ISender, loggers, services) is a loose mock / null logger.
    /// </summary>
    public T Handler<T>() where T : class => (T)Handler(typeof(T));

    public object Handler(Type handlerType)
    {
        var ctor = handlerType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        return ctor.Invoke(ctor.GetParameters().Select(p => Resolve(p.ParameterType)).ToArray());
    }

    private object Resolve(Type type)
    {
        if (type == typeof(IAppDbContext)) return Db;
        if (type == typeof(ITenantContext)) return Tenant.Object;
        if (type == typeof(ICurrentUserService)) return CurrentUser.Object;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ILogger<>))
            return Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(type.GetGenericArguments()))!;
        var mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type))!;
        return mock.Object;
    }

    /// <summary>
    /// Asserts the call is rejected the way handlers reject a parent record outside the tenant
    /// (not found / invalid / forbidden) — not by an unrelated crash.
    /// </summary>
    public static async Task ShouldRejectAsync(Func<Task> act)
    {
        var ex = await Record.ExceptionAsync(act);
        ex.Should().NotBeNull("a parent record of another tenant must be rejected");
        ex.Should().Match<Exception>(e =>
            e is KeyNotFoundException || e is NotFoundException || e is InvalidOperationException
            || e is ForbiddenAccessException,
            $"expected a not-found/invalid/forbidden rejection but got {ex!.GetType().Name}: {ex.Message}");
    }

    /// <summary>Adds the given entities and saves, then clears the change tracker.</summary>
    public async Task SeedAsync(params object[] entities)
    {
        Db.AddRange(entities);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
    }

    /// <summary>A core.corporation row with the given id (Id has a protected setter).</summary>
    public static Aynesil.Domain.Modules.Core.Entities.Corporation NewCorporation(Guid id)
    {
        var corporation = new Aynesil.Domain.Modules.Core.Entities.Corporation
        {
            Code = id.ToString("N")[..8], LegalName = "Kurum", DisplayName = "Kurum",
        };
        typeof(Aynesil.Domain.Modules.Core.Entities.Corporation)
            .GetProperty(nameof(corporation.Id))!.SetValue(corporation, id);
        return corporation;
    }

    public void Dispose() => Db.Dispose();
}
