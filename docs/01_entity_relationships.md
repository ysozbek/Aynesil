# 01 — Entity Relationship Explanations

This document explains the key entities and how they relate, grouped by bounded context. Diagrams are simplified (key columns / FKs only). Every tenant-scoped table additionally carries `corporation_id` and the standard audit columns (`created_at/by`, `updated_at/by`, `deleted_at`, `row_version`).

> Legend: `1—∞` one-to-many · `∞—∞` many-to-many (via junction) · dashed = soft (no DB-level FK, intentional to avoid cycles).

---

## 1. Tenancy & Identity (Layer 1)

```mermaid
erDiagram
  corporation ||--o{ campus : has
  corporation ||--o{ user_account : has
  user_account ||--o{ user_identity : "external IdP"
  user_account ||--o{ user_role : granted
  role ||--o{ user_role : assigned
  role ||--o{ role_permission : grants
  permission ||--o{ role_permission : in
  campus ||--o{ user_role : "optional scope"
```

- **corporation** is the tenant root. **campus** is a branch within a corporation. Users belong to a corporation and optionally have a primary campus.
- **user_role** is the RBAC grant. Its optional `campus_id` makes a grant *campus-scoped*: the same user can be an Administrator at one campus and a read-only user elsewhere.
- **permission** is a global `resource:action` catalog; **role_permission** maps permissions to roles. Authorization = union of permissions across a user's active `user_role` grants, filtered by campus scope.
- **menu_item** is a self-referential tree; visibility is driven by `required_permission_id`, so menus are *dynamic* per user.

---

## 2. Configurable Reference Data (Layer 1)

```mermaid
erDiagram
  ref_type ||--o{ ref_value : categorizes
  ref_value ||--o{ ref_value : "parent (hierarchy)"
  ref_value ||--o{ ref_value_translation : localized
  ref_value ||--o{ ref_value_tenant_override : "tenant overlay"
  corporation ||--o{ ref_value : "tenant-specific (nullable)"
```

- **ref_type** is the *catalog of lists* (`session_type`, `therapy_type`, …). **ref_value** holds the entries. `corporation_id IS NULL` ⇒ system/global; set ⇒ tenant-specific.
- Business tables reference **ref_value(id)** (e.g. `session.session_type_id`). `scheduling.session` additionally pins the category via a **composite FK** `(session_type_ref_type, session_type_id) → ref_value(ref_type_id, id)`, so a value of the wrong category is rejected by the database.
- **ref_value_tenant_override** lets a tenant deactivate / reorder / re-default a shared value without mutating it. The view `ref.v_effective_ref_value` resolves the tenant-effective list.

---

## 3. CRM → Admissions → Student (lifecycle spine)

```mermaid
erDiagram
  lead ||--o{ lead_activity : logs
  lead ||--o{ lead_status_history : tracks
  lead ||--o{ interview : "pre-enrollment"
  lead ||--o{ assessment_session : evaluated
  lead ||--o| student : "converts to"
  student ||--o{ enrollment : has
  enrollment ||--o{ student_program : contains
  program ||--o{ student_program : "instantiated as"
```

- A **lead** moves through `lead_status` / `pipeline_stage` (both reference data). Communication is captured in **lead_activity**; pre-enrollment **interview** records outcomes.
- On conversion, `lead.converted_student_id` links to **student** (and `student.lead_id` keeps the back-reference as a soft link). Assessment history attached to the lead remains valid because **assessment_session** can reference *either* a `lead_id` or a `student_id`.
- **enrollment** is the overall lifecycle record (status from `enrollment_status`); **student_program** binds a student to one or more **programs** (a student may have many concurrent programs).

---

## 4. Assessment & Evaluation

