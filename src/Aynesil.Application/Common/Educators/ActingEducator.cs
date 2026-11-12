using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Common.Educators;

/// <summary>
/// "Actor = signed-in user" for columns whose FK points at educators.educator, not iam.user_account.
/// The educator is resolved from the session (educators.educator.user_id), never from the payload.
/// </summary>
public static class ActingEducator
{
    /// <summary>The educator record linked to the user in this tenant, or null.</summary>
    public static async Task<Guid?> FindIdAsync(
        IAppDbContext db, Guid corporationId, Guid? userId, CancellationToken ct)
    {
        if (!userId.HasValue) return null;

        return await db.Educators
            .AsNoTracking()
            .Where(e => e.CorporationId == corporationId && e.UserId == userId.Value)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Like <see cref="FindIdAsync"/>, but a user with no educator record is a 422 (the request is
    /// well-formed, the signed-in user simply cannot perform an educator action).
    /// </summary>
    public static async Task<Guid> RequireIdAsync(
        IAppDbContext db, Guid corporationId, Guid? userId, CancellationToken ct)
        => await FindIdAsync(db, corporationId, userId, ct)
           ?? throw new ValidationException([new ValidationFailure(
               "educator", "Bu işlem için oturumdaki kullanıcıya bağlı bir eğitmen kaydı gerekir.")]);

    /// <summary>Display names (first + last) for educator ids; unknown ids are absent.</summary>
    public static async Task<Dictionary<Guid, string>> LoadNamesAsync(
        IAppDbContext db, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        var rows = await db.Educators.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .ToListAsync(ct);
        return rows.ToDictionary(e => e.Id, e => $"{e.FirstName} {e.LastName}".Trim());
    }
}
