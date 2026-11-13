-- =============================================================================
-- Aynesil Flyway V49 — menü seed ↔ Vue Router hizalaması
-- =============================================================================
-- Etkin menü öğeleri ile frontend/aynesil-web/src/router/index.ts birebir karşılaştırıldı
-- (parametreli rotalar — camps/:id, students/:id — eşleşme sayılmadı; V31 sonrası kalanlar):
--
--   KAPAT : sayfası olmayan etkin öğeler (tıklayınca 404 / yanlış detay sayfası)
--     camps.activities   /camps/activities   — kamp etkinlik ekranı yok (kamp detayından yönetilmiyor)
--     reports            /reports            — genel rapor sayfası yok; raporlar modül altlarında
--     students-care-team /students/care-team — bakım ekibi ekranı yok (API var, UI yok)
--   DÜZELT: sayfası olan ama menüde olmayan öğeler
--     students-guardians → /guardians (V31 kapatmıştı; liste sayfası top-level /guardians'ta)
--     legal.reports              /legal/reports              (yeni)
--     notifications-preferences  /notifications/preferences  (yeni)
--     portal altına: portal-home /portal, portal-children /portal/children,
--                    portal-notifications /portal/notifications (yeni). Sidebar'da alt öğesi olan
--                    üst öğe yalnızca başlıktır; /portal bu yüzden kendi alt öğesiyle kalır.
--
-- Bilerek menüye eklenmeyenler: /account/password (başlıktaki kullanıcı rozetinden), /403, /404,
-- /consultancy/follow-ups/open (takip listesinin süzgeci, oradan açılıyor), */new ve :id sayfaları.
-- Idempotent: UPDATE'ler hedefli, INSERT'ler ON CONFLICT DO NOTHING.
-- =============================================================================

update iam.menu_item set is_active = false, updated_at = now()
 where corporation_id is null
   and code in ('camps.activities', 'reports', 'students-care-team')
   and is_active;

update iam.menu_item set route = '/guardians', is_active = true, updated_at = now()
 where corporation_id is null and code = 'students-guardians'
   and (route is distinct from '/guardians' or not is_active);

insert into iam.menu_item (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select null, parent.id, v.code, v.route, v.icon, v.sort_order, p.id, true
  from (values
    ('legal',         'legal.reports',             '/legal/reports',             'chart',         5,  'legal_report:read'),
    ('notifications', 'notifications-preferences', '/notifications/preferences', 'setting-2',     30, 'notification:read'),
    ('portal',        'portal-home',               '/portal',                    'home-2',        10, 'portal:access'),
    ('portal',        'portal-children',           '/portal/children',           'people',        20, 'portal:access'),
    ('portal',        'portal-notifications',      '/portal/notifications',      'bell',          30, 'portal:access')
  ) as v(parent_code, code, route, icon, sort_order, perm_code)
  join iam.menu_item parent on parent.code = v.parent_code and parent.corporation_id is null
  left join iam.permission p on p.code = v.perm_code
on conflict (corporation_id, code) do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
  from iam.menu_item m
  join (values
    ('legal.reports',             'tr', 'Raporlar'),
    ('legal.reports',             'en', 'Reports'),
    ('notifications-preferences', 'tr', 'Bildirim Tercihleri'),
    ('notifications-preferences', 'en', 'Preferences'),
    ('portal-home',               'tr', 'Özet'),
    ('portal-home',               'en', 'Overview'),
    ('portal-children',           'tr', 'Çocuklarım'),
    ('portal-children',           'en', 'My Children'),
    ('portal-notifications',      'tr', 'Bildirimler'),
    ('portal-notifications',      'en', 'Notifications')
  ) as t(code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
