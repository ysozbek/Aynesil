-- =====================================================================
-- AyNesil Platform :: Seed — başlangıç izin kataloğu (platform verisi)
-- Flyway eşdeğeri: db/migrations/V2__seed_data.sql (son bölüm).
-- Kalan izinler modül migration'larıyla gelir.
--
-- Bu dosya eskiden örnek bir kurumu da açıyordu (02_akran_bootstrap.sql: akran kurumu,
-- kampüsler, admin rolü, kuruma özel referans değerleri). Kurum verisi platform seed'inden
-- çıkarıldı; kurumlar devops/new_tenant.py ile, akran db/seed/tenants/akran/create.sh ile açılır.
-- =====================================================================

insert into iam.permission(code, resource, action) values
  ('student:read','student','read'),
  ('student:write','student','write'),
  ('session:read','session','read'),
  ('session:write','session','write'),
  ('finance:read','finance','read'),
  ('finance:write','finance','write'),
  ('refdata:manage','refdata','manage')
on conflict (code) do nothing;
