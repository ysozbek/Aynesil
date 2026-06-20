# 07 — Recommended Implementation Order

Sequenced to respect dependencies and to deliver usable slices early. Each phase is independently testable.

## Phase 0 — Foundations
1. `00_extensions_conventions.sql`: extensions, schemas, `uuid_generate_v7`, tenant-context functions, `set_updated_at`, `audit_trigger`.
2. Decide connection roles: a **migration/owner** role (bypasses RLS) and a least-privilege **application** role (subject to RLS).

## Phase 1 — Platform spine (Layer 1)
3. `ref` localization (`locale`, `i18n_message`).
4. `core` tenancy (`corporation`, `campus`).
5. `ref` reference-data engine (`ref_type/value/translation/override` + `type_id`, `value_label`, `v_effective_ref_value`).
6. **Seed** locales + reference catalog (`seed/01`). *Now the business vocabulary exists.*
7. `iam` identity/RBAC/menus.
8. `core` platform services (settings, files, audit/activity, notifications, reporting/KPI, integration).

> Deliverable after Phase 1: a reusable, multi-tenant platform with auth, configurable lists, files, audit, notifications — domain-agnostic.

## Phase 2 — Master data (Layer 2)
9. `students` (student, guardians, clinical/case mgmt, portal access).
10. `educators` (profiles, specialties, certifications, hierarchy).

## Phase 3 — Admissions funnel
11. `crm` (leads, activities, interviews).
12. `assessment` (templates, sessions, scoring, recommendations).
13. `education` programs + enrollment (the conversion target).

## Phase 4 — Service delivery
14. `education` goals + BEP/IEP + academic periods.
15. `scheduling` rooms → sessions → recurrence → attendance → make-up (+ conflict constraints).

## Phase 5 — Commerce & compliance
16. `finance` (packages, credit ledger, invoices, payments, discounts/scholarships).
17. `legal` (contract & consent templates + signed instances).

## Phase 6 — Extended operations
18. `media` (cameras, viewing authorizations + logs) — depends on `legal` consent.
19. `ops` (meetings, leave, performance snapshots).
20. `camps`, `consultancy`.

## Phase 7 — Experience & insight
21. Parent portal access + read views.
22. KPI snapshots / materialized views for dashboards.

## Phase 8 — Cross-cutting hardening (run `99_*` after tables exist; revisit each phase)
23. `99_triggers_rls_policies.sql`: `updated_at` triggers, RLS tenant isolation, audit triggers on sensitive schemas, educator-overlap guard, log-table policies.
24. **Seed** Akran tenant bootstrap (`seed/02`).
25. Partitioning automation (pg_partman or scheduled job) for `audit_log`, `activity_log`, `integration_log`, `viewing_log`.
26. Materialized views + indexes for attendance/utilization/calendar reporting.

## Execution (one-shot, for a fresh database)

```bash
psql -v ON_ERROR_STOP=1 -f db/00_extensions_conventions.sql
for f in db/layer1_core/*.sql; do psql -v ON_ERROR_STOP=1 -f "$f"; done
for f in db/layer2_sped/*.sql; do psql -v ON_ERROR_STOP=1 -f "$f"; done
psql -v ON_ERROR_STOP=1 -f db/99_triggers_rls_policies.sql
psql -v ON_ERROR_STOP=1 -f db/seed/01_reference_data_seed.sql
psql -v ON_ERROR_STOP=1 -f db/seed/02_akran_bootstrap.sql
```

(Run as the owner role; the app then connects as the least-privilege role.)

## Suggested milestones

- **M1 (Phases 0–1):** Platform usable by any tenant; sign-in, configurable lists, audit.
- **M2 (Phases 2–3):** Lead → assessment → enrolled student works end-to-end.
- **M3 (Phases 4–5):** Sessions scheduled, attended, billed against packages.
- **M4 (Phases 6–7):** Parent portal, camera viewing, camps, consultancy, dashboards.
