# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

AyNesil is a multi-tenant SaaS platform for special-education / therapy centers. It is a .NET 9 modular monolith (Clean Architecture + CQRS) on PostgreSQL 17, with a Vue 3 frontend. Much of the docs and comments are in Turkish.

## Authoritative sources (read before designing anything)

- `db/` (`db/layer1_core/`, `db/layer2_sped/`, `db/99_triggers_rls_policies.sql`, `db/seed/`, `db/migrations/`) is the **authoritative schema**. Never invent tables, columns, FKs or relationships. If a needed structure is missing: stop, explain what's missing, and wait for the user's decision.
- `ARCHITECTURE.md` (summary) and `docs/01`…`08` (detail) are authoritative for architecture. Do not modify `ARCHITECTURE.md` on your own initiative.
- Other docs live under `docs/` grouped by audience (index: `docs/README.md`): `docs/musteri/` (user guide, demo script, onboarding roadmap), `docs/calisma/` (work-in-progress lists, decision records), `docs/raporlar/` (dated audit reports). `docs/01`…`08` and `ABAC_CARE_TEAM_DESIGN.md` stay at the `docs/` root because applied migrations reference them by path.
- `.cursorrules` holds the full project rule set; the key points are below.

## Build / run / verify

**Do not run `dotnet build`, `dotnet run`, `dotnet restore`, `npm run build` or `npm run dev` yourself to verify changes** unless the user explicitly asks. NuGet restore is unreliable in the sandbox. Instead, state clearly that the build was not verified and list which projects/files need building. When the user reports a build error, treat the exact message as ground truth.

Commands the user runs:

```bash
# Full stack (db, redis, flyway migrations, api, web)
docker compose up --build            # or: python3 devops/run_local.py
docker compose run aynesil-flyway    # apply new db/migrations/V*.sql

# Backend
dotnet build Aynesil.sln
dotnet run --project src/Aynesil.Api          # http://localhost:5000
dotnet test tests/Aynesil.Tests                # xUnit + FluentAssertions + Moq + Testcontainers
dotnet test tests/Aynesil.Tests --filter "FullyQualifiedName~SomeTest"
dotnet test tests/Aynesil.IntegrationTests        # Docker: PG17 + real Flyway; [Trait("Category","Integration")]
dotnet test Aynesil.sln --filter "Category!=Integration"   # everything except Docker tests

# Frontend (frontend/aynesil-web)
npm run dev          # Vite on :5173, proxies /api → VITE_API_BASE_URL (default http://localhost:5000)
npm run type-check   # vue-tsc --noEmit
npm run build

# DB schema smoke test (PG17 green build)
db/validation/smoke_test.sh
```

`devops/*.py` wrap build/publish/migrate (`migrate.py --local | --incremental | --seed-only | --skip-seed`).

New customer (tenant) onboarding: `python3 devops/new_tenant.py --check --code <code> --name "<name>" --admin-username <email> --admin-name "<name>"` (drop `--check` to commit; `--dry-run` prints SQL). Login resolves users by username alone (`iam.authenticate_user`), so `user_account.username` is **globally unique** across corporations (V36, `uq_user_account_username`). User-creation handlers check it via the SECURITY DEFINER `iam.username_exists()` (`Features/Users/UsernameAvailability.cs`), since RLS hides other tenants' rows. Onboarding phases: `docs/musteri/KURULUM_YOL_HARITASI.md`.

**İlk girişte şifre değiştirme (V50):** yönetici/API ile açılan hesaplar (`/users`, `auth/register`, eğitmen/veli hesabı, `new_tenant.py` yöneticisi) `iam.user_account.must_change_password = true` başlar. Girişte JWT'ye `mcp` claim'i konur; `PasswordChangeRequiredMiddleware` bayrak açıkken yalnızca `auth/change-password`, `auth/logout`, `auth/refresh` uçlarına izin verir (diğerleri 403 `Auth.PasswordChangeRequired`); arayüz yalnızca `/account/password`'i açar. `UserAccount.ChangePassword` (parola değiştirme ya da sıfırlama) bayrağı kapatır. Yeni hesap oluşturan kodlar `UserAccount.Create(..., mustChangePassword: true)` geçirmelidir. Demo seed demo kullanıcılarında bayrağı kapatır (`ilkgiris@…` örneği hariç). Şifremi unuttum ekranları var; e-posta sağlayıcısı bağlı olmadığı için bağlantı gitmez.

Ekranı olmayan endpoint'lerin güncel listesi ve gerekçeleri: `docs/ekransiz-endpointler.md`.

Educator/guardian login accounts are linked via `/api/educators/{id}/user-account` and `/api/guardians/{id}/user-account` (link existing / unlink / create with a one-time temporary password; `Common/Identity/UserAccountLinks.cs`). A user backs at most one educator and one guardian (V48 `uq_educator_user`, `uq_guardian_user`). `ActingEducator` (session notes, BEP approval) and the parent portal resolve the person from this link. New parent accounts need a role with `portal:access`; every tenant gets a `guardian` role (V48, `new_tenant.py`).

