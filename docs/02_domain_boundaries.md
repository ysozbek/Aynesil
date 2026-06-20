# 02 — Domain Boundaries

The platform is split into two **layers** and a set of **bounded contexts** (DDD-style). Each bounded context owns its tables, has a clear responsibility, and communicates with others through well-defined references (FKs) or events (the outbox).

## Layering principle

- **Layer 1 — Generic Enterprise Platform**: domain-agnostic capabilities reusable by *any* organization (not just schools). It must contain **no special-education concepts**.
- **Layer 2 — Special Education & Therapy**: the Akran Hareket domain, built strictly *on top of* Layer 1 primitives (tenancy, reference data, users, files, notifications, audit).

The dependency rule is one-directional: **Layer 2 depends on Layer 1; Layer 1 never depends on Layer 2.**

---

## Layer 1 bounded contexts

| Context | Schema | Responsibility | Must NOT contain |
|---|---|---|---|
| Tenancy & Organization | `core` | Corporations, campuses, tenant context | Domain entities |
| Identity & Access | `iam` | Authentication, external IdP, RBAC, dynamic menus | Business roles hardcoded |
| Reference Data & Localization | `ref` | Configurable lists, locales, translations engine | Business-specific value lists in code |
| Platform Services | `core` | Settings, files, audit/activity, notifications, reporting, KPI, integration | Therapy/education logic |

**Boundary rules**
- Anything a business user might rename/extend (a "type", "status", "category", "method", "stage") belongs to **Reference Data**, never an enum in code.
- All cross-cutting capabilities (files, notifications, audit, settings, reporting) are generic and polymorphic so Layer 2 entities plug in without Layer 1 changes.

---

## Layer 2 bounded contexts

| Context | Schema | Owns | Key dependencies (Layer 1 + L2) |
|---|---|---|---|
| CRM & Admissions | `crm` | Leads, activities, interviews, conversion | `ref`, `iam`, `students` |
| Assessment | `assessment` | Templates, sessions, scoring, recommendations | `crm`, `students`, `educators` |
| Student Records | `students` | Students, guardians, diagnoses, case mgmt, portal access | `core`, `iam`, `ref` |
| Educator Mgmt | `educators` | Educators, specialties, certifications, hierarchy | `core`, `iam`, `ref` |
| Education | `education` | Programs, enrollment, goals, BEP/IEP, academic periods | `students`, `educators`, `ref` |
| Scheduling | `scheduling` | Sessions, rooms, recurrence, attendance, make-up, calendar | `education`, `students`, `educators` |
| Finance | `finance` | Packages, credits, payments, invoicing, discounts | `students`, `education`, `scheduling` |
| Legal | `legal` | Contracts & consent (versioned) | `students`, `core` (files) |
| Media | `media` | Cameras, live viewing, viewing logs | `scheduling`, `students`, `legal` |
| Operations | `ops` | Meetings, educator leave, performance/KPI | `educators`, `students`, `scheduling` |
| Camps | `camps` | Camp definitions, periods, enrollment, attendance | `students`, `finance`, `ref` |
| Consultancy | `consultancy` | Institutions, plans, visits, observations, reports | `educators`, `core` (files) |
| Parent Portal | *(cross-cutting)* | Guardian visibility + read projections | `students`, `scheduling`, `finance`, `education` |

---

## Cross-context interaction rules

1. **References are downward or sideways within Layer 2**, never upward into a higher-level orchestrator. Where a strict FK would create a cycle (e.g. `student.lead_id` ↔ `lead.converted_student_id`), the *weaker* direction is modeled as a soft reference (no FK) and documented.
2. **No cross-context writes by reaching into another schema's tables** at the application layer — go through that context's own service. (The DB allows it; the architecture forbids it.)
3. **Eventing**: asynchronous, cross-context side effects (e.g. "enrollment created" → send welcome notification) use `core.outbox_event` rather than direct coupling.
4. **Shared vocabulary** (statuses/types/categories) is *always* `ref.ref_value`, so contexts agree on configurable lists without compile-time coupling.

---

## Why these boundaries

- **Reusability**: Layer 1 can be lifted into a completely different vertical (clinics, tutoring centers, fitness) with zero changes.
- **Team scalability**: each schema can be owned by a different squad; PostgreSQL schemas make ownership and grants explicit.
- **Blast-radius control**: a change to Finance cannot structurally break Clinical records; cross-context contracts are narrow (a handful of FKs + events).
- **Compliance isolation**: the most sensitive data (clinical, media) is concentrated in `students`, `media`, `legal`, letting us apply stricter audit/encryption/retention policies to a small surface.
