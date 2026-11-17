namespace Aynesil.Application.Common.Interfaces;

/// <summary>
/// Kullanıcı oturumlarının anında iptali (V63, <c>iam.user_account.sessions_revoked_at</c>).
/// <para>
/// Erişim jetonu (JWT) izinleri içinde taşır ve süresi dolana kadar imzası geçerlidir; API her istekte
/// (UserSessionMiddleware) jetonun <c>iat</c> değerini kullanıcının <c>sessions_revoked_at</c>'i ve durumuyla
/// karşılaştırır. Sonuç kullanıcı başına kısa süre önbelleklenir; değeri değiştiren her işlem önbelleği temizler.
/// </para>
/// Kullanıcıya özgü işlemler (şifre değişimi/sıfırlama, askıya alma, silme) değeri domain'de ayarlar
/// (<c>UserAccount.RevokeSessions</c>) ve kaydettikten sonra <see cref="Forget"/> çağırır. Rol/izin değişikliği
/// birden çok kullanıcıyı etkiler: <see cref="RevokeAccessTokensAsync"/> / <see cref="RevokeRoleMembersAsync"/>.
/// Yenileme jetonları bunlarla iptal edilmez: istemci 401 alınca yeniler ve güncel izinlerle yeni jeton alır.
/// </summary>
public interface ISessionRevocation
{
    /// <summary>Kullanıcının önbelleklenmiş oturum durumunu atar (durumu/iptal zamanı kaydedildikten sonra).</summary>
    void Forget(Guid userId);

    /// <summary>Kullanıcıların şu ana kadar verilmiş erişim jetonlarını geçersiz kılar ve önbelleği temizler.</summary>
    Task RevokeAccessTokensAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);

    /// <summary>Rolün bütün üyelerinin erişim jetonlarını geçersiz kılar (rolün izinleri değişti).</summary>
    Task RevokeRoleMembersAsync(Guid roleId, CancellationToken ct);
}
