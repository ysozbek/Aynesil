-- =====================================================================
-- AyNesil Platform :: Layer 2 — Finance (Packages, Credits, Payments, Invoicing)
-- Reference (ref_value) categories: package_type, payment_method, discount_type,
--   scholarship_type
-- Money: numeric(14,2) + currency; balances derived from an append-only ledger.
-- =====================================================================

create table finance.package_definition (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  code           text not null,
  name           text not null,
  package_type_id uuid references ref.ref_value(id),            -- ref_type 'package_type' (session_package/program_package)
  program_id     uuid references education.program(id),
  total_credits  numeric(10,2),                                 -- e.g. number of sessions
  validity_days  integer,                                       -- expiration window after purchase
  list_price     numeric(14,2) not null default 0,
  currency       char(3) not null default 'TRY',
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, code)
);

create table finance.student_package (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid not null references students.student(id) on delete cascade,
  package_definition_id uuid references finance.package_definition(id),
  purchased_on   date not null default current_date,
  expires_on     date,
  total_credits  numeric(10,2) not null default 0,
  price          numeric(14,2) not null default 0,
  currency       char(3) not null default 'TRY',
  status         text not null default 'active' check (status in ('active','exhausted','expired','cancelled')),
  created_at  timestamptz not null default now(),
  created_by  uuid,
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1
);
comment on table finance.student_package is 'Purchased package instance. Remaining credits = SUM(finance.credit_ledger.delta).';

-- Append-only ledger: every credit grant/consumption/adjustment is one immutable row.
create table finance.credit_ledger (
  id                 uuid primary key default core.uuid_generate_v7(),
  corporation_id     uuid not null references core.corporation(id),
  student_package_id uuid not null references finance.student_package(id) on delete cascade,
  entry_type         text not null check (entry_type in ('grant','consume','refund','adjustment','expire')),
  delta              numeric(10,2) not null,                    -- + grant / - consume
  session_id         uuid references scheduling.session(id),    -- when consumed by a session
  reason             text,
  occurred_at        timestamptz not null default now(),
  created_by         uuid
);
create index ix_credit_ledger_pkg on finance.credit_ledger(student_package_id, occurred_at);

create table finance.invoice (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  student_id     uuid references students.student(id),
  guardian_id    uuid references students.guardian(id),
  invoice_no     text,
  issue_date     date not null default current_date,
  due_date       date,
  currency       char(3) not null default 'TRY',
  subtotal       numeric(14,2) not null default 0,
  discount_total numeric(14,2) not null default 0,
  tax_total      numeric(14,2) not null default 0,
  grand_total    numeric(14,2) not null default 0,
  status         text not null default 'draft' check (status in ('draft','issued','paid','partial','void','overdue')),
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, invoice_no)
);

create table finance.invoice_line (
  id          uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  invoice_id  uuid not null references finance.invoice(id) on delete cascade,
  description text not null,
  student_package_id uuid references finance.student_package(id),
  quantity    numeric(10,2) not null default 1,
  unit_price  numeric(14,2) not null default 0,
  line_total  numeric(14,2) not null default 0,
  sort_order  integer not null default 0
);

create table finance.payment (
  id              uuid primary key default core.uuid_generate_v7(),
  corporation_id  uuid not null references core.corporation(id),
  invoice_id      uuid references finance.invoice(id),
  student_id      uuid references students.student(id),
  payment_method_id uuid references ref.ref_value(id),          -- ref_type 'payment_method'
  amount          numeric(14,2) not null,
  currency        char(3) not null default 'TRY',
  status          text not null default 'captured' check (status in ('pending','authorized','captured','failed','refunded')),
  gateway_provider_id uuid references core.integration_connection(id),
  gateway_reference text,
  idempotency_key text,                                         -- gateway callback dedupe
  paid_at         timestamptz,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1,
  unique nulls not distinct (corporation_id, idempotency_key)
);

create table finance.refund (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  payment_id     uuid not null references finance.payment(id),
  amount         numeric(14,2) not null,
  reason         text,
  status         text not null default 'pending' check (status in ('pending','processed','failed')),
  processed_at   timestamptz,
  created_at     timestamptz not null default now()
);

create table finance.discount (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  invoice_id     uuid references finance.invoice(id),
  student_package_id uuid references finance.student_package(id),
  discount_type_id uuid references ref.ref_value(id),           -- ref_type 'discount_type'
  is_percentage  boolean not null default true,
  value          numeric(14,2) not null,
  reason         text,
  created_at     timestamptz not null default now()
);

create table finance.scholarship (
  id               uuid primary key default core.uuid_generate_v7(),
  corporation_id   uuid not null references core.corporation(id),
  student_id       uuid not null references students.student(id),
  scholarship_type_id uuid references ref.ref_value(id),        -- ref_type 'scholarship_type'
  percentage       numeric(5,2),
  amount           numeric(14,2),
  valid_from       date,
  valid_to         date,
  approved_by      uuid,
  note             text,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  row_version integer not null default 1
);

create table finance.promotion (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  code           text not null,
  name           text not null,
  is_percentage  boolean not null default true,
  value          numeric(14,2) not null,
  valid_from     date,
  valid_to       date,
  max_redemptions integer,
  is_active      boolean not null default true,
  unique (corporation_id, code)
);
