# ABAC_CARE_TEAM_DESIGN.md — Care-Team Attribute-Based Authorization

Status: **Design proposal — not implemented.** No `db/` changes, migrations, or code are produced by this document. It refines the deferred decision recorded in `ARCHITECTURE.md` §"Authorization Strategy".

---

## 1. Executive Summary

The platform today enforces two orthogonal authorization layers:

1. **Tenant isolation** — PostgreSQL RLS keyed on `corporation_id` via the `app.current_corporation_id` GUC (`core.current_corporation_id()`), default-deny, applied automatically to every table carrying `corporation_id` (`db/99_triggers_rls_policies.sql` §2).
2. **RBAC** — global permission catalog (`iam.permission`), tenant roles (`iam.role`), grants (`iam.user_role`, optionally campus-scoped), enforced in the API via `HasPermissionAttribute` + `perm` JWT claims.

RBAC answers *"may this user read case notes at all?"* It cannot answer *"…but only for the students on their care team."* That second question is **per-row, relationship-driven**, i.e. ABAC.

**Recommendation:** introduce a **Care-Team ABAC layer** as a purely **additive** change, modeled directly on the existing Parent-Portal precedent (`students.guardian_portal_access` + `core.current_user_id()`-resolved views — `db/layer2_sped/13_parent_portal.sql`), which is already an ABAC overlay on top of RBAC + RLS.

The recommended shape is:

- **A dedicated assignment table** `students.student_care_assignment` (M:N student↔educator, effective-dated, role as configurable reference data) — *not* a reuse of `session_educator`/`student_program`.
- **Combined enforcement**: a **PostgreSQL RESTRICTIVE RLS policy** as the non-bypassable security backstop on clinical tables, plus **application-layer filtering** for UX and clear 403s.
- **Bypass as a permission** (`care_team:bypass`), surfaced to the database through a new GUC `app.care_team_bypass`, set by the same interceptor that already sets the tenant/user GUCs.
- **Scope limited to clinical/educational records**; operational, financial, and CRM modules remain RBAC-only.

The design is backward compatible and additive (new tables, indexes, RLS policies, permissions, reference values, menu) with **one unavoidable behavioral change**: clinical RLS becomes stricter, so privileged roles must be granted the bypass *before* the restrictive policies go live. This is handled by phasing (§8).

---

## 2. Care-Team Relationship Model

### 2.1 What exists today (and why none of it fits)

| Existing structure | What it models | Why it is not a care-team assignment |
|---|---|---|
| `scheduling.session_educator` | Which educators staff a *single session* (`role` enum: lead/assistant/observer/supervisor) | Ephemeral, per-occurrence. A therapist assigned to a student but on leave this week would lose access. Deriving durable assignment from session history is fragile and explodes in cost. Role is a hardcoded enum (violates the configurable-reference-data rule for clinical roles). |
| `education.student_program` | Student↔program enrollment | Program-level, not person-level. No educator dimension. |
| `educators.educator_specialty` | Educator's skills | No student linkage at all. |
| `educators.educator_hierarchy` | Supervisor graph between educators | Educator↔educator, not educator↔student. Useful as a *supervisory bypass input*, not the assignment itself. |
| `education.education_plan.prepared_by/approved_by`, `assessment.assessment_session.assessor_id` | Authorship on individual artifacts | Single-actor, artifact-scoped; cannot express a standing multi-member team. |

**Conclusion: Option B — a dedicated care-team assignment model.**

### 2.2 Recommended model — `students.student_care_assignment`

A single edge table expressing a durable, effective-dated, role-typed link between a student and an educator.

