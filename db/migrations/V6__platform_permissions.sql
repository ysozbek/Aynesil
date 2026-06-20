-- =============================================================================
-- AyNesil Platform :: Flyway V6 — Platform permission catalog
-- Platform-level data — every deployment needs these.
-- Tenant-specific data (corporations, campuses, RBAC assignments) lives in
-- db/seed/tenants/<tenant_code>/ and is applied separately per environment.
-- Idempotent (ON CONFLICT DO NOTHING). Owner rolüyle çalışır.
-- =============================================================================

insert into iam.permission (code, resource, action) values
  -- Corporation
  ('corporation:read',         'corporation', 'read'),
  ('corporation:create',       'corporation', 'create'),
  ('corporation:update',       'corporation', 'update'),
  ('corporation:delete',       'corporation', 'delete'),
  -- Campus
  ('campus:read',              'campus',      'read'),
  ('campus:create',            'campus',      'create'),
  ('campus:update',            'campus',      'update'),
  ('campus:delete',            'campus',      'delete'),
  -- Users
  ('user:read',                'user',        'read'),
  ('user:create',              'user',        'create'),
  ('user:update',              'user',        'update'),
  ('user:delete',              'user',        'delete'),
  ('user:reset_password',      'user',        'reset_password'),
  -- Roles
  ('role:read',                'role',        'read'),
  ('role:create',              'role',        'create'),
  ('role:update',              'role',        'update'),
  ('role:delete',              'role',        'delete'),
  ('role:assign_permission',   'role',        'assign_permission'),
  -- Reference Data
  ('ref_data:read',            'ref_data',    'read'),
  ('ref_data:manage',          'ref_data',    'manage'),
  -- Settings
  ('settings:read',            'settings',    'read'),
  ('settings:manage',          'settings',    'manage'),
  -- Menu
  ('menu:read',                'menu',        'read'),
  ('menu:manage',              'menu',        'manage'),
  -- Notifications
  ('notification:read',        'notification','read'),
  ('notification:send',        'notification','send'),
  -- Files
  ('file:read',                'file',        'read'),
  ('file:upload',              'file',        'upload'),
  ('file:delete',              'file',        'delete'),
  -- Reports
  ('report:read',              'report',      'read'),
  ('report:run',               'report',      'run'),
  ('report:manage',            'report',      'manage'),
  ('report:export',            'report',      'export'),
  -- Audit
  ('audit:read',               'audit',       'read'),
  -- Integrations
  ('integration:read',         'integration', 'read'),
  ('integration:manage',       'integration', 'manage'),
  -- Legacy codes seeded in V2 (ON CONFLICT skips them)
  ('student:read',             'student',     'read'),
  ('student:write',            'student',     'write'),
  ('session:read',             'session',     'read'),
  ('session:write',            'session',     'write'),
  ('finance:read',             'finance',     'read'),
  ('finance:write',            'finance',     'write'),
  ('refdata:manage',           'refdata',     'manage')
on conflict (code) do nothing;
