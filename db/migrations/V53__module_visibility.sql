-- =====================================================================
-- V53 — Kurum bazında modül görünürlüğü
--
-- Mekanizma: core.corporation.settings -> {"modules": {"camps": true, ...}}
--   • Anahtar yoksa modül AÇIK sayılır; kapatmak için açıkça false yazılır.
--   • Anahtarı yalnızca platform yazar (devops/set_modules.py, devops/new_tenant.py);
--     kurum yöneticisinin ayar güncellemesi `modules`'ü yok sayar (API).
--   • API: [RequireModule] -> 403 Error.ModuleDisabled. Menü: iam.menu_item.feature_flag
--     modül koduyla eşleşir; kapalı modülün düğümü (ve alt öğeleri) menüden düşer.
--
-- Modül kodları: camps, consultancy, camera, surveys, care_team, kpi
-- (Aynesil.Shared/Constants/Modules.cs ile eşgüdümlü.)
-- Şema değişikliği yoktur; yalnızca veri. Idempotent.
-- =====================================================================

-- ---------------------------------------------------------------------
-- 1) Menü düğümlerine modül bayrağı (platform varsayılan menüsü: corporation_id IS NULL)
--    Kök kodlar + altındaki tüm öğeler + modül yolu önekiyle eşleşen rotalar.
-- ---------------------------------------------------------------------
with recursive modules(module_code, root_code) as (
  values
    ('camps',       'camps'),
    ('camps',       'camp-management'),
    ('consultancy', 'consultancy'),
    ('camera',      'cameras'),
    ('camera',      'camera-management'),
    ('kpi',         'performance'),
    ('kpi',         'performance_kpi'),
    ('surveys',     'students-surveys'),
    ('surveys',     'students-feedback'),
    ('care_team',   'students-care-team')
),
tree as (
  select m.id, mo.module_code
    from iam.menu_item m
    join modules mo on mo.root_code = m.code
   where m.corporation_id is null
  union
  select c.id, t.module_code
    from iam.menu_item c
    join tree t on c.parent_id = t.id
   where c.corporation_id is null
),
by_route(id, module_code) as (
  select m.id,
         case
           when m.route like '/camps%'                       then 'camps'
           when m.route like '/consultancy%'                 then 'consultancy'
           when m.route like '/cameras%'                     then 'camera'
           when m.route like '/kpi%'
             or m.route like '/performance-kpi%'             then 'kpi'
           when m.route like '/students/surveys%'
             or m.route like '/students/feedback%'
             or m.route like '/surveys%'                     then 'surveys'
           when m.route like '/students/care-team%'
             or m.route like '/care-team%'                   then 'care_team'
         end
    from iam.menu_item m
   where m.corporation_id is null
     and m.route is not null
),
flags as (
  select distinct on (id) id, module_code
    from (select id, module_code from tree
          union all
          select id, module_code from by_route where module_code is not null) x
   order by id, module_code
)
update iam.menu_item m
   set feature_flag = f.module_code,
       updated_at   = now()
  from flags f
 where m.id = f.id
   and m.feature_flag is distinct from f.module_code;

-- ---------------------------------------------------------------------
-- 2) Mevcut kurumlar: modüller açık kalır (açık değer yazılır; zaten yazılı anahtarlar korunur).
--    Yeni kurumlar new_tenant.py ile bu modüller kapalı başlar.
-- ---------------------------------------------------------------------
update core.corporation c
   set settings = jsonb_set(
         coalesce(c.settings, '{}'::jsonb),
         '{modules}',
         jsonb_build_object(
           'camps', true, 'consultancy', true, 'camera', true,
           'surveys', true, 'care_team', true, 'kpi', true)
         || coalesce(c.settings -> 'modules', '{}'::jsonb),
         true)
 where c.settings -> 'modules' is distinct from
       (jsonb_build_object(
          'camps', true, 'consultancy', true, 'camera', true,
          'surveys', true, 'care_team', true, 'kpi', true)
        || coalesce(c.settings -> 'modules', '{}'::jsonb));