```mermaid
erDiagram
  assessment_template ||--o{ assessment_section : contains
  assessment_section ||--o{ assessment_item : contains
  assessment_template ||--o{ assessment_session : instantiated
  assessment_session ||--o{ assessment_response : captures
  assessment_item ||--o{ assessment_response : answered
  assessment_session ||--o| assessment_report : produces
  assessment_session ||--o{ program_recommendation : yields
```

- **assessment_template** (versioned, translatable) defines sections and items. An **assessment_session** is one administration of a template to a lead/student by an assessor (educator), capturing **assessment_response** rows and an **assessment_report**.
- **program_recommendation** is the bridge to enrollment; `recommended_program_id` is a soft reference to `education.program` (avoids a cross-domain cycle).

---

## 5. Students, Guardians & Clinical/Case Management

```mermaid
erDiagram
  student ||--o{ student_campus : "multi-campus"
  student ||--o{ student_guardian : has
  guardian ||--o{ student_guardian : of
  guardian ||--o| user_account : "portal login"
  student ||--o{ emergency_contact : has
  student ||--o{ diagnosis : has
  student ||--o{ developmental_profile : has
  student ||--o{ medical_report : has
  student ||--o{ development_report : has
  student ||--o{ external_institution_report : has
  student ||--o{ case_note : has
```

- **student_campus** models multi-campus service delivery (a student may attend several campuses, one flagged primary).
- **student_guardian** is the M:N with `relationship_id`, custody, portal, and financial-responsibility flags. A **guardian** gains portal access by linking to a **user_account**.
- Clinical records (**diagnosis**, **medical_report**, etc.) are KVKK special-category data: soft-deleted, audited (DB trigger), and their files flagged `is_sensitive`.

---

## 6. Educators & Hierarchy

```mermaid
erDiagram
  educator ||--o{ educator_campus : "works at"
  educator ||--o{ educator_specialty : has
  educator ||--o{ educator_certification : holds
  educator ||--o{ educator_hierarchy : "is supervised (edges)"
  educator ||--o| user_account : "login"
```

- **educator.title_id** is configurable reference data (Therapist/Psychologist/Consultant/… and any tenant addition).
- **educator_hierarchy** is an *edge list* (`educator_id`, `supervisor_id`, `relationship_id`, optional `campus_id`) — a flexible graph supporting Educator → Consultant → Coordinator and any depth, optionally per campus.

---

## 7. Goals & Education Plan (BEP/IEP)

```mermaid
erDiagram
  goal_library ||--o{ goal_template : contains
  goal_template ||--o{ student_goal : instantiated
  student ||--o{ student_goal : has
  student_goal ||--o{ student_goal : "long->short term"
  student_goal ||--o{ goal_progress : measured
  education_plan ||--o{ education_plan_goal : includes
  student_goal ||--o{ education_plan_goal : referenced
  education_plan ||--o{ education_plan_review : reviewed
  education_plan ||--o{ education_plan_approval : approved
  education_plan ||--o{ education_plan_revision : versioned
```

- Reusable **goal_template** (translatable; categorized by `goal_category` / `development_area`) instantiate into **student_goal** (which can nest long-term → short-term via `parent_goal_id`).
- **goal_progress** is the time series for trend analysis. **education_plan** (BEP) bundles goals, carries `status` (draft→approved→active), is approved by a coordinator (**education_plan_approval**), and is visible to guardians only when `status='approved'` AND `guardian_visible=true`. **education_plan_revision** snapshots versions.

---

## 8. Scheduling (Sessions, Rooms, Recurrence, Attendance, Make-up)

```mermaid
erDiagram
  recurring_schedule ||--o{ session : generates
  recurring_schedule ||--o{ recurrence_exception : excepts
  room ||--o{ session : "booked in"
  session ||--o{ session_participant : students
  session ||--o{ session_educator : educators
  session ||--o{ session_goal : targets
  session ||--o{ session_note : notes
  session ||--o{ attendance : records
  session ||--o{ makeup_request : "missed -> make-up"
```