- **Cardinality:** M:N. A student has many care-team members; an educator serves many students. Multiple simultaneous assignments are first-class.
- **Role:** configurable reference data — `ref_type = 'care_team_role'` (values: primary therapist, secondary therapist, coordinator, psychologist, consultant, observer, supervisor, …). Per the project's reference-data rule, these are **never enums**; tenants can extend them.
- **Primary designation:** `is_primary boolean`, with a *partial unique index* enforcing at most one active primary per student (or per (student, role) if a per-discipline primary is desired — recommend per-student initially).
- **Effective dating:** `active_from` / `active_to` (nullable = open-ended). "Active" is derived: `active_from <= now AND (active_to IS NULL OR active_to > now)` AND `status='active'` AND `deleted_at IS NULL`. This naturally handles temporary, time-bounded, and historical assignments without separate tables.
- **Active/inactive:** `status` (active/suspended/ended) plus soft delete (`deleted_at`). Assignments are **not financial records**, so soft delete is appropriate; however, history should be preserved (do not hard-delete) so the audit trail of "who could see what, when" survives.
- **Campus awareness:** `campus_id` (nullable) to align with the platform's campus-as-sub-scope model.
- **Identity bridge:** access is evaluated against the *logged-in user*. The bridge is `educators.educator.user_id → iam.user_account.id = core.current_user_id()`. This requires assigned clinicians to have a populated `educator.user_id` (a data-backfill concern, §5).

### 2.3 Option A vs Option B — tradeoffs

| | A) Reuse existing structures | B) Dedicated assignment table (recommended) |
|---|---|---|
| Schema change | None | One additive table + indexes |
| Semantic fit | Poor — conflates scheduling/enrollment with authorization | Exact — one purpose, one source of truth |
| Effective dating | Absent or implicit | Explicit, first-class |
| Role configurability | `session_educator.role` is an enum | Reference data (rule-compliant) |
| Query/RLS cost | Heavy (must aggregate session history) | Single indexed EXISTS lookup |
| Future extensibility (§7) | Blocked | Built in |
| Risk to approved modules | Higher (overloads scheduling semantics) | Isolated, additive |

Option B wins on every axis except "zero new tables", which is outweighed by correctness, performance, and the explicit rule that clinical role classifications must be reference data.

---

## 3. Scope of Enforcement

Guiding principle (KVKK / doc `08_risks_recommendations.md` §1 — *minimum necessary*): protect **special-category clinical/educational data** with ABAC; leave **operational, financial, and pre-enrollment** data on RBAC, where coordination requires broad visibility.

### 3.1 Clinical / educational — RBAC + ABAC

| Data domain | Table(s) | Decision | Rationale |
|---|---|---|---|
| Diagnoses | `students.diagnosis` | RBAC + ABAC | Health special-category; highest sensitivity. |
| Medical reports | `students.medical_report` | RBAC + ABAC | Special-category. |
| Development reports | `students.development_report` | RBAC + ABAC | Clinical narrative. |
| External institution reports | `students.external_institution_report` | RBAC + ABAC | Clinical. |
| Developmental profile | `students.developmental_profile` | RBAC + ABAC | Clinical. |
| Case notes | `students.case_note` | RBAC + ABAC (+ existing `case_note:read_confidential` sub-gate) | Most sensitive narrative. |
| Assessments | `assessment.assessment_session` / `_response` / `_report` | RBAC + ABAC **only when `student_id` is set** | Pre-enrollment lead assessments have no care team (see CRM note). |
| Goals & progress | `education.student_goal`, `education.goal_progress` | RBAC + ABAC | Individualized clinical targets. |
| Education plans (BEP/IEP) | `education.education_plan` + `_goal/_review/_approval/_revision` | RBAC + ABAC | Individualized; note guardians have a separate approved-only view. |
| Session notes | `scheduling.session_note` | RBAC + ABAC (via session→participant→student) | Clinical narrative tied to a student. |
| Clinical attachments | `core.file_object` (`is_sensitive=true`) / `core.file_attachment` | RBAC + ABAC at the *owner* layer; files via owner resolution | Polymorphic; see §9 risk — gate at the owning clinical record initially. |

**Student profile (`students.student`) — split treatment.** Full ABAC on the master record would break scheduling/finance/reception screens that legitimately need a name. Recommended: keep the *master row* RBAC-only (directory-level: name, status, campus), and apply ABAC to the *clinical satellites* above. If a "clinical profile view" composite is later built, gate that **read model**, not the base row. Document this explicitly so no one assumes `students.student` is care-team-gated.