## Architecture

### Layers (src/)
- **Aynesil.Domain** — persistence-ignorant entities under `Modules/<BoundedContext>/`, all inheriting `BaseEntity` (audit fields + soft delete). Domain events in `Events/`.
- **Aynesil.Application** — `Features/<Module>/{Commands,Queries,Dtos}` with MediatR handlers and FluentValidation. Pipeline behaviors in `Common/Behaviors` (Validation, Logging, Performance).
- **Aynesil.Infrastructure** — `Persistence/AynesilDbContext` with EF Core Fluent configurations (`Persistence/Configurations`, no DataAnnotations), interceptors, and a few repositories.
- **Aynesil.Api** — thin controllers inheriting `BaseController` that dispatch to MediatR; every response is `ApiResponse<T>` (`Aynesil.Shared`), lists use `PaginatedResult<T>`. Global `ExceptionMiddleware`.
- **Aynesil.Shared** — `ApiResponse`, pagination, `Constants/Permissions.cs` and `CacheKeys.cs`.

**Persistence access:** handlers use `IAppDbContext` directly. Only add a repository for reusable, complex query logic that doesn't fit in one handler (e.g. `ISessionRepository` conflict detection, `IMenuRepository` recursive tree). Don't write repositories for simple CRUD.

### Bounded contexts = PostgreSQL schemas
- Layer 1 (domain-agnostic): `core`, `iam`, `ref`
- Layer 2 (special-ed domain): `crm`, `students`, `assessment`, `educators`, `education`, `scheduling`, `finance`, `legal`, `media`, `ops`, `camps`, `consultancy`, plus portal/care-team/surveys
- Dependencies go one way only: **Layer 2 → Layer 1**.

### Multi-tenancy and RLS
- Shared schema with `corporation_id` on business rows, enforced by **default-deny RLS**. Campus is an authorization sub-scope inside a corporation, not a tenant.
- The API connects as the least-privilege `aynesil_app` role. `TenantConnectionInterceptor` sets the GUCs `app.current_corporation_id`, `app.current_user_id` and (transaction-local, derived only from JWT `perm` claims) `app.care_team_bypass` on every connection. With no tenant, queries return zero rows.
- The owner role (`aynesil_owner`) bypasses RLS and is used only by Flyway/seed.
- Auditing is done by DB triggers (`core.audit_trigger` → `core.audit_log`), alongside `AuditSaveChangesInterceptor`. Rows have an optimistic `row_version`. PKs are UUID v7.

### Authorization
Permission-based RBAC: `[HasPermission(Permissions.X.Y)]` with a dynamic policy provider (`Api/Authorization`). Never check role names. On the frontend, use `composables/usePermission.ts`. Menus are DB-driven (`iam.menu_item` + translations, `required_permission`, `feature_flag`).

