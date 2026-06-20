-- =====================================================================
-- AyNesil Platform :: Layer 1 — Identity, Authentication, RBAC, Menus
-- =====================================================================

-- ---------------------------------------------------------------------
-- Users & external identity providers (SSO/OIDC/SAML modeled from day 1)
-- ---------------------------------------------------------------------
create table iam.user_account (
  id                uuid primary key default core.uuid_generate_v7(),
  corporation_id    uuid not null references core.corporation(id),
  username          citext not null,
  email             citext,
  phone             text,
  full_name         text not null,
  password_hash     text,                                   -- NULL when authenticated only via external IdP
  status            text not null default 'active' check (status in ('invited','active','suspended','disabled')),
  preferred_locale  text references ref.locale(code),
  primary_campus_id uuid references core.campus(id),
  mfa_enabled       boolean not null default false,
  mfa_secret        text,
  last_login_at     timestamptz,
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  updated_by  uuid,
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, username)
);
comment on table iam.user_account is 'Authentication identity. Educators and guardians link to a user_account for portal/app access.';
create unique index uq_user_email on iam.user_account(corporation_id, email) where email is not null and deleted_at is null;

create table iam.identity_provider (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid references core.corporation(id),       -- NULL = platform-wide provider
  code           text not null,
  kind           text not null check (kind in ('oidc','saml','oauth2','ldap','local')),
  display_name   text not null,
  config         jsonb not null default '{}'::jsonb,         -- endpoints, client id, secret REFERENCE (not raw secret)
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, code)
);

create table iam.user_identity (
  id               uuid primary key default core.uuid_generate_v7(),
  user_id          uuid not null references iam.user_account(id) on delete cascade,
  provider_id      uuid not null references iam.identity_provider(id),
  external_subject text not null,
  created_at       timestamptz not null default now(),
  unique (provider_id, external_subject)
);

create table iam.auth_session (
  id                 uuid primary key default core.uuid_generate_v7(),
  corporation_id     uuid not null references core.corporation(id),
  user_id            uuid not null references iam.user_account(id) on delete cascade,
  issued_at          timestamptz not null default now(),
  expires_at         timestamptz not null,
  revoked_at         timestamptz,
  refresh_token_hash text,
  ip_address         inet,
  user_agent         text
);
create index ix_auth_session_user on iam.auth_session(user_id) where revoked_at is null;

-- ---------------------------------------------------------------------
-- RBAC: roles, permissions, assignments (campus-scopable)
-- ---------------------------------------------------------------------
create table iam.permission (
  id          uuid primary key default core.uuid_generate_v7(),
  code        text not null unique,           -- 'student:read', 'session:create'
  resource    text not null,
  action      text not null,
  description text
);
comment on table iam.permission is 'Platform catalog of resource:action permissions (global, not tenant-scoped).';

create table iam.role (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid references core.corporation(id),       -- NULL = system role template (cloned per tenant)
  code           text not null,
  name           text not null,
  description    text,
  is_system      boolean not null default false,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, code)
);

create table iam.role_permission (
  role_id       uuid not null references iam.role(id) on delete cascade,
  permission_id uuid not null references iam.permission(id) on delete cascade,
  primary key (role_id, permission_id)
);

create table iam.user_role (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  user_id        uuid not null references iam.user_account(id) on delete cascade,
  role_id        uuid not null references iam.role(id),
  campus_id      uuid references core.campus(id),             -- NULL = corporation-wide; set = campus-scoped grant
  valid_from     timestamptz,
  valid_to       timestamptz,
  created_at     timestamptz not null default now(),
  created_by     uuid,
  unique nulls not distinct (user_id, role_id, campus_id)
);
comment on table iam.user_role is 'Role grant, optionally scoped to a single campus (scoped authorization).';

-- ---------------------------------------------------------------------
-- Dynamic, permission-driven menus
-- ---------------------------------------------------------------------
create table iam.menu_item (
  id                     uuid primary key default core.uuid_generate_v7(),
  corporation_id         uuid references core.corporation(id),     -- NULL = platform default menu
  parent_id              uuid references iam.menu_item(id),
  code                   text not null,
  route                  text,
  icon                   text,
  sort_order             integer not null default 0,
  required_permission_id uuid references iam.permission(id),       -- visibility driven by permission
  feature_flag           text,
  is_active              boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, code)
);

create table iam.menu_item_translation (
  menu_item_id uuid not null references iam.menu_item(id) on delete cascade,
  locale       text not null references ref.locale(code),
  label        text not null,
  primary key (menu_item_id, locale)
);
