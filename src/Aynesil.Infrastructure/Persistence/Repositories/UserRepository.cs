using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Iam.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>.
/// iam.user_account carries corporation_id, so PostgreSQL RLS restricts
/// rows to the current tenant context set by TenantConnectionInterceptor.
/// The EF Core global query filter (deleted_at IS NULL) provides an application-level soft-delete guard.
/// </summary>
internal sealed class UserRepository : GenericRepository<UserAccount>, IUserRepository
{
    public UserRepository(AynesilDbContext context) : base(context) { }

    public async Task<UserAccount?> GetByUsernameAsync(
        Guid corporationId, string username, CancellationToken ct = default) =>
        await Set
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.CorporationId == corporationId &&
                u.Username == username.ToLowerInvariant(), ct);

    public async Task<UserAccount?> GetByEmailAsync(
        Guid corporationId, string email, CancellationToken ct = default) =>
        await Set
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.CorporationId == corporationId &&
                u.Email == email.ToLowerInvariant(), ct);

    public async Task<bool> IsUsernameTakenAsync(
        Guid corporationId, string username, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalized = username.ToLowerInvariant();
        return await Set.AnyAsync(u =>
            u.CorporationId == corporationId &&
            u.Username == normalized &&
            (excludeId == null || u.Id != excludeId), ct);
    }

    public async Task<bool> IsEmailTakenAsync(
        Guid corporationId, string email, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalized = email.ToLowerInvariant();
        return await Set.AnyAsync(u =>
            u.CorporationId == corporationId &&
            u.Email == normalized &&
            (excludeId == null || u.Id != excludeId), ct);
    }

    public async Task<IReadOnlyList<UserAccount>> GetByStatusAsync(
        Guid corporationId, string status, CancellationToken ct = default) =>
        await Set
            .AsNoTracking()
            .Where(u => u.CorporationId == corporationId && u.Status == status)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

    public async Task<UserAccount?> GetWithRolesAsync(Guid userId, CancellationToken ct = default) =>
        await Set
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
}
