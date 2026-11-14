-- =====================================================================
-- V55 — Seans notu düzenleme izi
--
-- Veli portalı parent_visible notları gösterdiği için, notun metni sonradan değiştiyse
-- velinin "düzenlendi" görebilmesi gerekir. edited_at/edited_by yalnızca not METNİ
-- değiştiğinde dolar (salt görünürlük değişikliği düzenleme sayılmaz; o iz core.audit_log'ta).
-- edited_by: iam.user_account.id (düzenleyen kullanıcı; FK yok — created_by/recorded_by gibi).
-- Geçmiş notlar NULL kalır (düzenlenmemiş). Idempotent.
-- =====================================================================
alter table scheduling.session_note add column if not exists edited_at timestamptz;
alter table scheduling.session_note add column if not exists edited_by uuid;
comment on column scheduling.session_note.edited_at is 'Son metin düzenlemesi; NULL = hiç düzenlenmedi.';
comment on column scheduling.session_note.edited_by is 'Metni son düzenleyen kullanıcı (iam.user_account.id).';
