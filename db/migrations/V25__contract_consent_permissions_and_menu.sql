-- =============================================================================
-- AyNesil Platform :: Flyway V25 — Contract & Consent Management
-- =============================================================================
-- ADDITIVE + backward-compatible. Objects created / seeded:
--   1. ref_value:            contract_type (5 values)
--                            consent_type  (3 additional: kvkk_consent,
--                                           communication_consent, marketing_consent)
--   2. ref_value_translation: tr + en for all new values
--   3. Permissions:          contract_template:*, student_contract:*,
--                            consent_template:*, student_consent:*,
--                            legal_report:read
--   4. Role grants:          admin (only seeded role)
--   5. Menu items:           legal top-level + 4 sub-items (tr + en)
--
-- Idempotent (ON CONFLICT DO NOTHING / IF NOT EXISTS).
-- Runs as table owner — bypasses RLS.
-- =============================================================================


-- ── Step 1: ref_values ────────────────────────────────────────────────────────

insert into ref.ref_value (ref_type_id, code, sort_order, is_default, is_system)
select ref.type_id(v.type_code), v.code, v.sort_order, v.is_default, false
from (values
  -- contract_type — these values were missing; ref_type already seeded in V2 seed
  ('contract_type', 'enrollment_contract',   1, true),
  ('contract_type', 'package_agreement',     2, false),
  ('contract_type', 'parent_agreement',      3, false),
  ('contract_type', 'consultancy_agreement', 4, false),
  ('contract_type', 'kvkk_disclosure',       5, false),
  -- consent_type — 3 new values (data_processing / camera_viewing / media_release already seeded in V2)
  ('consent_type', 'kvkk_consent',           4, false),
  ('consent_type', 'communication_consent',  5, false),
  ('consent_type', 'marketing_consent',      6, false)
) as v (type_code, code, sort_order, is_default)
where ref.type_id(v.type_code) is not null
on conflict do nothing;


-- ── Step 2: translations (tr + en) ────────────────────────────────────────────

insert into ref.ref_value_translation (ref_value_id, locale, label)
select rv.id, t.locale, t.label
from (values
  -- contract_type
  ('contract_type', 'enrollment_contract',   'tr', 'Kayıt Sözleşmesi'),
  ('contract_type', 'enrollment_contract',   'en', 'Enrollment Contract'),
  ('contract_type', 'package_agreement',     'tr', 'Paket Sözleşmesi'),
  ('contract_type', 'package_agreement',     'en', 'Package Agreement'),
  ('contract_type', 'parent_agreement',      'tr', 'Veli Sözleşmesi'),
  ('contract_type', 'parent_agreement',      'en', 'Parent Agreement'),
  ('contract_type', 'consultancy_agreement', 'tr', 'Danışmanlık Sözleşmesi'),
  ('contract_type', 'consultancy_agreement', 'en', 'Consultancy Agreement'),
  ('contract_type', 'kvkk_disclosure',       'tr', 'KVKK Aydınlatma Metni'),
  ('contract_type', 'kvkk_disclosure',       'en', 'KVKK Disclosure Text'),
  -- consent_type (new)
  ('consent_type', 'kvkk_consent',           'tr', 'KVKK Açık Rıza Beyanı'),
  ('consent_type', 'kvkk_consent',           'en', 'KVKK Explicit Consent'),
  ('consent_type', 'communication_consent',  'tr', 'İletişim İzni'),
  ('consent_type', 'communication_consent',  'en', 'Communication Consent'),
  ('consent_type', 'marketing_consent',      'tr', 'Pazarlama İzni'),
  ('consent_type', 'marketing_consent',      'en', 'Marketing Consent')
) as t (type_code, value_code, locale, label)
join ref.ref_value rv
  on rv.ref_type_id = ref.type_id(t.type_code)
 and rv.code        = t.value_code
 and rv.corporation_id is null
on conflict (ref_value_id, locale) do nothing;


