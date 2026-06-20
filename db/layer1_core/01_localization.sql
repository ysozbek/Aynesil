-- =====================================================================
-- AyNesil Platform :: Layer 1 — Localization
-- =====================================================================

create table ref.locale (
  code         text primary key,                       -- BCP-47: 'tr', 'en', 'en-US'
  english_name text not null,
  native_name  text not null,
  direction    text not null default 'ltr' check (direction in ('ltr','rtl')),
  is_active    boolean not null default true,
  sort_order   integer not null default 0,
  created_at   timestamptz not null default now()
);
comment on table ref.locale is 'Supported platform locales (system reference data).';

-- Static UI / system string translations (admin-managed key/value catalog).
create table ref.i18n_message (
  id         uuid primary key default core.uuid_generate_v7(),
  namespace  text not null,            -- 'ui.menu', 'validation', 'email.subject'...
  msg_key    text not null,
  locale     text not null references ref.locale(code),
  value      text not null,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  row_version integer not null default 1,
  unique (namespace, msg_key, locale)
);
comment on table ref.i18n_message is 'Static UI/system string translations. Entity content is translated via per-entity *_translation tables.';

-- Localized label resolver for reference values (fallback chain handled in 03).
-- (Function created in 03_reference_data.sql after ref_value/translation exist.)