### Database changes
- Schema changes go in Flyway migrations `db/migrations/V{N}__description.sql`, using the next sequential number (latest is V52, so the next is V53). Requires PostgreSQL 15+ (target 17). `afterMigrate__app_role_grants.sql` re-grants the app role. Migrations must be idempotent (`IF NOT EXISTS`, `ON CONFLICT DO NOTHING`). New tables need RLS policies and grants to `aynesil_app`.
- A new module typically ships a `V{N}__<module>_permissions_and_menu.sql` that seeds `iam` permissions, `menu_item` rows and their `tr` translations. Keep it in sync with `Shared/Constants/Permissions.cs` and frontend routes.
- The layered DDL files (`db/layer*/`) are also kept in sync when a migration changes a table. Migrations are the source of truth; `bash db/validation/smoke_test.sh` builds both and fails on any drift (`db/validation/drift.sql`, incl. RLS/policies) or on a tenant table without RLS + `tenant_isolation`. It also fails on ANY table (partitions included) that has RLS disabled while `aynesil_app` can access it, unless listed with a reason in `rls_off_exceptions` (global catalogs, outbox, Hangfire). Child tables without `corporation_id` get `parent_read`/`parent_write` policies bound to the parent row (V44); handlers check the parent via `TenantOwnership.EnsureWritable`. A table created by a migration must enable RLS and add its policy itself — `99_triggers_rls_policies.sql` only runs in the V1 baseline.
- **Tenant integrity (#86, V38–V41, `db/98_tenant_integrity.sql`):** an FK between two tenant tables must be composite — `FOREIGN KEY (corporation_id, x) REFERENCES parent (corporation_id, id)` — and the parent needs a unique `(corporation_id, id)` index (FK checks bypass RLS). References to tables that also hold global rows (`ref.ref_value`, templates, definitions) get a `core.enforce_global_or_tenant_ref` trigger instead. New tables/FKs follow the same rule. Manual down scripts live in `db/rollback/`.

### Frontend (frontend/aynesil-web)
Vue 3 Composition API + Pinia + Vue Router + vue-i18n + Metronic 9 (Tailwind 4, `@keenthemes/ktui`). Per-module layering: `services/*.service.ts` (Axios via `api.service.ts`), `stores/*.store.ts`, `types/*.types.ts`, `views/<module>/`. Shared UI is in `components/shared` (DataTable, FormModal, ConfirmModal). All display text goes through i18n (`i18n/locales/tr.ts`). `@` aliases `src/`.

## Domain rules that shape code

- **Reference data, not enums:** any classification that may change (session/therapy/program/assessment types, statuses, leave types, payment methods, etc.) uses the `ref.ref_type`/`ref_value` engine, with translation and tenant overrides. Reference values are never physically deleted, only deactivated.
- **Localization:** per-entity `*_translation` sidecar tables plus `ref.i18n_message`. No hardcoded user-facing text.
- **Financial records are immutable:** payments, invoices, credit usage and refunds are never deleted. Corrections use reversal/adjustment records. Balances derive from the append-only `credit_ledger`.
- **Signed contracts and consents are immutable and versioned.** Sensitive access (e.g. cameras) is gated by `student_consent` plus audit.
- **Scheduling conflicts** are prevented in the DB (`EXCLUDE USING gist` for rooms/leave, a trigger for educator double-booking).
- Workflow state transitions must be validated, and invalid transitions rejected.
- Files are stored as metadata records/references (local storage now, S3 later).
- Naming: "AyNesil" only in UI/marketing text. Use "Aynesil" in all code, namespaces, config and DB identifiers (`aynesil`, `AYNESIL_` env prefix). "Akran Hareket" is a tenant row, never in code.

## Deliberately deferred (don't implement, don't assume against)

- **OrganizationGroup/holding tier:** don't build it, but don't hardcode "Corporation is the permanent top level".
- **SaaS licensing/subscriptions:** don't build it, but don't assume every tenant has every module. `menu_item.feature_flag` is the seam.
- **ABAC/Care-Team:** partially introduced via migrations V15/V16 (`students.user_can_access_student()`, `app.care_team_bypass`, `Application/Common/CareTeam`). Follow the existing implementation and don't extend it beyond what `db/` defines. Don't assume RBAC is the only permanent mechanism for clinical data.

- **E-posta/SMS sağlayıcısı:** henüz bağlı değil. `INotificationService` için `LoggingNotificationService` kayıtlı: bildirimi göndermez, yalnızca şablon kodu + alıcıyı log'a yazar (token gibi değişken değerleri asla loglanmaz). Gerçek sağlayıcı (SMTP/outbox + Hangfire işçisi) eklendiğinde `Infrastructure/DependencyInjection.cs`'teki kayıt onunla değiştirilir; o zamana kadar davet/parola sıfırlama e-postaları gitmez.
- **DI doğrulaması:** servis kayıtları `AddAynesilApi` içindedir; `ServiceRegistrationTests` ValidateOnBuild + ValidateScopes ile hepsini doğrular (eksik kayıt testte kırılır).

## Dağıtım notları

- **Hız sınırı sayaçları örnek başınadır.** Anonim auth uçlarının sınırı (`Api/RateLimiting/AuthRateLimiter`: giriş 10/dk, sıfırlama isteği 3/saat, sıfırlama onayı 10/dk) süreç belleğinde tutulur. API birden fazla örnekte çalışırsa her örnek kendi sayacını işletir, yani etkin sınır örnek sayısıyla çarpılır. Çok örnekli dağıtımdan önce Redis tabanlı bir sınırlayıcı (ör. `StackExchange.Redis` üzerinde sabit pencere sayacı; Redis zaten bağlı) gerekir. Tek örnekli dağıtımda sorun yok.
- **Ters vekil ve gerçek istemci IP'si.** `UseForwardedHeaders` pipeline'ın en başında, hız sınırından önce çalışır. `X-Forwarded-For` yalnızca `ForwardedHeaders:KnownProxies` / `KnownNetworks`'teki bağlantılardan kabul edilir; bilinmeyen kaynaktan gelen başlık yok sayılır. Development'ta ikisi de boştur (yalnızca loopback güvenilir). Üretimde vekil adresi ya da ağı ortam değişkeniyle verilmelidir: `AYNESIL_ForwardedHeaders__KnownProxies__0=10.0.0.5`, `AYNESIL_ForwardedHeaders__KnownNetworks__0=172.18.0.0/16`. Vekil tanımlanmazsa hız sınırı vekilin IP'sine uygulanır ve tüm istemciler tek kovayı paylaşır.

## Working style (from .cursorrules)

Generate one requested module at a time, layer by layer. After each major layer, stop, summarize the files, and wait for confirmation.
