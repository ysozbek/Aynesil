# 03 — Module Boundaries

"Domains" (doc 02) are the conceptual contexts. "Modules" are the *physical* packaging: PostgreSQL schemas, file layout, and the deployment/feature units that map to them. Modules are how boundaries are **enforced**, not just described.

## Physical packaging = PostgreSQL schemas

Each module is a PostgreSQL schema. This gives us:

- **Namespacing** — no table-name collisions, readable qualified names (`scheduling.session`).
- **Grant-based enforcement** — a service role can be granted `USAGE` on only its own schema(s) plus read on `ref`/`core`, making accidental cross-module coupling fail at the DB.
- **Search-path discipline** — application connections set a minimal `search_path`.

```
core   iam   ref          <- Layer 1 (platform)
crm  students  assessment  educators  education  scheduling
finance  legal  media  ops  camps  consultancy   <- Layer 2 (special-ed)
```

## File layout = module manifest

```
db/
  00_extensions_conventions.sql      # extensions, schemas, shared functions/triggers
  layer1_core/
    01_localization.sql              # ref: locale, i18n_message
    02_tenancy.sql                   # core: corporation, campus
    03_reference_data.sql            # ref: ref_type/value/translation/override + helpers
    04_identity_access.sql           # iam: users, IdP, roles, permissions, menus
    05_platform_services.sql         # core: settings, files, audit, notifications, reporting, integration
  layer2_sped/
    01_students.sql … 13_parent_portal.sql
  99_triggers_rls_policies.sql       # cross-cutting wiring (run LAST)
  seed/
    01_reference_data_seed.sql       # locales + ref catalog + default values + tr/en
    02_akran_bootstrap.sql           # example tenant + RBAC + tenant ref values/overrides
docs/                                # this analysis set (outputs 3–10)
```

## Module dependency matrix (compile/DDL order)

| Module | Depends on (must exist first) |
|---|---|
| `core` (functions/schemas) | — |
| `ref` localization | `core` |
| `core` tenancy | `ref.locale` |
| `ref` reference data | `core.corporation` |
| `iam` | `core`, `ref` |
| `core` services | `iam`, `ref` |
| `students` | `core`, `iam`, `ref` |
| `educators` | `core`, `iam`, `ref` |
| `crm` | `students`, `iam`, `ref` |
| `assessment` | `crm`, `students`, `educators` |
| `education` | `students`, `educators`, `ref` |
| `scheduling` | `education`, `students`, `educators`, `ref` |
| `finance` | `students`, `education`, `scheduling`, `core` |
| `legal` | `students`, `core` |
| `media` | `scheduling`, `students`, `legal`, `core` |
| `ops` | `educators`, `students`, `scheduling`, `iam` |
| `camps` | `students`, `finance`, `ref` |
| `consultancy` | `educators`, `core` |
| parent portal | `students`, `scheduling`, `finance`, `education` |

The numeric file prefixes encode exactly this order.

## Module → capability (feature flags)

Modules map cleanly to **feature flags / licensing**. A tenant that does not buy "Camps" or "Camera Viewing" simply has those modules disabled at the application layer (and `menu_item.feature_flag` hides their menus) — no schema divergence between tenants. The schema is the *superset*; tenants light up subsets.

## Enforcement checklist

- [ ] Each application service connects with a role granted only its module schema(s) + read on `ref`/`core`.
- [ ] Cross-module access goes through the owning module's API, not raw tables.
- [ ] New "type/status/category" → `ref.ref_type`, never a new enum or table.
- [ ] New cross-module side effect → `core.outbox_event`, not a synchronous cross-schema write.