-- ── Step 3: Permission catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action, description) values
  -- Contract Templates
  ('contract_template:read',    'contract_template', 'read',    'View contract templates and version history'),
  ('contract_template:create',  'contract_template', 'create',  'Create a new contract template'),
  ('contract_template:update',  'contract_template', 'update',  'Update the current version of a contract template'),
  ('contract_template:delete',  'contract_template', 'delete',  'Soft-delete a contract template'),
  ('contract_template:version', 'contract_template', 'version', 'Create a new version of a contract template (archives current)'),
  -- Student Contracts
  ('student_contract:read',      'student_contract', 'read',      'View student contract records'),
  ('student_contract:generate',  'student_contract', 'generate',  'Generate a new student contract from a template'),
  ('student_contract:update',    'student_contract', 'update',    'Update draft/sent contract details'),
  ('student_contract:delete',    'student_contract', 'delete',    'Soft-delete a draft contract'),
  ('student_contract:send',      'student_contract', 'send',      'Send a contract to the guardian (draft → sent)'),
  ('student_contract:sign',      'student_contract', 'sign',      'Record guardian signature (sent → signed)'),
  ('student_contract:activate',  'student_contract', 'activate',  'Countersign and activate a contract (signed → active)'),
  ('student_contract:expire',    'student_contract', 'expire',    'Mark an active contract as expired'),
  ('student_contract:terminate', 'student_contract', 'terminate', 'Forcefully terminate a contract'),
  -- Consent Templates
  ('consent_template:read',    'consent_template', 'read',    'View consent templates and version history'),
  ('consent_template:create',  'consent_template', 'create',  'Create a new consent template'),
  ('consent_template:update',  'consent_template', 'update',  'Update the current version of a consent template'),
  ('consent_template:delete',  'consent_template', 'delete',  'Soft-delete a consent template'),
  ('consent_template:version', 'consent_template', 'version', 'Create a new version of a consent template'),
  -- Student Consents
  ('student_consent:read',           'student_consent', 'read',           'View student consent records (KVKK ledger)'),
  ('student_consent:grant',          'student_consent', 'grant',          'Record a consent grant for a student'),
  ('student_consent:withdraw',       'student_consent', 'withdraw',       'Record a consent withdrawal'),
  ('student_consent:attach_evidence','student_consent', 'attach_evidence','Attach a signed consent form as evidence'),
  -- Legal Reports
  ('legal_report:read', 'legal_report', 'read', 'View contract, consent and signature compliance reports')
on conflict (code) do nothing;


-- ── Step 4: Role grants (admin) ───────────────────────────────────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.role r
cross join iam.permission p
where r.code = 'admin'
  and p.code in (
    'contract_template:read', 'contract_template:create', 'contract_template:update',
    'contract_template:delete', 'contract_template:version',
    'student_contract:read', 'student_contract:generate', 'student_contract:update',
    'student_contract:delete', 'student_contract:send', 'student_contract:sign',
    'student_contract:activate', 'student_contract:expire', 'student_contract:terminate',
    'consent_template:read', 'consent_template:create', 'consent_template:update',
    'consent_template:delete', 'consent_template:version',
    'student_consent:read', 'student_consent:grant', 'student_consent:withdraw',
    'student_consent:attach_evidence',
    'legal_report:read'
  )
on conflict do nothing;


-- ── Step 5: Menu items (platform default — corporation_id NULL) ────────────────

insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select
    null, null,
    v.code, v.route, v.icon, v.sort_order, p.id, true
from (values
    ('legal', '/legal', 'file-text', 70, 'student_contract:read')
) as v (code, route, icon, sort_order, perm_code)
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('legal', 'tr', 'Sözleşme ve Rıza Yönetimi'),
    ('legal', 'en', 'Contracts & Consents')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- Sub-items
insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select
    null,
    parent.id,
    v.code, v.route, v.icon, v.sort_order, p.id, true
from (values
    ('legal.contract_templates', '/legal/contract-templates', 'file',          1, 'contract_template:read'),
    ('legal.contracts',          '/legal/contracts',          'file-check',    2, 'student_contract:read'),
    ('legal.consent_templates',  '/legal/consent-templates',  'shield',        3, 'consent_template:read'),
    ('legal.consents',           '/legal/consents',           'user-check',    4, 'student_consent:read')
) as v (code, route, icon, sort_order, perm_code)
join  iam.menu_item parent on parent.code = 'legal' and parent.corporation_id is null
left join iam.permission p  on p.code = v.perm_code
on conflict do nothing;

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('legal.contract_templates', 'tr', 'Sözleşme Şablonları'),
    ('legal.contract_templates', 'en', 'Contract Templates'),
    ('legal.contracts',          'tr', 'Öğrenci Sözleşmeleri'),
    ('legal.contracts',          'en', 'Student Contracts'),
    ('legal.consent_templates',  'tr', 'Rıza Şablonları'),
    ('legal.consent_templates',  'en', 'Consent Templates'),
    ('legal.consents',           'tr', 'Rıza Beyanları'),
    ('legal.consents',           'en', 'Consent Records')
) as t (code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;
