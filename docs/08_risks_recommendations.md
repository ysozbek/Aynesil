# 08 — Risks & Architectural Recommendations

Ordered roughly by severity / attention required.

## 1. KVKK / GDPR — special-category (health) data  ⚠️ highest
Diagnoses, clinical/medical reports, therapy notes, and camera footage are **special-category personal data**. Akran operates in Türkiye → **KVKK** (plus GDPR-equivalent expectations).

**Recommendations**
- **Lawful basis & consent**: `legal.student_consent` is the consent ledger; gate processing/feature access on an *active* consent (e.g. camera viewing requires a `camera_viewing` consent).
- **Field-level encryption** for the most sensitive fields (`national_id`, raw diagnosis text) — application-side envelope encryption or `pgcrypto`; store only key references.
- **Right to erasure**: combine soft delete (`deleted_at`) with **crypto-shredding** (drop the row's data key) and **anonymization** of retained analytical rows. Define per-entity **retention policies**.
- **Mandatory access logging** on clinical and media reads via `core.activity_log` and `media.viewing_log`.
- **Data minimization** in the parent portal (only `parent_visible` / approved content).

## 2. Reference-data over-generalization (controlled EAV)
Risk: the configurable engine becomes a dumping ground, eroding type safety and query performance.

**Mitigations** (see doc 06): composite-FK category pinning on safety-critical columns, `value_schema` validation of `metadata`, and a firm *vocabulary-vs-structure* rule. Review new `ref_type` additions in design review.

## 3. Scheduling concurrency & double-booking
Two simultaneous bookings can both pass an application check.

**Mitigations**
- Room overlaps: `EXCLUDE USING gist` on `scheduling.session` (DB-enforced, race-proof).
- Educator overlaps: trigger `scheduling.check_educator_overlap` (multi-educator sessions can't use a single EXCLUDE). For full race-safety under high concurrency, wrap bulk generation in `SERIALIZABLE` or take per-resource advisory locks.
- Recurrence: store **rules** (`recurring_schedule`) + **exceptions**, and *materialize* concrete `session` rows via an idempotent generator (don't compute on the fly).

## 4. High-volume tables & growth
`session`, `attendance`, `audit_log`, `activity_log`, `notification_delivery`, `viewing_log`, `credit_ledger` grow without bound.

**Recommendations**
- **Range partition by time** the append-only logs (already partitioned; automate monthly partitions with **pg_partman** and set **retention**). Note: a `DEFAULT` partition is provided for bootstrap — switch to explicit monthly partitions before high volume (you can't add an overlapping partition while rows sit in DEFAULT).
- **Materialized views** for attendance %, utilization, calendar projections; refresh on schedule.
- Index the hot access paths: `(campus_id, starts_at)`, `(room_id, starts_at)`, `(educator_id, …)`.

## 5. Multi-tenant isolation correctness
A missing `corporation_id` filter is a cross-tenant leak.

**Recommendations**
- **RLS default-deny** is enabled on every tenant table (no tenant context ⇒ no rows). The app **must** connect as a non-owner role (the owner bypasses RLS).
- Consider **composite FKs that include `corporation_id`** on the highest-risk parent/child pairs to make cross-tenant references structurally impossible (a hardening step beyond the per-row RLS).
- Add an automated test: with `app.current_corporation_id` unset, every tenant table returns zero rows.

## 6. Financial integrity
Money bugs are unacceptable.

**Recommendations**
- Balances are **derived from the append-only `credit_ledger`**, never stored mutably as source of truth (reconcile/cache only).
- `numeric` everywhere (no floats); currency-aware (`currency` column).
- **Idempotency keys** on `payment` for gateway callbacks; use the **outbox** for exactly-once external effects.

## 7. Soft delete vs. uniqueness & FKs
Soft-deleted rows can collide with new ones and linger behind FKs.

**Recommendations**
- Use **partial unique indexes** `WHERE deleted_at IS NULL` for natural keys.
- Decide cascade vs. restrict per relationship deliberately; prefer restrict for clinical/financial history.

## 8. Identity, SSO & secrets
**Recommendations**
- External IdP is modeled from day 1 (`iam.identity_provider` / `user_identity`) — avoid a painful retrofit.
- **Never store raw secrets**: `integration_connection.secret_ref` / `webhook_endpoint.secret_ref` point to a secret manager.
- Treat `password_hash` as optional (SSO-only users).

## 9. Auditability guarantees
**Recommendation**: prefer **DB-trigger audit** (`core.audit_trigger`, already attached to clinical/financial/legal/scheduling) over app-only logging, so coverage can't be bypassed. Keep `audit_log` immutable (no UPDATE/DELETE grants to app roles).

## 10. Streaming / camera vendor lock-in
**Recommendation**: keep camera streams **provider-agnostic** (`camera.stream_provider_id` → `integration_connection`, `stream_ref` opaque). Live viewing must always check (a) active authorization, (b) backing consent, (c) write a `viewing_log` row.

## 11. PK / enumeration leakage
**Recommendation**: UUID v7 chosen — globally unique, time-ordered, no sequential enumeration leakage in portal/API. Don't expose internal `bigint` log ids externally.

## 12. Calendar performance & correctness
**Recommendation**: model calendars as **read projections/views** over `session`, `ops.meeting`, `ops.leave_request`, `camps`, and `scheduling.calendar_entry` (holidays), with hourly granularity from `timestamptz` ranges — not a duplicated event store.

## 13. Operational maturity (recommended next steps)
- Migrations via **Flyway/Liquibase** (plain SQL) once schema stabilizes; keep the numbered files as the baseline.
- Backups + PITR; restore drills.
- `pg_stat_statements` + index review after first load tests.
- A staging tenant that mirrors Akran for safe schema evolution.

---

### Risk register (summary)

| # | Risk | Likelihood | Impact | Primary mitigation |
|---|---|---|---|---|
| 1 | Health-data privacy (KVKK) | High | Severe | Consent gating, encryption, retention, access logs |
| 2 | EAV over-generalization | Medium | High | Composite-FK pinning, schema validation, governance |
| 3 | Double-booking | High | Medium | EXCLUDE constraints + overlap trigger + serializable |
| 4 | Table growth | High | Medium | Partitioning + retention + matviews |
| 5 | Tenant leakage | Medium | Severe | RLS default-deny + composite tenant FKs + tests |
| 6 | Financial errors | Medium | Severe | Ledger + numeric + idempotency |
| 7 | Soft-delete collisions | Medium | Low | Partial unique indexes |
| 8 | Secret leakage | Low | Severe | Secret references only |
