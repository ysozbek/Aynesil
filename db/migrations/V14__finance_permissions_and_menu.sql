-- =============================================================================
-- AyNesil Platform :: Flyway V14 — Finance Permissions + Sub-menu
-- =============================================================================
-- Adds granular permission codes for the Finance / Payment module:
--   Package Definitions, Student Packages, Credit Ledger, Invoices,
--   Payments, Refunds, Discounts, Scholarships, Promotions, Finance Reports.
--
-- The coarse permissions finance:read / finance:write introduced in V6 are kept
-- as-is for backward compatibility. New granular codes are additive.
--
-- Seeds sub-navigation menu items under the existing 'payments' top-level item
-- (created by V8) with tr/en translations.
-- Grants all new permissions to the default admin role.
--
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır — RLS bypass.
-- =============================================================================

-- ── Step 1: Permission Catalog ────────────────────────────────────────────────

insert into iam.permission (code, resource, action) values
  -- Package Definitions
  ('package_definition:read',   'package_definition', 'read'),
  ('package_definition:create', 'package_definition', 'create'),
  ('package_definition:update', 'package_definition', 'update'),
  ('package_definition:delete', 'package_definition', 'delete'),
  -- Student Packages
  ('student_package:read',     'student_package', 'read'),
  ('student_package:purchase', 'student_package', 'purchase'),
  ('student_package:cancel',   'student_package', 'cancel'),
  -- Credit Ledger
  ('credit_ledger:read',    'credit_ledger', 'read'),
  ('credit_ledger:consume', 'credit_ledger', 'consume'),
  ('credit_ledger:grant',   'credit_ledger', 'grant'),
  ('credit_ledger:adjust',  'credit_ledger', 'adjust'),
  -- Invoices
  ('invoice:read',   'invoice', 'read'),
  ('invoice:create', 'invoice', 'create'),
  ('invoice:update', 'invoice', 'update'),
  ('invoice:void',   'invoice', 'void'),
  -- Finance Payments (transactions)
  ('finance_payment:read',    'finance_payment', 'read'),
  ('finance_payment:record',  'finance_payment', 'record'),
  ('finance_payment:capture', 'finance_payment', 'capture'),
  -- Refunds
  ('refund:read',    'refund', 'read'),
  ('refund:request', 'refund', 'request'),
  ('refund:process', 'refund', 'process'),
  -- Discounts
  ('discount:read',  'discount', 'read'),
  ('discount:apply', 'discount', 'apply'),
  -- Scholarships
  ('scholarship:read',   'scholarship', 'read'),
  ('scholarship:grant',  'scholarship', 'grant'),
  ('scholarship:update', 'scholarship', 'update'),
  -- Promotions
  ('promotion:read',   'promotion', 'read'),
  ('promotion:create', 'promotion', 'create'),
  ('promotion:update', 'promotion', 'update'),
  -- Finance Reports
  ('finance_report:read', 'finance_report', 'read')
on conflict (code) do nothing;

-- ── Step 2: Finance Sub-menu Items ────────────────────────────────────────────
-- Parent: 'payments' (inserted by V8, sort_order 80, perm: finance:read)

insert into iam.menu_item
    (corporation_id, parent_id, code, route, icon, sort_order, required_permission_id, is_active)
select
    null,
    parent.id,
    v.code,
    v.route,
    v.icon,
    v.sort_order,
    p.id,
    true
from (values
    ('payments-packages',     '/payments/packages',             'shopping-bag',  10, 'student_package:read'),
    ('payments-definitions',  '/payments/package-definitions',  'tag',           20, 'package_definition:read'),
    ('payments-invoices',     '/payments/invoices',             'receipt',       30, 'invoice:read'),
    ('payments-transactions', '/payments/transactions',         'credit-card',   40, 'finance_payment:read'),
    ('payments-refunds',      '/payments/refunds',              'arrow-uturn-left', 50, 'refund:read'),
    ('payments-credits',      '/payments/credits',              'ticket',        60, 'credit_ledger:read'),
    ('payments-scholarships', '/payments/scholarships',         'academic-cap',  70, 'scholarship:read'),
    ('payments-promotions',   '/payments/promotions',           'gift',          80, 'promotion:read'),
    ('payments-reports',      '/payments/reports',              'chart-bar',     90, 'finance_report:read')
) as v(code, route, icon, sort_order, perm_code)
cross join (
    select id from iam.menu_item where code = 'payments' and corporation_id is null
) as parent
left join iam.permission p on p.code = v.perm_code
on conflict do nothing;

-- ── Step 3: Translations (tr + en) ───────────────────────────────────────────

insert into iam.menu_item_translation (menu_item_id, locale, label)
select m.id, t.locale, t.label
from iam.menu_item m
join (values
    ('payments-packages',     'tr', 'Öğrenci Paketleri'),
    ('payments-packages',     'en', 'Student Packages'),
    ('payments-definitions',  'tr', 'Paket Kataloğu'),
    ('payments-definitions',  'en', 'Package Catalog'),
    ('payments-invoices',     'tr', 'Faturalar'),
    ('payments-invoices',     'en', 'Invoices'),
    ('payments-transactions', 'tr', 'Tahsilatlar'),
    ('payments-transactions', 'en', 'Payments'),
    ('payments-refunds',      'tr', 'İadeler'),
    ('payments-refunds',      'en', 'Refunds'),
    ('payments-credits',      'tr', 'Kredi Defteri'),
    ('payments-credits',      'en', 'Credit Ledger'),
    ('payments-scholarships', 'tr', 'Burslar'),
    ('payments-scholarships', 'en', 'Scholarships'),
    ('payments-promotions',   'tr', 'Kampanyalar'),
    ('payments-promotions',   'en', 'Promotions'),
    ('payments-reports',      'tr', 'Finans Raporları'),
    ('payments-reports',      'en', 'Finance Reports')
) as t(code, locale, label) on t.code = m.code and m.corporation_id is null
on conflict (menu_item_id, locale) do nothing;

-- ── Step 4: Grant all new permissions to default admin role ───────────────────

insert into iam.role_permission (role_id, permission_id)
select r.id, p.id
from iam.permission p
cross join iam.role r
where p.code in (
    'package_definition:read', 'package_definition:create',
    'package_definition:update', 'package_definition:delete',
    'student_package:read', 'student_package:purchase', 'student_package:cancel',
    'credit_ledger:read', 'credit_ledger:consume',
    'credit_ledger:grant', 'credit_ledger:adjust',
    'invoice:read', 'invoice:create', 'invoice:update', 'invoice:void',
    'finance_payment:read', 'finance_payment:record', 'finance_payment:capture',
    'refund:read', 'refund:request', 'refund:process',
    'discount:read', 'discount:apply',
    'scholarship:read', 'scholarship:grant', 'scholarship:update',
    'promotion:read', 'promotion:create', 'promotion:update',
    'finance_report:read'
)
  and r.name = 'admin'
on conflict do nothing;
