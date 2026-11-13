-- =============================================================================
-- Aynesil Flyway V48 — eğitmen/veli ↔ kullanıcı hesabı bağı
-- =============================================================================
-- educators.educator.user_id ve students.guardian.user_id baştan beri vardı (V1) ve kurum içi
-- composite FK ile korunuyor (V39–V40: fk_educator_user_id_tnt, fk_guardian_user_id_tnt).
-- Bağı kuran API artık var (EducatorsController / GuardiansController ".../user-account"):
--
--   1. Bir kullanıcı en fazla BİR eğitmen kaydına ve en fazla BİR veli kaydına bağlanır.
--      ActingEducator (seans notu, BEP onayı…) eğitmeni oturumdaki kullanıcıdan çözer; iki kayıt
--      aynı kullanıcıya bağlı olsaydı yazar belirsizleşirdi. Silinmiş (deleted_at) kayıtlar sayılmaz.
--   2. Her kuruma "guardian" (Veli (portal)) rolü: yeni veli hesabı bu rolle açılır. Rol yalnızca
--      portalın gerektirdiği izinleri taşır. Kurum rolü değiştirebilir/silebilir (is_system = false).
--      Yeni kurumlar aynı rolü devops/new_tenant.py ile alır.
-- =============================================================================

create unique index if not exists uq_educator_user
  on educators.educator (user_id) where user_id is not null and deleted_at is null;

create unique index if not exists uq_guardian_user
  on students.guardian (user_id) where user_id is not null and deleted_at is null;

insert into iam.role (corporation_id, code, name, description, is_system)
select c.id, 'guardian', 'Veli (portal)', 'Veli portalı hesabı: yalnızca portal izinleri', false
  from core.corporation c
 where c.deleted_at is null
on conflict (corporation_id, code) do nothing;

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
  from iam.role r
  join iam.permission p on p.code in ('portal:access', 'parent_feedback:create', 'survey:respond', 'notification:read')
 where r.code = 'guardian' and r.corporation_id is not null
on conflict do nothing;
