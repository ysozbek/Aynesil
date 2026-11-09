# Cross-Layer Business Workflow Alignment Report

**Date:** 2026-10-20
**Scope:** Deep audit + safe P0/P1 fixes across the core clinical workflow
(Students → Guardians → Enrollments/Programs → Scheduling/Sessions →
Assessments → Goals/BEP → Educators/Leave/KPI) plus cross-cutting
permissions/menu/router/tenant isolation. Remaining 20+ modules received a
lightweight inventory with top findings only.
**Mode:** P0 (security/data integrity) + P1 (cross-layer breaks that prevent
real workflow completion) fixes applied within the existing architecture and
approved schema. P2/P3 and schema-change items are reported as recommendations.

---

## A. Overall status

**Verdict: YELLOW**

| Dimension                                      | State before     | State after            |
| ---------------------------------------------- | ---------------- | ---------------------- |
| Core clinical workflow end-to-end usability    | RED (many breaks) | YELLOW (P0+P1 fixed)   |
| Backend endpoint ↔ API client contract         | RED (drift)      | GREEN for core modules |
| UI permission codes ↔ backend catalog          | YELLOW            | GREEN for core modules |
| Status enums alignment (BEP, leave, makeup)    | RED (mismatch)   | GREEN                  |
| Tenant isolation (RLS + app-layer)             | YELLOW            | YELLOW (unchanged; see F) |
| Deferred-architecture adherence (ABAC)         | YELLOW            | YELLOW (reported; see F) |
| Approved schema integrity (no DDL changes)     | GREEN             | GREEN                   |

**Headline reasons for YELLOW (not GREEN):**

1. Several high-value items need an explicit product/schema decision and are
   intentionally not fixed in this task — see section **H. BLOCKED decisions**.
2. The audit intentionally did not cover ~15 non-core modules in depth
   (consultancy, camps, cameras, corporate ops, notifications, legal/consents,
   portal, surveys, etc.) — see section **I. Remaining work list**.
3. Partial Care Team / ABAC enforcement is already implemented in production
   code despite `ARCHITECTURE.md` stating ABAC is deferred — this architectural
   drift is reported, not changed.

---

## B. Module and workflow inventory

The audit started from seven parallel deep-dive explorations. Each produced a
cross-layer inventory of DB tables, backend commands/queries, API controllers,
Pinia stores, Vue views, routes, permissions, and detailed per-finding evidence.
The findings below are grounded in that inventory; this report keeps the
inventory compact and surfaces the actionable deltas.

### B.1 Students + Guardians

- **DB** (`db/layer2_sped/01_students.sql`): `student`, `student_status_history`,
  `student_campus`, `guardian`, `student_guardian`, `emergency_contact`,
  `developmental_profile`, `diagnosis`, `medical_report`, `development_report`,
  `external_institution_report`, `case_note`. Related: `guardian_portal_access`
  (13), `student_consent` (legal, 08), `student_care_assignment` (14).
