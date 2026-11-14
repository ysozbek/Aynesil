-- =====================================================================
-- V54 — Akademik dönemler menüsü
--
-- V31 `plans-academic` menü öğesini, sayfası olmadığı için pasife almıştı. Dönem yönetimi ekranı
-- (liste / oluştur / düzenle / güncel dönem yap / sil) artık `/bep/academic-periods` yolunda;
-- öğe açılır ve yola hizalanır. Yalnızca veri, idempotent.
-- =====================================================================
update iam.menu_item
   set route      = '/bep/academic-periods',
       is_active  = true,
       updated_at = now()
 where code = 'plans-academic'
   and corporation_id is null
   and (route is distinct from '/bep/academic-periods' or is_active is distinct from true);
