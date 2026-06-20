-- =====================================================================
-- AyNesil Platform :: Layer 2 — Parent Portal (access layer + read views)
-- The portal is mostly an authorization/projection over existing domains.
-- Visibility is governed by: guardian linkage, portal_access flag, parent_visible
-- flags, and approved-status gates. RLS on base tables still applies.
-- =====================================================================

create table students.guardian_portal_access (
  id             uuid primary key default core.uuid_generate_v7(),
  corporation_id uuid not null references core.corporation(id),
  guardian_id    uuid not null references students.guardian(id) on delete cascade,
  student_id     uuid not null references students.student(id) on delete cascade,
  can_view_sessions    boolean not null default true,
  can_view_attendance  boolean not null default true,
  can_view_reports     boolean not null default true,
  can_view_plan        boolean not null default true,
  can_view_finance     boolean not null default true,
  can_view_camera      boolean not null default false,
  granted_at     timestamptz not null default now(),
  revoked_at     timestamptz,
  unique (guardian_id, student_id)
);
comment on table students.guardian_portal_access is 'Per-(guardian,student) portal visibility switches. Camera defaults OFF (consent required).';

-- Students the CURRENT portal user (a guardian) may access.
create or replace view students.v_portal_my_students as
select sga.student_id,
       sga.guardian_id,
       sga.can_view_sessions, sga.can_view_attendance, sga.can_view_reports,
       sga.can_view_plan, sga.can_view_finance, sga.can_view_camera
from students.guardian_portal_access sga
join students.guardian g on g.id = sga.guardian_id
where sga.revoked_at is null
  and g.user_id = core.current_user_id();

-- Sessions visible to the portal user (their accessible students' sessions).
create or replace view scheduling.v_portal_sessions as
select sess.id      as session_id,
       sp.student_id as student_id,
       sess.title,
       sess.starts_at,
       sess.ends_at,
       sess.status
from scheduling.session sess
join scheduling.session_participant sp on sp.session_id = sess.id
where sess.deleted_at is null
  and exists (select 1 from students.v_portal_my_students m
              where m.student_id = sp.student_id and m.can_view_sessions);

-- Package balances visible to the portal user (derived from the credit ledger).
create or replace view finance.v_portal_package_balance as
select pkg.id as student_package_id,
       pkg.student_id,
       pkg.expires_on,
       pkg.total_credits,
       coalesce(sum(l.delta), 0) as remaining_credits,
       pkg.status
from finance.student_package pkg
left join finance.credit_ledger l on l.student_package_id = pkg.id
where exists (select 1 from students.v_portal_my_students m
              where m.student_id = pkg.student_id and m.can_view_finance)
group by pkg.id, pkg.student_id, pkg.expires_on, pkg.total_credits, pkg.status;

-- Approved education plans visible to guardians.
create or replace view education.v_portal_education_plan as
select ep.id, ep.student_id, ep.title, ep.version, ep.status, ep.effective_from, ep.effective_to
from education.education_plan ep
where ep.status = 'approved' and ep.guardian_visible = true
  and exists (select 1 from students.v_portal_my_students m
              where m.student_id = ep.student_id and m.can_view_plan);