- **Backend:** `src/Aynesil.Application/Features/Students/*` — commands for
  Student CRUD, status, campus (enroll/transfer/**end**), guardians/links,
  emergency, developmental profile, diagnosis, reports, case notes, portal
  access. Controllers: `StudentsController`, `GuardiansController`.
- **Permissions:** `student:*`, `guardian:*`, `case_note:*` (incl.
  `read_confidential`), `portal:access`.
- **API/stores/views:** `student.service.ts`, `guardian` wrapper in same file,
  `student.store.ts`, `case.store.ts`, `guardian.store.ts`, `consent.store.ts`.
  Views: `students/*` (list/form/detail/dashboard/timeline),
  `guardians/*` (list/form/detail).

### B.2 Enrollments + Programs

- **DB** (`db/layer2_sped/05_education.sql` top section): `program`,
  `program_translation`, `program_service`, `enrollment`, `student_program`.
  Enrollment ↔ `student_program` link is **nullable** (student_programs can
  exist standalone).
- **Backend:** `src/Aynesil.Application/Features/Programs/*`. Three controller
  classes in `ProgramsController.cs`: `ProgramsController` (`/api/programs`),
  `EnrollmentsController` (`/api/enrollments`), `StudentProgramsController`
  (`/api/student-programs`).
- **Permissions:** `program:*`, `enrollment:read|create|update|manage_programs`.
  (No separate `student_program:*` prefix — the "manage_programs" permission
  governs StudentProgram CRUD under the enrollment-scoped route.)
- **API/stores/views:** `program.service.ts` (3 exports: `programService`,
  `enrollmentService`, `studentProgramService`), `program.store.ts`.
  Views: `programs/ProgramListView|FormView|DetailView` + new
  `EnrollmentListView|DetailView`. Student detail now has a Programs tab that
  lists all student_programs including standalone ones.

### B.3 Scheduling + Sessions

- **DB** (`db/layer2_sped/06_scheduling.sql`): `room`, `calendar_entry`,
  `recurring_schedule`, `recurrence_exception`, `session`, `session_participant`
  (optional `student_program_id`), `session_educator`, `session_goal`,
  `session_note`, `attendance`, `makeup_request`.
- **Integrity:** `excl_room_overlap` GiST EXCLUDE on `(room_id, time_range)`
  + `trg_educator_overlap` trigger on `session_educator`.
- **Backend:** `src/Aynesil.Application/Features/Scheduling/*`. Single
  `SchedulingController` with route base `/api/scheduling`. `ISessionRepository`
  holds `HasRoomConflictAsync` / `HasEducatorConflictAsync` (defense in depth
  for the DB constraints).
- **Permissions:** `room:*`, `session:*` (incl. `reschedule|complete|cancel|`
  `manage_participants|manage_educators|manage_goals|manage_calendar|`
  `bulk_generate|bulk_cancel|bulk_reassign`), `session_note:*`, `attendance:*`,
  `makeup_request:*` (`request|manage`).
- **API/stores/views:** `scheduling.service.ts`, split stores
  (`scheduling.store`, `session.store`, `attendance.store`, `calendar.store`,
  `makeupSession.store`). Views: `scheduling/*`.

### B.4 Assessment

- **DB** (`db/layer2_sped/04_assessment.sql`): `assessment_template` (versioned
  rows), `assessment_template_translation`, `assessment_section`,
  `assessment_item`, `assessment_session`, `assessment_response`,
  `assessment_report`, `program_recommendation`. No separate `assessment_result`
  table — session carries `total_score`; narrative lives in `assessment_report`.
- **Backend:** `src/Aynesil.Application/Features/Assessment/*`. Controllers:
  `AssessmentTemplatesController`, `AssessmentSessionsController`.
- **Permissions:** `assessment_template:*`, `assessment_session:*`,
  `assessment_report:*`, `program_recommendation:*`.
- **API/stores/views:** `assessment.service.ts`, split stores
  (`assessment.store`, `assessmentTemplate.store`). Views: `assessment/*`
  (dashboard, template list/form/detail, session list/form/detail).

### B.5 Goals + Education Plans (BEP/IEP)

- **DB** (`db/layer2_sped/05_education.sql` lower section): `goal_library`,
  `goal_template`, `goal_template_translation`, `student_goal` (with
  `parent_goal_id` for long-/short-term nesting), `goal_progress`,
  `academic_period`, `education_plan`, `education_plan_goal`,
  `education_plan_review`, `education_plan_approval`, `education_plan_revision`.
- **Plan status workflow (DDL + domain):** `draft → in_review → approved →
  active → revised | closed` (`EducationPlan.cs`).
- **Backend:** `src/Aynesil.Application/Features/Goals/*`,
  `src/Aynesil.Application/Features/Plans/*`. Controllers: `GoalsController`,
  `EducationPlansController`.
- **Permissions:** `goal_library:*`, `goal_template:*`, `student_goal:*`
  (+ `change_status`), `goal_progress:*`, `academic_period:*`,
  `education_plan:*` (`submit|approve|revise|manage_goals|add_review|`
  `guardian_view`).
- **API/stores/views:** `goal.service.ts`, `bep.service.ts`, `goal.store`,
  `bep.store`. Views: `goals/*`, `bep/*`.

### B.6 Educators + Leave + KPI

- **DB** (`db/layer2_sped/02_educators.sql`, `10_ops.sql`,
  `db/layer1_core/05_platform_services.sql`): `educator`, `educator_campus`,
  `educator_specialty`, `educator_certification`, `educator_hierarchy`;
  `leave_request`, `leave_approval`, `leave_balance`;
  `educator_performance_snapshot`, `parent_feedback`; `core.kpi_definition`,
  `core.kpi_value`. No `educator_schedule` table (availability via
  `session_educator` + ops leave). No `kpi_measurement` table (values live in
  `core.kpi_value` and are populated by compute, not manual insert).
- **Backend:** `EducatorsController`, `LeaveController`,
  `PerformanceKpiController` + feature folders.
- **Permissions:** `educator:*`, `leave_request:*`, `leave_balance:*`,
  `leave_report:*`, `kpi:*`, `kpi_snapshot:*`, `kpi_report:*`,
  `kpi_dashboard:read`, `parent_feedback:*`.
- **API/stores/views:** `educator.service.ts`, `leave.service.ts`,
  `kpi.service.ts`; `educator.store`, `leave.store`, `leaveApproval.store`,
  `kpi.store`, `performance.store`. Views: `educators/*`, `leave/*`, `kpi/*`.

### B.7 Cross-cutting (permissions / router / menu / authorization / tenancy)

- **327** permission constants in `src/Aynesil.Shared/Constants/Permissions.cs`.
  **307** attached to at least one endpoint; **20** currently dead at the API
  layer (e.g. `file:*`, `audit:read`, `integration:*`, generic `report:*`,
  `settings:*`, `user:reset_password`).
- **~579** controller actions across 35 controllers; **575** gated by
  `[HasPermission]`. Non-permission-gated: `Health` (anonymous),
  `AuthController` logout/change-password (authorized only), `MenusController`
  `/me` (filters by caller permissions), `SettingsController` (shell).
- **171** named Vue routes; **169** declare `meta.permission`.
- **Tenant isolation:** JWT → `TenantContextService` →
  `TenantConnectionInterceptor` sets PostgreSQL GUC
  `app.current_corporation_id` on every connection
  (`src/Aynesil.Infrastructure/Persistence/Interceptors/TenantConnectionInterceptor.cs`).
  Generic RLS on every table with `corporation_id` uses
  `with check (corporation_id = core.current_corporation_id())`
  (`db/99_triggers_rls_policies.sql` L57–62).
- **Care Team ABAC:** `CareTeamFilter`, `CareTeamController`,
  `app.care_team_bypass` GUC, and RESTRICTIVE RLS in
  `db/layer2_sped/15_care_team_rls.sql` are **already implemented**, which
  contradicts `ARCHITECTURE.md`'s "defer ABAC" posture. Reported in F.

---

## C. Findings

Priorities: **P0** security/data-integrity, **P1** workflow-breaking,
**P2** UX/consistency, **P3** polish. Items marked **FIXED** have been applied
in this task; **BLOCKED** items are listed again in H with required decisions.

| ID    | Module      | Severity | Status  | Summary |
| ----- | ----------- | -------- | ------- | ------- |
| F-001 | Scheduling  | P0       | FIXED   | Reschedule request payload mismatch (`startsAt`/`endsAt`/string rowVersion). Backend expects `NewStartsAt`/`NewEndsAt`/int. Fix: `types/scheduling.types.ts`, `views/scheduling/SessionFormView.vue`. |
| F-002 | Scheduling  | P0       | FIXED   | Calendar store/view assumed flat `CalendarEventDto[]`; backend returns `CalendarViewDto { sessions, entries }` for school/campus and `SessionListItemDto[]` for room/educator/student. Rewrote `stores/calendar.store.ts` with raw arrays + computed `events`, retypes in `services/scheduling.service.ts`. |
| F-003 | Scheduling  | P0       | FIXED   | Bulk-generate called without body. Backend `BulkGenerateRequest` requires `{ windowStart, windowEnd }`. Added `BulkGeneratePayload`, modal with date picker (`RecurringScheduleListView.vue`), service + store signatures. |
| F-004 | Scheduling  | P0       | FIXED   | Attendance dashboard called student-scoped attendance APIs without the required `corporationId`. Service, store, and `AttendanceDashboardView.vue` updated. |
| F-005 | Scheduling  | P0       | FIXED   | `RowVersion` typed as `string` across the TS layer vs `int` on the backend `RowVersionRequest` records. All scheduling DTOs/payloads now use `number`. |
| F-006 | Scheduling  | P0       | FIXED   | **Backend bug**: `BulkGenerateSessionsCommandHandler` passed `schedule.StudentProgramId` into the eighth parameter of `Session.Schedule`, which is `programServiceId` — causing FK/semantic errors against `education.program_service`. Fixed in `RecurringScheduleCommands.cs` (now passes `null` with named args). |
| F-007 | BEP         | P0       | FIXED   | UI used `pending_review` where backend/DB use `in_review`. Approve/reject actions were invisible on submitted plans. Aligned i18n, both views, filter options. Added `revised` to status map and filter. |
| F-008 | BEP         | P0       | FIXED   | **Backend**: `ReorderPlanGoalsCommand` had no plan-status guard; Add/Remove already required draft/in_review. Added same guard to Reorder (`EducationPlanGoalCommands.cs`). UI also hides reorder/add buttons unless the plan is draft/in_review. |
| F-009 | X-cutting   | P1       | FIXED   | Finance router permissions used non-catalog `payment:*` / `credit:read`. Aligned to `finance_payment:read|record` and `credit_ledger:read` in `router/index.ts`. Updated `v-if="can(...)"` in `PaymentDetailView`, `PaymentListView`, `FinanceDashboardView`. |
| F-010 | X-cutting   | P1       | FIXED   | `StudentsController.GetCaseNotes` checked `User.HasClaim("permission", ...)` whereas all other code uses the `perm` claim via `ICurrentUserService.HasPermission`. The permission `case_note:read_confidential` was effectively dead. Fixed to use `_currentUser.HasPermission(...)`. |
| F-011 | Students    | P1       | FIXED   | **Guardians detail: portal access API called with the student_guardian link id** instead of the real `studentId`. Backend route is `/api/guardians/{guardianId}/students/{studentId}/portal-access`. Fixed both grant/update and revoke paths. |
| F-012 | Students    | P1       | FIXED   | `StudentGuardianDto` omitted `studentId` and `studentFullName`, so guardian-centric screens could not display the student or send its id. Added both fields to backend DTO + projection builders + TS type. Guardian detail now links to the student record and labels the student correctly. |
| F-013 | Students    | P1       | FIXED   | Case-note add/edit/delete gated on `student:write` while backend uses `case_note:create|update|delete`. Granular gates applied. |
| F-014 | Students    | P1       | FIXED   | Developmental-profile edit gated on `student:write` while backend uses `student:update`. Fixed on both the "Add" and "Edit" buttons. |
| F-015 | Students    | P1       | FIXED   | Confidential case notes never requested. `StudentDetailView` and the lazy-load now pass `includeConfidential: true` when the caller holds `case_note:read_confidential`. Enforcement at the API is also fixed via F-010. |
| F-016 | Students    | P1       | FIXED   | "End campus enrollment" endpoint existed in service but had no UI entry point. Added `endCampusEnrollment` to the store and a modal/button on each active campus row in `StudentDetailView`. |
| F-017 | Students    | P1       | FIXED   | Development report update endpoint and service method existed; no store action or edit UI. Added `updateDevelopmentReport` to `case.store.ts` and an Edit button + reused add modal in `StudentDetailView`. |
| F-018 | Enrollment  | P1       | FIXED   | `EnrollmentDetailView` had no way to change a `student_program` status or end it. Added `openEditSp` modal and quick-complete action, calling the existing `updateStudentProgram` store action. |
| F-019 | Enrollment  | P1       | FIXED   | `EnrollmentListView.submitCreate` silently swallowed a failed program assignment after a successful enrollment create. Now surfaces the error, keeps the user on the list, and refreshes the list so they can retry on the detail page. |
| F-020 | Scheduling  | P1       | FIXED   | Makeup status enum (`pending` vs `requested`) and permission tokens (`makeup_request:create|update` vs `request|manage`) misaligned with backend. Fixed in `MakeupRequestListView`, `SchedulingDashboardView`, and i18n. |
| F-021 | Scheduling  | P1       | FIXED   | `MakeupRequestListView` referenced DTO fields the backend list DTO does not emit (`missedSessionTitle`, `missedSessionDate`, `makeupSessionDate`). Column set reduced to what the API actually returns; also cleaned frontend `MakeupRequestDto`. |
| F-022 | Scheduling  | P1       | FIXED   | List view linked to a non-existent `/scheduling/makeup-requests/new` route. Replaced with an inline create modal using the existing `createMakeupRequest` action. |
| F-023 | Scheduling  | P1       | FIXED   | Session detail used coarse `session:update` for Cancel/Complete/Attendance/Participants/Educators/Notes/Goals. Switched to the granular backend permissions (`session:cancel`, `session:complete`, `attendance:record`, `session:manage_participants`, `session:manage_educators`, `session:manage_goals`, `session_note:write|delete`). |
| F-024 | Scheduling  | P1       | FIXED   | Rescheduled sessions (status `rescheduled`) hid all lifecycle actions. Introduced `isLiveSession(status)` so rescheduled sessions keep complete/cancel/reschedule controls, with a localised status label added. |
| F-025 | Assessment  | P1       | FIXED   | Scoring tab only hydrated responses after calling `Start`. Reopening an in-progress session showed blank fields. Added hydration in `onMounted` when `status ∈ {in_progress, completed}`. |
| F-026 | Assessment  | P1       | FIXED   | Frontend DTO types defined fields the backend never emits (`leadContactName`, `studentName`, `assessorName`, `completedAt`, `typeName`, `categoryName`, `templateCode`, `itemPrompt`, `respondedAt`, `finalizedByName`, `recommendedProgramName`, `recommendedByName`). Realigned `assessment.types.ts` to mirror `AssessmentProjection.cs`; swapped UI references to real fields (`performedAt`, `typeCode`, `categoryCode`, IDs where names are not provided). |
| F-027 | BEP         | P1       | FIXED   | Guardian-visibility toggle gated on `education_plan:update`; backend enforces `education_plan:approve`. Fixed. |
| F-028 | BEP         | P1       | FIXED   | Menu seed `V12__goals_plans_permissions_and_menu.sql` publishes `/education-plans/*` URLs while the SPA mounts the views under `/bep/*`. Added redirect aliases so menu rows no longer dead-end. (The menu seed itself needs a follow-up migration — see H.) |
| F-029 | BEP         | P1       | FIXED   | List view's row "edit" action was gated on `row.status === 'draft' || row.status === 'returned'` — but `returned` is not a valid status token. Changed to `draft || in_review` to match the backend `UpdateDetails` rule. |
| F-030 | Leave       | P1       | FIXED   | `LeaveFormView` read `auth.user?.id` (not a real field on `AuthUser`) for `educatorId` on submit. Replaced with an educator picker populated from `useEducatorStore` and a validation gate; also pre-fills from the current record on edit via `l.educatorId`. |
| F-031 | Leave       | P1       | FIXED   | Status/unit casing mismatch — UI used `Pending`/`Approved`/`Rejected`/`Cancelled` and `Day`/`Hour` while backend uses lowercase. Normalized in `LeaveDetailView`, `LeaveListView`, `LeaveDashboardView`, `LeaveCalendarView`, `leaveApproval.store`, and `LeaveFormView`. |
| F-032 | Leave       | P1       | FIXED   | `leave.service.ts` typed `create` return as `LeaveRequestDto`; backend returns `Guid`. `approve`/`reject` return `LeaveApprovalDto`, not the request. `cancel` is NoContent. Service types corrected; store now refetches the full request after approve/reject/cancel and after create. |
| F-033 | Leave       | P1       | FIXED   | Reject button used `leave_request:approve`; backend enforces `leave_request:reject`. Fixed. |
| F-034 | KPI         | P1       | FIXED   | `kpi.service.ts` called wrong URLs (`/educator/{id}/compute`, `/bulk-compute`) vs backend `/educators/{id}/compute`, `/compute/bulk`. Compute body was missing `corporationId`. Removed the non-existent `GET /snapshots/{id}`. Fixed `listKpiValues`/`listParentFeedback` to the real paginated shape, split `getTrends` into `getEducatorTrend` / `getCorporationTrend`, added `listEducatorKpiValues`. Updated store signatures. |
| F-035 | Educators   | P1       | FIXED   | Educator detail fetched reference values using uppercase codes `CERTIFICATION_TYPE`, `EDUCATOR_RELATIONSHIP` while seed codes are lowercase. Changed to `certification_type`, `educator_relationship`. |

### P0 items reported but **not** fixed in this task (see H)

| ID    | Module      | Severity | Status  | Summary |
| ----- | ----------- | -------- | ------- | ------- |
| F-100 | Assessment  | P0       | BLOCKED | `assessment_response` can be mutated after session completion — enforcement is API-only (`SubmitResponsesCommand` requires `in_progress`); domain `AssessmentResponse.Update` has no status check, and the DB table has no trigger/CHECK. Fix requires domain + possibly DB-level enforcement decision. |
| F-101 | Assessment  | P0       | BLOCKED | `assessment_section`, `assessment_item`, `assessment_template_translation` lack `corporation_id` and are not covered by the generic tenant RLS loop. Fix requires DDL (new columns + policies). Out of scope. |
| F-102 | Scheduling  | P0       | BLOCKED | `ScheduleSessionCommand` requires `participantStudentIds` + `educatorAssignments`, but `SessionFormView` sends neither — generated sessions have no participants and no educators until the user adds them from the detail view. Fix requires picker UI (student + educator selectors). |
| F-103 | Scheduling  | P0       | BLOCKED | `RescheduleSessionCommand` only re-checks room conflicts, not educator conflicts, after a time change. The educator-overlap trigger fires on `session_educator` insert/update, not on session time updates. Needs a conflict pre-check in the handler. |
| F-104 | BEP         | P0       | BLOCKED | Approved/active plan content is live-linked to mutable `student_goal` rows (no snapshot). Snapshot exists only on `revise`, not on `approve`. Needs product decision — snapshot on approve, or lock `UpdateStudentGoal` while a goal is on a non-draft plan? |
| F-105 | Students    | P0       | BLOCKED | Create-student/create-guardian commands accept client-supplied `CorporationId` without comparing it to `ITenantContext.CorporationId`. Mitigated by RLS `with check`; defense-in-depth gap. Low-risk fix but applies to many commands — tracked as a cross-module hardening pass. |

### P2 / P3 highlights (not applied; see I for the full backlog)

- Session create/reschedule has no conflict preview (`ConflictCheckDto` exists
  as a backend DTO but no endpoint).
- Program `IsActive` toggle is missing — the delete handler message says
  "Deactivate instead" but no Activate/Deactivate command/endpoint exists.
- End-enrollment does not cascade to `student_program.status`; UI only blocks
  new assignments.
- `program_service` is hard-deleted; schema has no `deleted_at` column — this
  is a reference-data safety concern tracked as BLOCKED (schema change needed).
- CRM root menu seed (V8) still gates on `student:read` while the router uses
  `lead:read`; educators root (V8) gates on `user:read` while routes use
  `educator:read`. Requires a new menu migration.
- Assessment template `version` endpoint does not copy sections/items; operators
  must rebuild. Documented operational risk, not a bug.
- KPI definition create/edit UI is missing (backend supports it).
- `/leave/balances` initialize endpoint has no client method.
- Scheduling `events` type retains the legacy `isMakeup` field for the UI badge
  even though the normalized flat shape is derived client-side.

---

## D. Change log

All edits are grounded in the audits performed by the parallel explore agents
and the plan at `.cursor/plans/cross-layer_alignment_fix_bea54083.plan.md`.
No DDL migrations were created, no existing migrations were edited, no files
belonging to platform infrastructure were touched outside the listed paths.

### D.1 Backend (.NET)

| File | What changed |
|------|---------------|
| `src/Aynesil.Api/Controllers/StudentsController.cs` | Injected `ICurrentUserService`; replaced `User.HasClaim("permission", …)` with `_currentUser.HasPermission(…)` so `case_note:read_confidential` actually gates the `includeConfidential` branch. |
| `src/Aynesil.Application/Features/Scheduling/Commands/RecurringScheduleCommands.cs` | Fixed bulk-generate FK/semantic bug: no longer passes `schedule.StudentProgramId` into `Session.Schedule`'s `programServiceId` parameter. Uses named arguments and `null`. |
| `src/Aynesil.Application/Features/Plans/Commands/EducationPlanGoalCommands.cs` | `ReorderPlanGoalsCommandHandler` now checks `plan.Status ∈ {draft, in_review}` before reordering, matching Add/Remove. Throws a clear error otherwise. |
| `src/Aynesil.Application/Features/Students/Dtos/StudentProjection.cs` | `StudentGuardianDto` gained `StudentId` + `StudentFullName`. Both projection builders (`BuildGuardianLinkDtosAsync` for student-centric, `BuildGuardianStudentLinkDtosAsync` for guardian-centric) now resolve the student and populate the new fields. `ToStudentGuardianDto` grew an optional `Student?` parameter. |
| `src/Aynesil.Application/Features/Students/Commands/GuardianLinkCommands.cs` | `LinkGuardianToStudent` and `UpdateGuardianLink` handlers now pass the student into `ToStudentGuardianDto`. |

### D.2 Frontend TypeScript / Vue

**Types**

- `types/scheduling.types.ts`: `rowVersion` → `number` across all DTOs; renamed
  `RescheduleSessionPayload.{startsAt,endsAt}` → `{newStartsAt,newEndsAt}`;
  added `BulkGeneratePayload`; replaced `date`-only `AddRecurringExceptionPayload`
  with full `{exceptionDate, action, newStartAt?, reason?}`; added
  `CalendarViewDto`; realigned `MakeupRequestDto` + `MakeupRequestListItemDto`
  to real backend shapes.
- `types/student.types.ts`: `StudentGuardianDto` now carries `studentId` +
  `studentFullName`.
- `types/assessment.types.ts`: Removed phantom fields that the API never emits.
  Added `templateVersion`, `performedAt`, `AssessmentSessionStatus` union.
  Renamed `sessionId` → `assessmentSessionId` on responses/reports/
  recommendations. Report now exposes `finalizedBy` (Guid); recommendation now
  exposes `recommendedBy` (Guid).

**Services**

- `services/scheduling.service.ts`: Calendars typed correctly (`CalendarViewDto`
  for school/campus, `SessionListItemDto[]` for room/educator/student). Campus
  / educator / student calendars now accept `corporationId`. Attendance
  endpoints accept `corporationId`. Bulk-generate accepts a body.
  `noShowSession`/`completeMakeupRequest` take numeric `rowVersion`.
- `services/leave.service.ts`: Return types corrected — `create` → `string`
  (Guid), approve/reject → `LeaveApprovalDto`, cancel → `void`.
- `services/kpi.service.ts`: Compute URLs fixed to `/educators/{id}/compute` and
  `/compute/bulk`. Compute now requires `corporationId`. Removed
  `getSnapshot`; added `listEducatorKpiValues`. `getTrends` split into
  `getEducatorTrend` + `getCorporationTrend`. `listKpiValues` /
  `listParentFeedback` typed as paginated.

**Stores**

- `stores/calendar.store.ts`: Rewritten. Owns `sessions` + `entries` arrays and
  exposes a flat computed `events` for the calendar UI. All five fetch methods
  updated for new signatures.
- `stores/attendance.store.ts`: Accepts `corporationId`.
- `stores/session.store.ts`: `noShowSession` takes `number`.
- `stores/scheduling.store.ts`: `generateSessions` requires
  `BulkGeneratePayload`.
- `stores/makeupSession.store.ts`: `completeMakeupRequest` takes `number`.
- `stores/case.store.ts`: Added `updateDevelopmentReport`.
- `stores/student.store.ts`: Added `endCampusEnrollment`.
- `stores/leave.store.ts`: Refetches `fetchLeave(id)` after approve/reject/
  cancel and after create (so `currentLeave` holds the full `LeaveRequestDto`,
  not the step DTO or empty).
- `stores/leaveApproval.store.ts`: Status filter is now `'pending'`.
- `stores/kpi.store.ts`: `fetchKpiValues` / `fetchParentFeedback` read
  `res.data.items`. `computeEducator` requires `corporationId`.
- `stores/performance.store.ts`: Removed `fetchSnapshot` / `currentSnapshot`
  (endpoint doesn't exist).
- `stores/program.store.ts`: *(no edits in this task; was already modified by
  the user's in-progress work).*

**Views (list of per-file deltas is in C; a quick summary):**

- Scheduling: `SessionFormView` (reschedule payload), `CalendarView`
  (corporationId), `AttendanceDashboardView` (corporationId),
  `RecurringScheduleListView` (window picker + bulk_generate permission),
  `SessionDetailView` (granular permissions + rescheduled branch),
  `MakeupRequestListView` (requested/manage + inline create modal +
  pruned columns), `SchedulingDashboardView` (requested filter + no missing
  fields).
- Students: `StudentDetailView` (case-note permissions, dev-profile permission,
  includeConfidential, end-campus action + modal, development report
  edit + modal).
- Guardians: `GuardianDetailView` (studentId instead of linkId, student name
  display, click-through to student detail).
- Enrollment: `EnrollmentDetailView` (edit/end student_program UI),
  `EnrollmentListView` (surface silent assign failure).
- Assessment: `SessionDetailView` (hydrate on load, performedAt, IDs instead of
  phantom names), `SessionListView` (templateName column + templateVersion),
  `TemplateDetailView` + `TemplateListView` (typeCode/categoryCode),
  `AssessmentDashboardView` (templateName + typeCode).
- BEP: `EducationPlanDetailView` (status alignment, guardian_visible permission,
  reorder/add guards), `EducationPlanListView` (status alignment).
- Leave: `LeaveFormView` (educator picker + lowercase unit),
  `LeaveDetailView` (lowercase + reject permission + unit label),
  `LeaveListView` / `LeaveDashboardView` / `LeaveCalendarView` (lowercase
  status).
- Educators: `EducatorDetailView` (lowercase ref codes).
- Finance: `FinanceDashboardView` / `PaymentListView` / `PaymentDetailView`
  (permission codes).

**Router + i18n**

- `router/index.ts`: Finance permission codes; new `/education-plans/*` →
  `/bep/*` redirect aliases.
- `i18n/locales/tr.ts`: `bep.status` now uses `in_review` + adds `revised`;
  `scheduling.makeup.status.pending` → `requested`, added `createTitle` + `note`;
  `assessment.session` added `leadSubject` / `studentSubject`; `enrollment` added
  `assignFailedAfterCreate`, `editProgram`, `endProgram`,
  `programChangeStatus`.

### D.3 Preserved pre-existing user changes

- `frontend/aynesil-web/src/views/programs/EnrollmentListView.vue` (new),
  `.../EnrollmentDetailView.vue` (new), `router/index.ts` (new enrollment
  routes), `stores/program.store.ts` (branch on `enrollmentId` in assign),
  `views/students/StudentDetailView.vue` (programs tab).
- `tsconfig.app.json`, `tsconfig.json`, `tsconfig.node.json`,
  `package.json`, `vite-env.d.ts`, `.vscode/settings.json`: user-side changes
  not modified.

---

## E. End-to-end coverage matrix (core clinical workflow)

Columns: **DB** (schema), **BE** (backend commands/queries), **API** (controller
+ permission wiring), **CL** (API client), **PS** (Pinia store), **UI** (Vue
view), **RT** (route + menu), **PM** (permission alignment), **T** (automated
tests). Legend: **P**=pass, **W**=warn (functional but caveats), **F**=fail,
**B**=blocked.

| Workflow | DB | BE | API | CL | PS | UI | RT | PM | T |
|----------|----|----|-----|----|----|----|----|----|----|
| Student lifecycle (list/create/edit/status/transfer/end-campus) | P | P | P | P | P | P | P | P | B |
| Guardian CRUD + link + portal access | P | P | P | P | P | P | P | P | B |
| Case notes incl. confidential | P | P | P | P | P | P | P | P | B |
| Student programs (assign + status/end) | P | P | P | P | P | P | P | P | B |
| Enrollment lifecycle (create/change-status/end) | P | P | P | P | P | P | P | P | B |
| Session schedule / reschedule / complete / cancel | P | P | P | P | P | W¹ | P | P | B |
| Session attendance | P | P | P | P | P | P | P | P | B |
| Session makeup request (create/approve/reject) | P | P | P | P | P | P | P | P | B |
| Recurring schedule + bulk-generate | P | P | P | P | P | P | P | P | B |
| Calendar views | P | P | P | P | P | P | P | P | B |
| Assessment session (create/start/respond/complete) | P | W² | P | P | P | P | P | P | B |
| Assessment report finalize | P | P | P | P | P | P | P | P | B |
| Student goal create/update/progress | P | P | P | P | P | W³ | P | P | B |
| Education plan (draft→in_review→approved→active→revised/closed) | P | P | P | P | P | P | P | P | B |
| Education plan add/remove/reorder goals | P | P | P | P | P | P | P | P | B |
| Education plan review/approval/revision | P | P | P | P | P | P | P | P | B |
| Educator CRUD + specialties + certifications | P | P | P | P | P | P | P | P | B |
| Leave request submit/approve/reject/cancel | P | P | P | P | P | P | P | P | B |
| KPI compute + dashboards | P | P | P | P | P | W⁴ | P | P | B |

Footnotes:

1. **W¹ Session UI**: Creating a session still cannot collect participants /
   educators from the form (F-102 BLOCKED). After create, the user adds them on
   the detail view.
2. **W² Assessment backend**: Responses can still be edited post-completion by
   the domain layer (F-100 BLOCKED). API-only guard remains in place.
3. **W³ Student goal UI**: Add-from-template still requires typing the template
   id (BLOCKED — picker UI).
4. **W⁴ KPI UI**: Define/edit KPI UI is still missing (BLOCKED — new view).
   Compute itself is now callable from code via the fixed service, but no view
   invokes it with a corporationId yet.

---

## F. Security + authorization status

### F.1 RBAC

**Status: GREEN for core clinical workflow after fixes.** The remaining P2
menu-vs-router mismatches (V8 CRM/educators root rows) are listed as
recommendations because they require a new menu migration.

- Every controller action in the core modules carries `[HasPermission(...)]`.
- Confidential case-notes gate is now effective (F-010).
- Granular session permissions used in `SessionDetailView` (F-023).
- Leave reject permission corrected (F-033).

### F.2 Tenant isolation

**Status: YELLOW.**

- **Primary enforcement:** PostgreSQL RLS on every `corporation_id` table via
  `tenant_isolation` policy and `app.current_corporation_id` GUC
  (`db/99_triggers_rls_policies.sql`). JWT → GUC via
  `TenantConnectionInterceptor`. Cross-tenant SELECT/INSERT/UPDATE/DELETE are
  rejected by the DB when the GUC is set correctly (the production app
  connection uses the RLS-bound role).
- **App-layer gap:** Many commands accept a client-supplied `corporationId`
  without comparing it to `ITenantContext.CorporationId`. Reported as F-105
  BLOCKED — low risk (RLS backstop) but a worthwhile defense-in-depth pass.

### F.3 ABAC / Care Team

**Status: Architectural drift — reported, not changed.**

- `ARCHITECTURE.md` L100–112 explicitly defers ABAC/Care-Team authorization;
  new code "must not implement ABAC/care-team logic now."
- Repository state contradicts this: `CareTeamController`, `CareTeamFilter`,
  `app.care_team_bypass` GUC, and RESTRICTIVE RLS in
  `db/layer2_sped/15_care_team_rls.sql` are implemented and live.
- `GetCaseNotesQueryHandler` etc. use `CareTeamFilter.ApplyAsync` pre-filter;
  `TenantConnectionInterceptor` reads `care_team:bypass` from JWT.
- **Required decision:** Either promote Care Team / ABAC to a first-class
  design item and update `ARCHITECTURE.md`, or roll back the enforcement. This
  is listed in H.

### F.4 Other security notes

- `assessment_response` immutability (F-100), `assessment_section` /
  `assessment_item` tenant columns (F-101), and plan-content snapshot on
  approve (F-104) are all BLOCKED because they need DDL or product decisions.
- No controller was found that accepts a client `corporationId` and bypasses
  RLS at runtime. The `GetCorporationsQuery` (platform-admin view) is
  intentionally cross-tenant and gated by `corporation:read`.
- Soft-delete convention is honoured for business entities. Exceptions:
  `goal_library` (hard-delete, OK — library catalog), `program_service`
  (hard-delete, reported as a reference-data safety concern), menu/role
  configuration tables (hard-delete, intentional).

---

## G. Test results

### G.1 Commands actually run

The sandbox cannot reliably run `dotnet build` / `dotnet restore` (per
`.cursorrules`), and `npm run build` was not required by the user. The
following static checks were executed and all passed:

| Check | Scope | Result |
|-------|-------|--------|
| `ReadLints` (TypeScript / Vue) | Every changed `.vue` / `.ts` file | **PASS** |
| Repository-wide search for leftover phantom field references | `missedSessionTitle`, `pending_review`, old `student:write` case-note gates, `payment:*` router metas, etc. | **PASS** (zero remaining matches) |

### G.2 Not run (user action required)

| Check | Why not run |
|-------|-------------|
| `dotnet build Aynesil.Api` | Sandbox-forbidden per `.cursorrules`. Please run locally. |
| `dotnet build Aynesil.Application` | Same. |
| `dotnet test` | Same. |
| `npm run type-check` / `vue-tsc` | Not requested; `ReadLints` already catches TypeScript errors in the touched files. |
| `npm run build` | Not requested. |
| End-to-end smoke test | No E2E harness present in the repo. |

### G.3 Suggested local verification steps

1. `dotnet build` of `src/Aynesil.Api` and `src/Aynesil.Application` — covers
   the four backend files touched.
2. Open the Enrollment detail view, assign a program, change its status, mark
   it complete, remove it. All four backend endpoints are now exercised from
   the UI.
3. Submit a leave request as a non-educator user: the educator picker now
   appears and validates. On an existing request, approve/reject and verify
   the detail view refetches with the new approvals list.
4. Submit an education plan for review and confirm the Approve/Reject
   buttons now appear (previously hidden by the `pending_review` typo).
5. Open an in-progress assessment session detail: the Scoring tab should show
   the previously saved answers without clicking "Start".
6. Open the Scheduling dashboard. The pending-makeup list should populate
   (status `requested` server-side).
7. From a recurring schedule row, click Generate: a date-window modal appears
   and the request now includes a body.

---

## H. Decisions requiring your approval

| ID | Decision | Current state | Options |
|----|----------|---------------|---------|
| D-01 | **ABAC / Care Team posture** | `ARCHITECTURE.md` says deferred; production code implements it (controllers, filters, RLS, GUC). | (a) Promote and update `ARCHITECTURE.md`; (b) roll back enforcement; (c) keep as-is and amend docs with a "limited" caveat. |
| D-02 | **Assessment response immutability after completion** (F-100) | API blocks edits once completed, but domain `AssessmentResponse.Update` and DB table allow mutation. | (a) Add status check in `AssessmentResponse.Update`; (b) also add DB-level trigger/CHECK; (c) accept API-only enforcement and document. |
| D-03 | **Tenant isolation for assessment template children** (F-101) | `assessment_section`, `assessment_item`, `assessment_template_translation` lack `corporation_id`; generic RLS loop skips them. | DDL migration adding `corporation_id` + explicit policies, or document reliance on parent (and keep RESTRICTIVE policies). |
| D-04 | **Session create UX** (F-102) | Session create form sends no participants/educators; backend accepts them in one request. | Build minimal picker UI (student + educator selectors) and include in the create payload. |
| D-05 | **Reschedule educator conflict re-check** (F-103) | Backend reschedule skips educator conflict re-validation. | Add `HasEducatorConflictAsync` in `RescheduleSessionCommandHandler`. |
| D-06 | **BEP approval snapshot** (F-104) | Plan content is live-linked to goals even after approval. | (a) Snapshot plan goals on approve into `education_plan_goal.snapshot_jsonb` (new column); (b) lock `UpdateStudentGoal` when the goal belongs to a plan with status ∉ {draft, in_review}; (c) hybrid. |
| D-07 | **Program activate/deactivate** | Delete handler message says "deactivate", but no Activate/Deactivate command/endpoint exists. | Add commands + endpoints + UI toggle; permission reuses `program:update`. |
| D-08 | **Reference-data safety for `program_service`** | Hard-deleted; schema has no `deleted_at`. | DDL add `deleted_at` + domain soft-delete support. |
| D-09 | **Menu seed migration** | V8 menu rows for CRM root (`student:read`) and Educators root (`user:read`) misalign with router permissions after later route changes. | New migration to realign; alternatively mark deactivated and add replacements. |
| D-10 | **Education-plans menu base URL** | Menu seed publishes `/education-plans/*`; SPA uses `/bep/*`. We added client-side redirects. | Decide canonical URL and update menu migration accordingly. |
| D-11 | **StudentDetailView new tabs (goals, BEP, assessments, sessions)** | None exist. Users must jump out to standalone modules with manual student id. | Add tabs that reuse the existing stores with the current student id prefilled. Moderate UX work. |
| D-12 | **Student consents on student record** | Standalone `legal/consents` route only. | Add consents tab/section to StudentDetailView. |
| D-13 | **Guardian picker on student record** | Current link flow requires raw guardian UUID. | New reusable picker component. |
| D-14 | **"Get me as an educator"** | Leave form now requires an explicit educator pick, which is wrong for an educator submitting on their own behalf. | Add `GET /api/educators/me` (resolve educator by `user_id`) and prefill the picker. |
| D-15 | **Application-layer tenant guard** (F-105) | Many create commands accept client-side `corporationId` without a JWT comparison. | Add a shared helper that validates `req.CorporationId == _tenant.CorporationId` and apply across command handlers. |

---

## I. Remaining work list

### I.1 Completed in this task

See **D. Change log**. 35 cross-layer findings fixed; 6 P0 items + many P2/P3
items reported as BLOCKED or recommendations.

### I.2 Open — within existing architecture (safe follow-ups)

- Program activate/deactivate (D-07).
- Education-plans menu migration (D-09, D-10).
- `GET /api/educators/me` + prefill leave form (D-14).
- KPI definition create/edit view.
- POST `/leave/balances` initialize in client.
- `ConflictCheckDto` endpoint for pre-flight scheduling validation.
- Student detail tabs for goals/BEP/assessments/sessions (D-11).
- Student consents section on student record (D-12).
- Guardian picker component (D-13).
- Application-layer tenant guard helper applied across commands (D-15).

### I.3 Open — requires schema or product decision

- Response immutability (D-02), child-table corporation_id (D-03).
- Session create pickers (D-04) + reschedule educator conflict (D-05).
- BEP approval snapshot (D-06).
- `program_service` soft-delete (D-08).
- ABAC/Care-Team architectural reconciliation (D-01).

### I.4 Out of scope for this chat but on the roadmap

- Deep audit of non-core modules: Consultancy, Camps, Cameras, Corporations &
  Users admin, Notifications & templates, Legal (contracts/consents), Portal,
  Surveys, Settings, Reference Data admin, Interviews, Leads activities,
  Meetings, Refunds, Finance Reports.
- Automated test coverage for the core workflows (none exist today).
- Observability: add correlation ids / structured errors for the new paths.
- OrganizationGroup migration (deferred per `ARCHITECTURE.md`).
- SaaS licensing / `menu_item.feature_flag` wiring (deferred).

---

*This report describes work actually performed. Items marked BLOCKED were
deliberately not changed and are listed with the decision required to move
them forward. The approved database schema (`/db`) was not modified and no
EF Core migrations were generated.*