### 3.2 Non-clinical — RBAC-only (no ABAC)

| Module | Decision | Rationale |
|---|---|---|
| Scheduling (sessions, rooms, attendance, make-up) | RBAC-only | Operational; coordinators/reception need cross-student visibility. `session_educator` already records staffing. (Session **notes** are the exception — clinical.) |
| Attendance | RBAC-only | Operational logistics. |
| CRM (leads, interviews, lead assessments) | RBAC-only | Pre-enrollment; no enrolled student / care team exists yet. |
| Finance, packages, invoices, payments, credit ledger, refunds, discounts, scholarships, promotions | RBAC-only | Financial staff are not care-team members; financial-integrity rules already govern these. |
| Contracts & consent (`legal`) | RBAC-only | Administrative/legal function. |
| Parent portal | Own ABAC (guardian linkage) — unchanged | Already relationship-based via `guardian_portal_access`. |
| Notifications | RBAC-only | Delivery-scoped per recipient already. |
| KPI / reporting | RBAC-only (aggregated) | Aggregates, not row-level clinical reads. Guard *export* of identifiable clinical data via RBAC export permissions. |

---

## 4. Hierarchical Exceptions (Bypass)

### 4.1 Who bypasses and why

| Actor | Bypass? | Scope / condition |
|---|---|---|
| Corporation Admin | Yes | Corporation-wide. Operational necessity. |
| Branch/Campus Admin | Yes, **campus-scoped** | Bypass limited to their `user_role.campus_id`. |
| Clinical Coordinator | Yes, campus/program-scoped | Must triage and reassign across the caseload. |
| Program Coordinator | Yes, scoped | Oversees a program's students. |
| Lead Psychologist | Yes, scoped | Clinical oversight. |
| Supervisor | **Preferably no blanket bypass** | Derive access via (a) being assigned with the `supervisor` care-team role, or (b) the existing `educators.educator_hierarchy` graph feeding the access function. Keeps least-privilege. |
| Compliance Officer | Yes, **read-only** | Oversight; pair with mandatory access logging. |
| Auditor | Yes, **read-only, logged** | External/internal audit; never write. |

### 4.2 How to implement bypass

**Recommendation: a permission (`care_team:bypass`), surfaced to the DB via a derived GUC** — not a role-name check (the project rule forbids authorizing by role name) and not a hardcoded policy flag.

- Define permission `care_team:bypass` (and read-only variant if desired). Tenants grant it to whichever configurable roles they choose (Coordinator, Admin, etc.).
- The API already builds `perm` claims. The tenant-context interceptor (`TenantConnectionInterceptor`) additionally sets a session GUC:
  - `set_config('app.care_team_bypass', 'true'|'false', false)` derived **only** from the verified `perm` claim — same trust model as `app.current_corporation_id`.
- RLS policies and app filters consult this GUC. This keeps the hot RLS path free of per-row permission lookups while preserving "permissions are the source of truth."
- **Campus-scoped bypass:** for branch admins/coordinators, combine the bypass with the existing campus scope already available in `user_role.campus_id` (and `ITenantContext.CampusId`). The policy can read an optional `app.current_campus_id` GUC to constrain bypass to a campus.

Why a permission (attribute-fed GUC) over alternatives: a *role capability* would violate the no-role-name rule; a *static policy flag* isn't tenant-configurable; a *pure per-row attribute rule* (re-deriving permissions inside RLS) is correct but slow. The GUC-from-permission approach is the same pattern the platform already trusts for tenancy.

---

## 5. Enforcement Mechanism

### 5.1 Options

| | A) RLS only | B) App-layer only | C) Combined (recommended) |
|---|---|---|---|
| Security | Strong — non-bypassable, covers ad-hoc/report queries | Weak — one forgotten `WHERE` leaks data | Strong (RLS backstop) |
| Performance | EXISTS subquery per row; needs indexing | Tunable, eager | Tunable; RLS adds bounded overhead |
| Maintainability | Centralized in DB; logic split from app | All in C#; familiar | Slightly duplicated intent |
| Accidental bypass risk | Very low | High | Very low |
| Architecture fit | **Matches tenant-isolation philosophy** | Diverges from RLS strategy | Matches + good UX |