- **session** is the single schedulable unit; `session_type_id` is configurable (composite-FK pinned). `time_range` is a generated `tstzrange` used for conflict checks.
- **Conflict prevention:** an `EXCLUDE USING gist` constraint blocks overlapping non-cancelled sessions in the same **room**; a trigger blocks an **educator** being double-booked across sessions.
- **session_participant** (M:N students) supports group/intensive/camp sessions; **session_educator** (M:N) supports co-treatment. **session_note.parent_visible** governs portal exposure.
- **attendance** (reason = reference data) feeds statistics; **makeup_request** tracks a missed session through scheduling and completion.

---

## 9. Finance

```mermaid
erDiagram
  package_definition ||--o{ student_package : purchased
  student_package ||--o{ credit_ledger : "balance (append-only)"
  student_package ||--o{ invoice_line : billed
  invoice ||--o{ invoice_line : contains
  invoice ||--o{ payment : "paid by"
  payment ||--o{ refund : refunded
  invoice ||--o{ discount : applies
  student ||--o{ scholarship : awarded
  session ||--o{ credit_ledger : consumes
```

- **student_package** holds purchased credits; the *authoritative balance is the SUM of* **credit_ledger** *deltas* (grant/consume/refund/expire) — never a mutable column. A consumed session writes a `consume` ledger row.
- **invoice/invoice_line/payment/refund** model billing; **payment.idempotency_key** dedupes gateway callbacks. **discount/scholarship/promotion** types are all reference data.

---

## 10. Contracts & Consent (KVKK)

```mermaid
erDiagram
  contract_template ||--o{ contract_template_translation : localized
  contract_template ||--o{ student_contract : signed
  consent_template ||--o{ consent_template_translation : localized
  consent_template ||--o{ student_consent : granted
  student ||--o{ student_contract : has
  student ||--o{ student_consent : has
  student_consent ||--o{ viewing_authorization : "gates camera"
```

- Templates are versioned and translatable; signed instances (**student_contract**) store the immutable signed file and e-signature reference.
- **student_consent** is the KVKK consent ledger (granted/withdrawn). A `camera_viewing` consent is what authorizes **media.viewing_authorization**.

---

## 11. Camera & Live Viewing

```mermaid
erDiagram
  camera ||--o{ room_camera : "mounted in"
  room ||--o{ room_camera : has
  camera ||--o{ session_camera : covers
  session ||--o{ session_camera : has
  guardian ||--o{ viewing_authorization : granted
  viewing_authorization ||--o{ viewing_log : produces
```

- **viewing_authorization** is time-boxed, tied to a backing `camera_viewing` consent, and produces an immutable **viewing_log** (who watched what, when) for privacy auditing.

---

## 12. Meetings, Leave, Performance · Camps · Consultancy

- **ops.meeting** (type = reference data) with **meeting_participant** (users/guardians/leads/external), **meeting_outcome**, **meeting_follow_up** (action items).
- **ops.leave_request** (type = reference data) with an `EXCLUDE` constraint preventing overlapping leave, an approval chain (**leave_approval**), and **leave_balance** accruals.
- **ops.educator_performance_snapshot** rolls up session volume, attendance, goal-achievement, parent feedback, utilization — also expressible generically via `core.kpi_value`.
- **camps.camp → camp_period → camp_enrollment → camp_attendance/camp_report**; reuses reference data (`camp_type`, `attendance_reason`).
- **consultancy.institution → consultancy_plan → school_visit → observation_record → consultancy_report** (`institution_type` = reference data).

---

## 13. Parent Portal (projection layer)

The portal is mostly authorization over existing domains. **students.guardian_portal_access** holds per-(guardian, student) visibility switches. Read views (`v_portal_my_students`, `scheduling.v_portal_sessions`, `finance.v_portal_package_balance`, `education.v_portal_education_plan`) resolve the current portal user via `core.current_user_id()` and respect `parent_visible` / approved-status gates. RLS on base tables still applies.
