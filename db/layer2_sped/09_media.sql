-- =====================================================================
-- AyNesil Platform :: Layer 2 — Cameras & Live Viewing
-- Parent live-viewing is gated by KVKK consent (legal.student_consent) and
-- time-boxed authorizations. All viewing is access-logged.
-- =====================================================================

create table media.camera (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  campus_id      uuid references core.campus(id),
  code           text not null,
  name           text not null,
  stream_provider_id uuid references core.integration_connection(id),  -- streaming provider (vendor-agnostic)
  stream_ref     text,                                          -- provider-specific stream id (no raw secrets)
  is_active      boolean not null default true,
  created_at  timestamptz not null default now(),
  updated_at  timestamptz not null default now(),
  deleted_at  timestamptz,
  row_version integer not null default 1,
  unique (corporation_id, code)
);

create table media.room_camera (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  room_id        uuid not null references scheduling.room(id) on delete cascade,
  camera_id      uuid not null references media.camera(id) on delete cascade,
  unique (room_id, camera_id)
);

create table media.session_camera (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  session_id     uuid not null references scheduling.session(id) on delete cascade,
  camera_id      uuid not null references media.camera(id),
  unique (session_id, camera_id)
);

-- Time-boxed parent authorization to view a session/student feed.
create table media.viewing_authorization (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  guardian_id    uuid not null references students.guardian(id) on delete cascade,
  student_id     uuid not null references students.student(id),
  session_id     uuid references scheduling.session(id),        -- NULL => standing authorization for the student
  consent_id     uuid references legal.student_consent(id),     -- the camera_viewing consent backing this grant
  valid_from     timestamptz not null default now(),
  valid_to       timestamptz,
  granted_by     uuid,
  is_revoked     boolean not null default false,
  created_at     timestamptz not null default now()
);
comment on table media.viewing_authorization is 'Authorizes a guardian to view a feed; should reference an active camera_viewing consent.';
create index ix_viewing_auth_guardian on media.viewing_authorization(guardian_id) where is_revoked = false;

-- Immutable access log of who watched what, when (privacy/audit).
create table media.viewing_log (
  id             bigint generated always as identity,
  corporation_id uuid not null,
  guardian_id    uuid,
  user_id        uuid,
  session_id     uuid,
  camera_id      uuid,
  authorization_id uuid,
  started_at     timestamptz not null default now(),
  ended_at       timestamptz,
  ip_address     inet,
  primary key (id, started_at)
) partition by range (started_at);
create table media.viewing_log_default partition of media.viewing_log default;
