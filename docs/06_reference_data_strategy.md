# 06 — Configurable Reference Data Strategy

## Objective

Business users must manage every business list (session types, therapy types, lead sources, diagnosis categories, …) **without developer intervention and without schema changes**. The list of lists is open-ended, so the design treats *categories themselves as data*.

## The engine (4 tables)

| Table | Role |
|---|---|
| `ref.ref_type` | Catalog of **categories**. One row = one business list. Adding a new list is an `INSERT`, never DDL. |
| `ref.ref_value` | The **entries** in each category. Scope via `corporation_id` (NULL = system/global, set = tenant). |
| `ref.ref_value_translation` | Localized labels per value (see doc 05). |
| `ref.ref_value_tenant_override` | Per-tenant overlay to deactivate / reorder / re-default **shared** values without mutating them. |

## Three scopes — one structure

| Scope | How it's expressed | Who manages it | Example |
|---|---|---|---|
| **System reference data** | `is_system = true`, `corporation_id IS NULL` | Platform team only | `notification_channel`, `integration_kind` |
| **Configurable business (global) data** | shipped values, `corporation_id IS NULL`, `is_system = false` | Platform ships defaults; tenants override/extend | default `session_type`, `payment_method` |
| **Tenant-specific reference data** | `corporation_id = <tenant>` | The tenant's business admins | Akran's `hydrotherapy`, `home_visit` |

A tenant sees: **its own values + global values**, with its **overrides** applied. The view `ref.v_effective_ref_value` computes exactly this for `core.current_corporation_id()`.

## Business-user capabilities → mechanism

| Capability | Mechanism | Notes |
|---|---|---|
| Add value | `INSERT ref_value` (tenant scope) | allowed when `ref_type.allows_tenant_values` |
| Edit value | `UPDATE ref_value` / `ref_value_translation` | tenants edit their own rows |
| Deactivate | `is_active = false` (own rows) or `ref_value_tenant_override.is_active = false` (shared rows) | shared rows stay intact for other tenants |
| Reorder | `sort_order` or override `sort_order` | |
| Translate | `ref_value_translation` | per locale |
| Set default | `is_default` (+ override); enforced by partial unique index | exactly one default per (category, scope) |

## Integrity: keeping a controlled, *not* wild, EAV

This is a **controlled vocabulary** pattern, not open EAV. Guardrails:

1. **Category pinning via composite FK.** `ref_value` exposes `UNIQUE (ref_type_id, id)`. Safety-critical business columns add a constant `*_ref_type` column (defaulted via `ref.type_id('<code>')`) and a composite FK to `ref_value(ref_type_id, id)`. Demonstrated on `scheduling.session.session_type_id` — the DB rejects a `therapy_type` placed in a `session_type` slot. Apply this pattern wherever miscategorization would be dangerous (scheduling, finance).
2. **Metadata is validated, not free-for-all.** `ref_type.value_schema` (JSON-Schema) describes the shape of `ref_value.metadata`, validated by the app on write (e.g. a `session_type` requires `default_duration_minutes:int`, `color:string`).
3. **Vocabulary vs. structure boundary.** Reference data holds *labels/types/statuses*. Anything you **join, aggregate, or constrain heavily** stays a real column/table. Rule of thumb: *if it's a word the business renames, it's reference data; if it's a relationship the system reasons about, it's a table.*
4. **System rows are immutable to tenants.** `is_system = true` rows cannot be deleted by tenants; they can only be overridden via `ref_value_tenant_override`.
5. **Defaults are enforced in the DB.** `uq_ref_value_one_default` guarantees ≤ 1 default per category per scope.

## Hierarchy support

`ref_value.parent_value_id` (enabled per category by `ref_type.is_hierarchical`) supports taxonomies: diagnosis categories → subcategories, assessment categories, KPI/report category trees, ordered pipeline stages.

## Adding a brand-new business list (worked example)

Business wants a new list "Transport Mode":

```sql
INSERT INTO ref.ref_type(code, name, allows_tenant_values)
VALUES ('transport_mode', 'Transport Modes', true);
-- then tenants/platform add values + translations via the admin UI.
```

No migration, no deployment, no developer. Any table that needs it stores a `transport_mode_id uuid REFERENCES ref.ref_value(id)` (or composite-pinned for strictness).

## The example lists in the brief — all covered, zero new tables

`session_type, therapy_type, program_type, service_type, goal_category, development_area, assessment_type, assessment_category, meeting_type, leave_type, attendance_reason, missed_reason, payment_method, discount_type, scholarship_type, package_type, lead_source, lead_status, pipeline_stage, notification_type, notification_category, notification_channel, contract_type, consent_type, educator_title, educator_relationship, specialty, certification_type, guardian_relationship, student_status, enrollment_status, diagnosis_category, institution_type, room_type, camp_type, activity_type, academic_term, kpi_category, report_category, integration_kind` — every one is a `ref_type` row in the seed.
