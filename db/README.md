# AyNesil Platform — Database

PostgreSQL 17 schema for a two-layer enterprise SaaS platform:

- **Layer 1 — Generic Enterprise Platform** (`core`, `iam`, `ref`): tenancy, identity/RBAC, configurable reference data, localization, settings, files, audit, notifications, reporting, integration. Reusable by any organization.
- **Layer 2 — Special Education & Therapy** (`crm`, `students`, `assessment`, `educators`, `education`, `scheduling`, `finance`, `legal`, `media`, `ops`, `camps`, `consultancy`): the Akran Hareket domain.

See `../docs/` for the full architecture analysis (ER explanations, boundaries, localization, reference-data strategy, implementation order, risks).

## Key design choices

| Choice | Decision |
|---|---|
| Multi-tenancy | Shared schema, row-level isolation by `corporation_id`, enforced by **RLS** |
| Primary keys | **UUID v7** (`core.uuid_generate_v7()`; native `uuidv7()` on PG18+) |
| Module boundaries | One **PostgreSQL schema per bounded context** |
| Configurable lists | `ref.ref_type` / `ref.ref_value` engine — new lists are data, not DDL |
| Translations | Per-entity `*_translation` tables + `ref.i18n_message` for UI strings |
| Conflict prevention | `btree_gist` `EXCLUDE` constraints (rooms, leave) + overlap trigger (educators) |
| Audit | DB triggers → partitioned `core.audit_log`; soft delete + optimistic `row_version` |

## Run order (fresh database)

```bash
export PGURL="postgres://OWNER@localhost/akran"      # OWNER role bypasses RLS for setup

psql "$PGURL" -v ON_ERROR_STOP=1 -f db/00_extensions_conventions.sql
for f in db/layer1_core/*.sql; do psql "$PGURL" -v ON_ERROR_STOP=1 -f "$f"; done
for f in db/layer2_sped/*.sql; do psql "$PGURL" -v ON_ERROR_STOP=1 -f "$f"; done
psql "$PGURL" -v ON_ERROR_STOP=1 -f db/99_triggers_rls_policies.sql
psql "$PGURL" -v ON_ERROR_STOP=1 -f db/seed/01_reference_data_seed.sql
psql "$PGURL" -v ON_ERROR_STOP=1 -f db/seed/02_akran_bootstrap.sql
```

The numeric file prefixes encode dependency order.

## Tenant context (per request, set by the application)

The application connects as a **least-privilege, non-owner role** and sets the tenant/user context so RLS applies:

```sql
SELECT set_config('app.current_corporation_id', '<corporation-uuid>', false);
SELECT set_config('app.current_user_id',        '<user-uuid>',        false);
```

With no `app.current_corporation_id` set, tenant tables return **zero rows** (default-deny).

## Layout

```
db/
  00_extensions_conventions.sql
  layer1_core/   01_localization → 05_platform_services
  layer2_sped/   01_students → 13_parent_portal
  99_triggers_rls_policies.sql   # run LAST
  seed/          01_reference_data_seed, 02_akran_bootstrap
```

## Notes / TODO before production

- Replace the `DEFAULT` log partitions with monthly partitions (pg_partman) + retention.
- Add field-level encryption for `national_id` and raw clinical text (KVKK).
- Wire migrations (Flyway/Liquibase) using these files as the baseline.
