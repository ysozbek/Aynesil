# Akış 16 (teknik) — Yönetim

**Menü (V27, platform-admin):** `platform-admin.corporations` (`/corporations`, `corporation:read`), `.campuses` (`/campuses`, `campus:read`), `.users` (`user:read`), `.roles` (`role:read`), `.permissions` (`/permissions`, `role:read`), `.menus` (`menu:read`), `.reference-data` (`/reference-data`, `ref_data:read`); `settings` (`/settings`, `settings:read`).
**Router:** `/settings`, `/corporations[/:id[/settings]]`, `/campuses`, `/users[/:id]`, `/roles[/:id]`, `/permissions`, `/menus`, `/reference-data`, `/account/password` (izinsiz; her oturum sahibi, başlıktaki rozetten).

## Uçlar
- Kullanıcı (`UsersController`): CRUD (`user:create|update|delete`), `activate`, `suspend`, `POST roles`, `DELETE roles/{userRoleId}` (`user:update`)
- Rol (`RolesController`): CRUD, `POST/DELETE {id}/permissions[/{permissionId}]` (`role:assign_permission`); `PermissionsController` yalnızca GET
- Menü (`MenusController`): CRUD, `PUT {id}/translations`, `activate|deactivate` (`menu:manage`); kullanıcının menüsü için GET
- Referans veri (`ReferenceDataController`): `GET {typeCode}`, `POST {typeCode}`, `PUT values/{id}`, `PUT values/{id}/active` (`ref_data:manage`)
- Kampüs (`CampusesController`): CRUD + `activate|deactivate`; Kurum (`CorporationsController`): `POST`, `PUT`, `PUT {id}/settings` (`corporation:*`, V43 platform izinleri); **Ayarlar** (`SettingsController`): yalnızca `GET` (kurum ayar okuma) — yazma ucu yok [doğrulanacak: `SettingsView` ne gösteriyor].

## Kurallar
- `CreateUserCommand`: Username, FullName, Email, Phone, Password (≥8), PreferredLocale, PrimaryCampusId, ActivateImmediately (varsayılan true). Kullanıcı adı globalde benzersiz: `iam.username_exists()` (`Features/Users/UsernameAvailability.cs`, V36 `uq_user_account_username`) → `Rule.UsernameTaken` ("'{0}' kullanıcı adı zaten alınmış.").
- Kendini silme engeli: `DeleteUserCommand` (`_currentUser.UserId == req.UserId`).
- `Rule.SystemRoleNoDelete` ("Sistem rolleri silinemez."), `Rule.PlatformMenuNoDelete` ("Platform varsayılan menü öğeleri silinemez.").
- Referans değerler fiziksel silinmez (yalnızca `active` bayrağı); kodlar snake_case; kurum geçersiz kılma `ref.ref_value_tenant_override`; çeviri `ref_value_translation`.
- Menü: `iam.menu_item(+_translation)`, `required_permission_id`, `feature_flag` (lisans için dikiş yeri). Menü ağacı ve `required_permission` filtresi kullanıcının `perm` claim'lerine göredir.
- Yetkilendirme: `[HasPermission(...)]` + dinamik politika sağlayıcı (`Api/Authorization`); frontend `composables/usePermission.ts`.
- Kullanıcı detayı bağlı kayıtları `GET api/educators/by-user/{id}` (`educator:read`) ve `GET api/guardians/by-user/{id}` (`guardian:read`) ile gösterir — Users özelliği Layer 2'ye bağımlı olmasın diye uçlar ilgili modüllerde. Bağ kurma: Akış 14/15 teknik (`…/user-account`, V48).
- Şifre değiştirme: `POST api/auth/change-password` (frontend `views/account/ChangePasswordView.vue`). `must_change_password` (V50) açıkken bu sayfa dışında gezinilemez; API yalnızca bu uç, `logout` ve `refresh` ile sınırlıdır. Şifremi unuttum: `POST auth/request-password-reset` (hız sınırı 3/saat) ve `auth/confirm-password-reset` (`/forgot-password`, `/reset-password`); e-posta gitmediği için bağlantı üretilmiş olsa bile kullanıcıya ulaşmaz. Başarıda `RevokeAllAsync` → frontend çıkış yapıp `/login?passwordChanged=1`.
- Menü ↔ router hizası V49 (`V49__menu_route_alignment.sql`): kapatılan `camps.activities`, `reports`, `students-care-team`; `students-guardians` → `/guardians`; eklenen `legal.reports`, `notifications-preferences`, `portal-home|children|notifications`.
- Kurum açılışı: `python3 devops/new_tenant.py --check --code … --name … --admin-username … --admin-name …` (`--dry-run`, kurulum yol haritası `docs/musteri/KURULUM_YOL_HARITASI.md`). Script V48'den beri `admin`'e ek olarak `guardian` (Veli (portal)) rolünü de açar.

## Dosyalar
Vue `views/{users,roles,permissions,menus,refdata,campuses,corporations,settings}/*` · Store `user.store.ts`, `role.store.ts`, `permission.store.ts`, `menu.store.ts`, `refdata.store.ts`, `branch.store.ts`, `corporation.store.ts`, `settings.store.ts` · Servis `user|role|permission|menu-admin|campus|corporation.service.ts` · Application `Features/{Users,Roles,Permissions,Menus,RefData,Campuses,Corporations,Settings}/` · DB `iam.user_account`, `role`, `role_permission`, `user_role`, `permission`, `menu_item(+_translation)`, `auth_session`; `core.corporation`, `campus`, `setting_definition`, `setting_value`; `ref.ref_type`, `ref_value`, `ref_value_translation`, `ref_value_tenant_override`.

## [doğrulanacak]
Rol/izin değişikliğinin JWT'ye yansıma zamanı; `SettingsView`'in yazma yapıp yapmadığı; `Kurum Ekle`'nin platform yöneticisi dışında kullanımı.

### Modül görünürlüğü (V53)
`core.corporation.settings.modules` = `{"camps","consultancy","camera","surveys","care_team","kpi"}` → bool. Anahtar yoksa açık; yalnızca platform yazar (`devops/set_modules.py`, `new_tenant.py --enable-modules`), `PUT /api/corporations/{id}/settings` `modules`'ü yok sayar (`CorporationModules.PreserveModules`). `CorporationSettingsDto.Modules` etkin durumu salt okunur verir (`CorporationSettingsView` "Modüller" listesi). Uçlar `[RequireModule]` ile 403 `Error.ModuleDisabled`; menü `iam.menu_item.feature_flag` = modül kodu (V53 doldurur), `GetUserMenuQuery` her istekte eler. V54: `plans-academic` menüsü açıldı. Testler: `ModuleVisibilityTests` (birim + entegrasyon), smoke-api "modül kapalı/açık". Tam açıklama: `docs/ekransiz-endpointler.md` §5.