### 5.2 Recommendation — Combined (RLS as backstop, app as UX)

**The critical technical point: the care-team policy must be RESTRICTIVE, not PERMISSIVE.**

PostgreSQL combines multiple *permissive* policies with **OR**. The existing `tenant_isolation` policy is permissive. Adding another permissive policy would **widen** access (tenant OR care-team), which is wrong. A **restrictive** policy is **AND**-combined, so a row is visible only when it passes `tenant_isolation` **AND** the care-team check.

```sql
-- ILLUSTRATIVE — not final DDL
ALTER TABLE students.case_note ENABLE ROW LEVEL SECURITY;

CREATE POLICY care_team_read ON students.case_note
  AS RESTRICTIVE
  FOR ALL
  USING (
       coalesce(nullif(current_setting('app.care_team_bypass', true), '')::boolean, false)
    OR students.user_can_access_student(student_id)
  );
```

A single shared helper centralizes the relationship logic and handles tables where `student_id` is indirect:

```sql
-- ILLUSTRATIVE
CREATE FUNCTION students.user_can_access_student(p_student_id uuid)
RETURNS boolean
LANGUAGE sql STABLE
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM students.student_care_assignment a
    JOIN educators.educator e ON e.id = a.educator_id
    WHERE a.student_id = p_student_id
      AND e.user_id = core.current_user_id()
      AND a.status = 'active'
      AND a.deleted_at IS NULL
      AND a.active_from <= now()
      AND (a.active_to IS NULL OR a.active_to > now())
  );
$$;
```

- **Indirect tables** call the helper with the resolved student id (e.g. `education.goal_progress` → its `student_goal.student_id`; `scheduling.session_note` → `session_participant.student_id`). For these, the policy uses an `EXISTS` join up to the owning student.
- **Alignment with tenant isolation:** the helper queries `student_care_assignment`, itself tenant-isolated, so cross-corporation leakage remains impossible — care-team narrows *within* an already tenant-scoped set. The owner role still bypasses all RLS for migrations/seed (unchanged).
- **App layer** adds the same predicate to clinical list queries (eager filtering, pagination correctness) and returns clean 403/empty states. The DB remains the source of truth and the backstop.

**Performance:** index `student_care_assignment (educator_id, student_id) WHERE active` and `(student_id)`; ensure `educator.user_id` is indexed. The helper is `STABLE` so it is evaluated per row but with index-only EXISTS. For very large clinical list screens, the app-layer pre-filter (join to assignments) keeps RLS as a cheap confirmatory check rather than the primary filter. Consider a flattened `(user_id, student_id)` resolution view if EXPLAIN shows pressure.

---

## 6. Migration Impact

### 6.1 Can it be additive / zero breaking change?

**Yes, structurally additive.** Implementation requires only:

- **New tables:** `students.student_care_assignment` (+ optional `care_team` grouping table if desired later).
- **New reference values:** `ref_type 'care_team_role'` + values (an INSERT, not DDL, per the reference-data engine).
- **New indexes:** on the assignment table.
- **New RLS policies:** *restrictive* `care_team_*` policies on the clinical tables in §3.1 (policies are additive catalog objects — **no `ALTER TABLE … column`**).
- **New helper function(s):** `students.user_can_access_student(...)`.
- **New permissions:** `care_team:bypass`, plus management codes `care_team:read` / `care_team:assign` (catalog INSERT, mirroring V6/V10).
- **New GUC wiring:** extend `TenantConnectionInterceptor` to set `app.care_team_bypass` (and optionally `app.current_campus_id`).
- **New menu item(s)** for care-team management (mirroring the V10 pattern).

**No existing table, column, or FK is modified.**

### 6.2 The one unavoidable behavioral change

