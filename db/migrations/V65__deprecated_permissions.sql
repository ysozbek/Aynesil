-- =============================================================================
-- AyNesil Platform :: Flyway V65 — Kullanımdan kalkmış izinler rollerden kaldırıldı
-- =============================================================================
-- Sorun (docs/raporlar/2026-11-17_dogrulama.md D10): V2/V6'nın kaba izinleri
--   finance:read, finance:write, session:write, refdata:manage hiçbir uç tarafından
--   istenmiyor (yerlerini kaynak bazlı izinler aldı) ama "tüm izinler" ile açılan
--   rollerde duruyor; rol ekranında anlamsız seçenek olarak görünüyordu.
--   Tek kullanım: Finans menüsünün (code = 'payments', rota /finance) gerekli izni
--   hâlâ finance:read idi.
--
-- Çözüm:
--   1) 'payments' menüsü, /finance rotasının (FinanceDashboardView) istediği
--      finance_payment:read iznine bağlanır — izin rollerden kalkınca menü kaybolmasın.
--   2) Dört izin bütün rollerden kaldırılır (global ve kurum rolleri).
--   İzin satırları (iam.permission) silinmez; API listede göstermez ve rollere atamaz
--   (Permissions.Deprecated), new_tenant.py ve şablon seed vermez.
-- =============================================================================

update iam.menu_item m
   set required_permission_id = p.id,
       updated_at             = now()
  from iam.permission p
 where m.code = 'payments'
   and m.corporation_id is null
   and p.code = 'finance_payment:read'
   and m.required_permission_id is distinct from p.id;

delete from iam.role_permission rp
 using iam.permission p
 where p.id = rp.permission_id
   and p.code in ('finance:read', 'finance:write', 'session:write', 'refdata:manage');

do $$
begin
  if exists (select 1
               from iam.menu_item m
               join iam.permission p on p.id = m.required_permission_id
              where p.code in ('finance:read', 'finance:write', 'session:write', 'refdata:manage')) then
    raise exception 'V65: kullanımdan kalkmış bir izne bağlı menü öğesi kaldı; önce menüyü güncel bir izne bağlayın.';
  end if;
end $$;
