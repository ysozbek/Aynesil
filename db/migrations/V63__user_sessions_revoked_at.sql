-- =============================================================================
-- AyNesil Platform :: Flyway V63 — Kullanıcı oturumlarının anında iptali
-- =============================================================================
-- Sorun (docs/raporlar/2026-11-17_dogrulama.md D1): şifre sıfırlama, askıya alma ve
--   silme yalnızca yenileme jetonlarını (iam.auth_session) iptal ediyordu; elde kalan
--   erişim jetonu (JWT) süresi dolana kadar geçerliydi. İzinler JWT içinde taşındığı için
--   rol/izin değişikliği de aynı süre gecikmeli etkili oluyordu.
--
-- Çözüm: iam.user_account.sessions_revoked_at. Şifre değiştirme/sıfırlama, askıya alma,
--   devre dışı bırakma, silme ve rol/izin değişikliği bu değeri şimdiki zamana çeker.
--   API (UserSessionMiddleware) her istekte JWT "iat_ms" < sessions_revoked_at ise ya da
--   kullanıcı aktif değil/silinmişse 401 döner (kullanıcı başına 1 dk önbellek; değeri
--   değiştiren işlemler önbelleği temizler). NULL = hiç iptal edilmedi.
-- =============================================================================

alter table iam.user_account
  add column if not exists sessions_revoked_at timestamptz;

comment on column iam.user_account.sessions_revoked_at is
  'Bu andan önce verilmiş erişim jetonları (JWT iat_ms) geçersizdir (V63). Şifre değişimi/sıfırlama, askıya alma, silme ve rol/izin değişikliğinde güncellenir; NULL = iptal yok.';
