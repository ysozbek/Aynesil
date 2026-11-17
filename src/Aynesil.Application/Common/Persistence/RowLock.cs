using Aynesil.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Common.Persistence;

/// <summary>
/// Satır kilidi altında çalışma: işlem açılır, önce verilen satır <c>SELECT … FOR UPDATE</c> ile kilitlenir, sonra
/// iş yapılır ve işlem kaydedilir. Aynı satırı kilitleyen eşzamanlı istekler sırayla çalışır ("say, sonra ekle"
/// kontrolleri — ör. kamp kontenjanı — yarışa girmez; D6).
/// İlişkisel olmayan sağlayıcıda (birim testlerindeki InMemory) kilit ve işlem yoktur; iş doğrudan çalışır.
/// </summary>
public static class RowLock
{
    public static async Task<T> RunAsync<T>(
        IAppDbContext db, FormattableString lockSql, Func<Task<T>> work, CancellationToken ct)
    {
        if (!db.Database.IsRelational()) return await work();

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync(lockSql, ct);
            var result = await work();
            await tx.CommitAsync(ct);
            return result;
        });
    }

    /// <summary>Kamp dönemi satırını kilitler (kayıt ve bekleme listesinden alma; kontenjan kontrolü).</summary>
    public static Task<T> CampPeriodAsync<T>(IAppDbContext db, Guid campPeriodId, Func<Task<T>> work, CancellationToken ct)
        => RunAsync(db, $"select 1 from camps.camp_period where id = {campPeriodId} for update", work, ct);
}
