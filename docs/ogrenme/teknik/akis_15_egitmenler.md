# Akış 15 (teknik) — Eğitmenler

**Menü:** `educators` (`/educators`, `educator:read`; V11 `educators-list`, `educators-new`; `educators-hierarchy`/`educators-utilization` V31'de pasif). **Router:** `/educators[/new|/:id|/:id/edit|/:id/availability]`.

## Uçlar (`EducatorsController`, `api/educators`)
CRUD (`educator:create|update|delete`), `activate|deactivate`, `specialties` (POST/DELETE), `campuses` (POST, `PATCH …/end`), `certifications` (POST/PUT/DELETE), `hierarchy` (POST, `PATCH …/end`, DELETE).
`CreateEducatorCommand` alanları: FirstName, LastName, TitleId, Email, Phone, EmploymentType, HireDate, PrimaryCampusId (UserId create'te yok; bağ ayrı uçlarla). Müsaitlik ekranı için ayrı uç görülmedi [doğrulanacak].

## Kullanıcı hesabı bağı (`educators.educator.user_id`, V48)
| Uç | İzin | Not |
|---|---|---|
| `GET {id}/user-account` | `educator:read` | `LinkedUserAccountDto` ya da `null` |
| `GET by-user/{userId}` | `educator:read` | kullanıcı detayı için `LinkedEducatorDto` ya da `null` |
| `PUT {id}/user-account` `{ userId }` | `educator:update` + `user:update` | mevcut kullanıcıyı bağlar; aynı kullanıcıyla tekrar idempotent |
| `DELETE {id}/user-account` | `educator:update` + `user:update` | yalnızca bağ; hesap/roller kalır (`Success.UserAccountUnlinked`) |
| `POST {id}/user-account` `{ username, fullName, email?, phone?, roleId, campusId? }` | + `user:create` | hesap `active`, rol atanır, 14 karakterlik geçici şifre yanıtta bir kez (`CreatedLinkedUserAccountDto`) |

Kurallar: kullanıcı aynı kurumda (`TenantUserGuard` → 404 `Rule.UserNotInCorporation`; composite FK `fk_educator_user_id_tnt` de korur), bir kullanıcı ≤ 1 eğitmen (409 `Rule.UserAlreadyLinkedToEducator`; DB `uq_educator_user` kısmi tekil indeks), bağlı kayıt sessizce başka kullanıcıya geçmez (409 `Rule.RecordAlreadyLinkedToUser`). Oluştur+bağla tek transaction'da (execution strategy; iki `SaveChanges`, EF'te navigation yok).
Bağımlılar: `ActingEducator` (seans notu yazarı, BEP onaylayanı; bağ yoksa 422 `Rule.ActingEducatorRequired`, metin yöneticiye yolu tarif eder), izin talebi, bakım ekibi köprüsü (`ABAC_CARE_TEAM_DESIGN.md`).
Testler: `tests/Aynesil.Tests/TenantScoping/UserAccountLinkTests.cs` (handler kuralları), `tests/Aynesil.IntegrationTests/UserAccountLinkTests.cs` (RLS altında HTTP; bağsız 422 → bağlı eğitmen not yazar).

## Dosyalar
Vue `views/educators/*`, `components/shared/UserAccountLinkCard.vue` · Store `educator.store.ts` · Servis `educator.service.ts`, `userAccountLink.service.ts` · Application `Features/Educators/Commands/{EducatorCommands,SpecialtyAssignmentCommands,EducatorCampusCommands,CertificationCommands,HierarchyCommands,EducatorUserAccountCommands}.cs`, `Common/Identity/UserAccountLinks.cs` · Domain `Domain/Modules/Educators/` · DB `educators.educator`, `educator_specialty`, `educator_campus`, `educator_certification`, `educator_hierarchy`. Demo rolü eğitmen: `educator:read`, `education_plan:approve` (BEP onayı eğitmen kullanıcısıyla). Demo-seed bağı `PUT …/user-account` ile kurar (SQL değil).