Turning on a **restrictive** policy makes clinical reads *stricter at runtime*. Without mitigation, any user lacking both an assignment and the bypass would suddenly see zero clinical rows. This is a behavior change, not a schema break, and must be sequenced:

1. Ship the table, ref data, permission, and **grant `care_team:bypass` to all currently-privileged roles first** (Phase 2).
2. Backfill `educator.user_id` for active clinicians and create initial assignments (e.g. seeded from recent `session_educator` history) **before** policies go live.
3. Only then enable the restrictive policies (Phase 3), ideally behind a rollout switch so it can be enabled per tenant.

### 6.3 Impact on implemented modules (4A–4R)

- **Touched (read paths gain ABAC awareness):** Students/case management, Assessment (enrolled-student sessions), Education/goals & progress, Education Plans, Scheduling **session notes**, clinical file access.
- **Untouched:** CRM, core Scheduling (sessions/rooms/attendance/make-up), Finance (all), Legal/contracts, Notifications, KPI, IAM/RBAC, tenancy, reference data, localization.
- **Write paths:** assignment CRUD is a new feature (commands/handlers/validators + controller), gated by `care_team:assign`.

---

## 7. Future Extensibility

The single effective-dated assignment table, with a small set of forward-looking columns included **from day one**, supports every listed future need **without redesign**:

| Future need | Supported by |
|---|---|
| Time-bounded access grants | `active_from` / `active_to` |
| Temporary access delegation | `grant_type` (reference data) + `source_assignment_id` (self-FK) + `granted_by` |
| Substitute educator access | `grant_type = 'substitute'` + `source_assignment_id` pointing at the covered assignment + `active_to` |
| Supervisor access | `care_team_role = 'supervisor'`, or via `educator_hierarchy` feeding `user_can_access_student` |
| External consultant / visiting specialist | role value + bounded dating; no schema change |
| Emergency ("break-glass") access | `grant_type = 'emergency'` + mandatory `reason` + heightened audit logging; short `active_to` |

**Attributes to include from the beginning** (to avoid future migrations):

- `active_from`, `active_to` (effective dating)
- `status`, `deleted_at` + full audit fields (BaseEntity)
- `role_id` → `ref.ref_value` (`care_team_role`)
- `is_primary`
- `campus_id`
- `grant_type_id` → `ref.ref_value` (`care_team_grant_type`: permanent/temporary/delegated/substitute/emergency)
- `source_assignment_id` (nullable self-FK, delegation/substitution provenance)
- `granted_by` (user), `reason` (text, required for emergency/delegated)

Because all variability is expressed as **reference data + dating + provenance**, no new tables or columns are needed later. Break-glass and delegation become *rows*, not *schema*.

---

## 8. Implementation Roadmap

