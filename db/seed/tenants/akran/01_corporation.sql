-- =============================================================================
-- Tenant: Akran Hareket — Corporation bootstrap
-- Bu dosya V2'deki akran bootstrap ile aynı veriyi içerir (idempotent).
-- Temiz bir kurulumda V2 uygulandıktan sonra bu dosya çalıştırılırsa
-- ON CONFLICT ile atlanır; yeni bir ortamda standalone çalışabilir.
-- Owner rolüyle çalıştırın (RLS bypass gereklidir).
-- =============================================================================

insert into core.corporation (code, legal_name, display_name, default_locale, default_currency, timezone)
values ('akran', 'Akran Hareket Özel Eğitim', 'Akran Hareket', 'tr', 'TRY', 'Europe/Istanbul')
on conflict (code) do nothing;
