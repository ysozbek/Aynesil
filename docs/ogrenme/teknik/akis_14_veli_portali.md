# Akış 14 (teknik) — Veli portalı

**Menü:** `portal` grubu (`portal:access`); V49 ile alt öğeler `portal-home` (`/portal`), `portal-children` (`/portal/children`), `portal-notifications` (`/portal/notifications`). Sidebar'da alt öğesi olan üst öğe yalnızca başlık olduğundan `/portal` kendi alt öğesiyle durur. **Router:** `/portal`, `portal/children`, `portal/children/:studentId[/sessions|/goals|/packages|/documents]` (hepsi `ChildDetailView`), `portal/notifications`.

## Sekme koşulları (`views/portal/ChildDetailView.vue`)
`canViewSessions` → Seanslar · `canViewPlan` → Hedefler · `canViewFinance` → Paketler · `canViewReports` → Belgeler · Toplantılar koşulsuz. `canViewAttendance` ve `canViewCamera` için sekme yok (kamera akışı: `viewing_authorization`).
Not: `GET api/portal/students/{id}/development-reports`, `/attendance`, `/bep` uçları var; ekranlardan hangisini çağırdığı [doğrulanacak] (`parentPortal.store.ts` `fetchDevelopmentReports` tanımlı).

## Uçlar (`PortalController`, `api/portal`, `portal:access`)
`my-students`, `students/{id}` (özet), `…/dashboard`, `…/development-reports`, `…/sessions`, `…/attendance`, `…/packages`, `…/documents`, `…/bep`, `…/goal-progress`, `…/meetings`, `notifications`.

## Portal hesabı (`students.guardian.user_id`, V48)
"Erişim Aç" (`POST api/guardians/{id}/students/{sid}/portal-access`) yalnızca `guardian_portal_access` satırı + `student_guardian.portal_access` yazar; **kullanıcı hesabı açmaz** (doğrulandı). Hesap ayrı uçlarla:
`GET|PUT|DELETE|POST api/guardians/{id}/user-account`, `GET api/guardians/by-user/{userId}` — eğitmendeki desenin aynısı (Akış 15 teknik), izin `guardian:manage_portal` + `user:update` (+ `user:create`). Ek kural: yeni hesabın rolü `portal:access` içermeli (422 `Rule.RoleLacksPermission`). Bir kullanıcı ≤ 1 veli (`uq_guardian_user`, 409 `Rule.UserAlreadyLinkedToGuardian`).
**Geçici şifre / davet kararı:** mevcut auth akışında davet token'ı yok; parola sıfırlama token'ı e-posta ister ve sağlayıcı bağlı değil (`LoggingNotificationService`). Bu yüzden hesap `active` açılır, CSPRNG ile 14 karakterlik (benzer karakterler hariç, her sınıftan en az bir) geçici şifre üretilir, yalnızca hash saklanır, düz metin yanıtta bir kez döner ve loglanmaz. Veli `POST api/auth/change-password` ile değiştirir (frontend `/account/password`; başarıda tüm oturumlar kapanır). **İlk girişte zorunlu değiştirme (V50):** `iam.user_account.must_change_password` (yönetici/API ile açılan hesaplarda `true`); JWT'ye `mcp` claim'i konur, `PasswordChangeRequiredMiddleware` bayrak açıkken `change-password`/`logout`/`refresh` dışındaki her uca 403 `Auth.PasswordChangeRequired` döner, arayüz yalnızca `/account/password`'i açar. `UserAccount.ChangePassword` (kendi isteğiyle ya da sıfırlama bağlantısıyla) bayrağı kapatır. E-posta sağlayıcısı gelince davet/sıfırlama bağlantısına geçilebilir.
Her kuruma `guardian` rolü (Veli (portal): `portal:access`, `parent_feedback:create`, `survey:respond`, `notification:read`): mevcut kurumlar V48, yenileri `devops/new_tenant.py`.
Test: `UserAccountLinkTests.A_guardian_account_opened_from_the_record_logs_in_to_the_portal` (geçici şifreyle giriş → `my-students` öğrenciyi döner; `/api/students` 403).

## Kurallar
- `PortalAccessHelper.VerifyAsync`: `guardian.user_id = giriş yapan` ∧ `guardian_portal_access.student_id` ∧ `revoked_at IS NULL`; aksi `ForbiddenAccessException(Rule.GuardianNoPortalAccess)` (ekran: `portal.noAccess` "Bu öğrenciye erişim yetkiniz bulunmuyor.").
- BEP: `education_plan.guardian_visible = true` filtresi. Gelişim raporu: yalnızca `IsFinalized`; erişim `CanViewReports` bayrağına bağlı, personel bakım ekibi süzgecine değil (kod yorumu; commit `2d08625`).
- Toplantılar: `meeting_participant.guardian_id` = giriş yapan velinin kaydı.
- Demo rolü `guardian`: `portal:access`, `parent_feedback:(submit|create)`, `survey:respond`, `notification:read`. Demo-seed veli ↔ kullanıcı bağını `PUT api/guardians/{id}/user-account` ile kurar.

## Dosyalar
`views/portal/*` (+`tabs/Child{Sessions,Goals,Packages,Documents,Meetings}Tab.vue`), `components/shared/UserAccountLinkCard.vue`, `views/account/ChangePasswordView.vue`, `Features/Students/Commands/GuardianUserAccountCommands.cs`, `stores/parentPortal.store.ts`, `services/portal.service.ts`, `PortalController.cs`, `Application/Features/Portal/Queries/PortalExtendedQueries.cs`, tablolar: `students.guardian`, `guardian_portal_access`, `student_guardian`.