| Phase | Scope | Key risks | Validation |
|---|---|---|---|
| **1 — Architecture approval** | This document; confirm scope (§3), bypass policy (§4), restrictive-RLS approach (§5). Update `ARCHITECTURE.md` §Authorization (human-approved per the file's update rule). | Scope creep; locking decisions too early. | Stakeholder sign-off; KVKK/clinical review of §3 list. |
| **2 — DDL migration (no policies yet)** | Additive migration: assignment table, indexes, ref values, permissions (incl. `care_team:bypass`), menu, helper function. Grant bypass to existing privileged roles. Backfill `educator.user_id`; seed initial assignments. | `educator.user_id` gaps; incomplete seed → future lockout. | Migration idempotent (ON CONFLICT); verify counts; confirm every active clinician maps to a user; `verify.sql`-style checks. |
| **3 — RLS integration** | Deploy **restrictive** clinical policies, ideally behind a per-tenant rollout switch. | Permissive-vs-restrictive mistake (silent over-permit); lockout if bypass not seeded; recursion/perf. | Automated tests: (a) assigned educator sees only their students; (b) unassigned blocked; (c) bypass role sees all; (d) tenant isolation still zero-leak with unset tenant; (e) EXPLAIN on clinical lists; (f) confirm policies are `RESTRICTIVE`. |
| **4 — Backend enforcement** | App-layer predicate in clinical query handlers; clean 403/empty responses; assignment management commands/queries; interceptor sets `app.care_team_bypass` (+ campus) from claims. | Drift between app filter and RLS predicate; GUC sourced from untrusted input. | Parity tests app-filter vs RLS; ensure GUC derives only from verified claims; integration tests per clinical endpoint. |
| **5 — Frontend permission-awareness** | Care-team assignment UI; hide/disable clinical actions for non-members; "not on this student's care team" messaging; menu via `required_permission_id`. | UX revealing existence of records; inconsistent client/server gating. | E2E per role; verify client never the sole gate; accessibility of empty/denied states. |

**Cross-phase validation strategy:** keep the DB the source of truth; every app-layer rule must have a corresponding RLS guarantee, tested by attempting raw queries as the app role with no app-layer filter (rows must still be restricted). Add an automated test analogous to the existing tenant-isolation test (doc 08 §5): *with a non-member user context, clinical tables return only assigned rows; with bypass, all tenant rows; with no tenant, zero rows.*

---

## 9. Risks

1. **Restrictive-policy lockout.** If bypass/assignments aren't seeded before policies go live, clinical screens go dark. → Phase ordering + per-tenant rollout switch + pre-flight checks.
2. **Permissive vs restrictive confusion.** A permissive care-team policy would *widen* access (OR with tenant policy). → Mandate `AS RESTRICTIVE`; assert it in tests.
3. **`educator.user_id` gaps.** Clinicians without a linked user can't be matched. → Backfill + validation gate in Phase 2.
4. **Indirect `student_id` resolution.** `goal_progress`, `session_note`, polymorphic `file_object`/`file_attachment` have no direct `student_id`. → Resolve via documented join paths; for generic files, gate at the owning clinical record initially, defer direct file-level ABAC.
5. **Performance on hot clinical lists.** Per-row EXISTS in RLS. → Targeted indexes, app-layer pre-filter, optional flattened resolution view; EXPLAIN-gate in Phase 3.
6. **GUC trust.** `app.care_team_bypass` must derive solely from verified `perm` claims, never client input — identical discipline to `app.current_corporation_id`.
7. **Coordinator visibility expectations.** Coordinators often expect campus-wide clinical visibility. → Campus-scoped bypass via existing `user_role.campus_id`.
8. **Pre-enrollment data.** Lead/CRM assessments have no care team. → ABAC applies only when `student_id` is set; lead-stage stays RBAC.
9. **Audit completeness.** ABAC narrows *who*, but reads of clinical data should still be logged (doc 08 §1). → Pair clinical reads with `core.activity_log`, especially for bypass/emergency/auditor access.

---

## 10. Recommendations (summary)

1. Adopt **Option B**: dedicated `students.student_care_assignment`, effective-dated, role as reference data — model it on the Parent-Portal ABAC precedent.
2. Enforce with the **combined model**: a **RESTRICTIVE** RLS policy as the non-bypassable backstop + app-layer filtering for UX, via a shared `user_can_access_student()` helper.
3. Implement **bypass as a permission** (`care_team:bypass`) surfaced through a new derived GUC `app.care_team_bypass`; scope admins/coordinators by campus; keep Compliance/Auditor read-only and logged; prefer supervisor access via assignment/hierarchy over blanket bypass.
4. Apply ABAC to **clinical/educational data only** (§3.1); keep operational/financial/CRM **RBAC-only**; keep `students.student` master row RBAC-only and gate clinical satellites.
5. Ship as **additive migration** (new table, indexes, ref values, permissions, restrictive policies, helper, menu, GUC wiring) — no existing column/FK changes; manage the one runtime-behavior change via **phased rollout** and pre-seeded bypass/assignments.
6. Include forward-looking columns (`grant_type_id`, `source_assignment_id`, `granted_by`, `reason`, effective dating) **from day one** so delegation, substitution, supervisor, external/visiting, and emergency access never require a redesign.
