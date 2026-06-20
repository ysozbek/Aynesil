-- =============================================================================
-- Tenant Bootstrap Template
-- Kopyalayın: cp -r db/seed/tenants/_template db/seed/tenants/<tenant_code>
-- <TENANT_CODE>, <LEGAL_NAME>, <DISPLAY_NAME> alanlarını doldurun.
-- Owner rolüyle çalıştırın (RLS bypass).
-- =============================================================================

insert into core.corporation (code, legal_name, display_name, default_locale, default_currency, timezone)
values (
  '<tenant_code>',      -- küçük harf, benzersiz (ör: 'ozelkent')
  '<Legal Adı>',        -- resmi tüzel kişilik adı
  '<Görünen Adı>',      -- uygulama içinde gösterilecek kısa ad
  'tr',                 -- varsayılan locale
  'TRY',                -- varsayılan para birimi
  'Europe/Istanbul'     -- timezone
)
on conflict (code) do nothing;
