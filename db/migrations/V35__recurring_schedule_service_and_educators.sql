-- =============================================================================
-- V35__recurring_schedule_service_and_educators.sql
--
-- Adds the two pieces a recurring schedule needs to materialise complete
-- sessions: the program service delivered and the assigned educators.
--   1. scheduling.recurring_schedule.program_service_id (nullable)
--   2. scheduling.recurring_schedule_educator (mirrors scheduling.session_educator)
--   3. RLS tenant_isolation
--   4. trg_audit (the baseline audit loop in 99_triggers_rls_policies.sql only
--      covers tables that existed when it ran)
--
-- Grants: handled by afterMigrate__app_role_grants.sql.
-- Idempotent: safe to run twice.
-- =============================================================================


-- ── Step 1: program service on the rule ──────────────────────────────────────

alter table scheduling.recurring_schedule
  add column if not exists program_service_id uuid
    references education.program_service(id);

comment on column scheduling.recurring_schedule.program_service_id is
  'Service delivered by sessions generated from this rule; copied to session.program_service_id.';


-- ── Step 2: educators on the rule ────────────────────────────────────────────

create table if not exists scheduling.recurring_schedule_educator (
  id                    uuid primary key default core.uuid_generate_v7(),
  corporation_id        uuid not null references core.corporation(id),
  recurring_schedule_id uuid not null references scheduling.recurring_schedule(id) on delete cascade,
  educator_id           uuid not null references educators.educator(id),
  role                  text not null default 'lead'
                          check (role in ('lead','assistant','observer','supervisor')),
  unique (recurring_schedule_id, educator_id)
);

comment on table scheduling.recurring_schedule_educator is
  'Educators assigned to a recurring schedule; copied to session_educator when sessions are generated.';

create index if not exists ix_recurring_schedule_educator_educator
  on scheduling.recurring_schedule_educator(educator_id);


-- ── Step 3: RLS ──────────────────────────────────────────────────────────────

alter table scheduling.recurring_schedule_educator enable row level security;

drop policy if exists tenant_isolation on scheduling.recurring_schedule_educator;
create policy tenant_isolation on scheduling.recurring_schedule_educator
  using  (corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());


-- ── Step 4: audit ────────────────────────────────────────────────────────────

create or replace trigger trg_audit
  after insert or update or delete on scheduling.recurring_schedule_educator
  for each row execute function core.audit_trigger();
