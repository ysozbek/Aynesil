# Eşleme tablosu — modül / controller / ekran / akış

Hiçbir controller açıkta kalmamalı; aşağıdaki tablo `src/Aynesil.Api/Controllers/` içindeki 36 dosyanın tamamını kapsar.

| Controller (`api/…`) | Frontend ekranı (`views/…`) | Akış |
|---|---|---|
| `LeadsController` (`leads`), `InterviewsController` (`interviews`) | `crm/` | 01 |
| `StudentsController` (`students`) | `students/` | 02 |
| `GuardiansController` (`guardians`, `…/user-account`) | `guardians/`, `components/shared/UserAccountLinkCard.vue` | 02, 14 |
| `LegalController` (`legal`) | `legal/` | 02 |
| `AssessmentTemplatesController`, `AssessmentSessionsController` | `assessment/` | 03 |
| `EducationPlansController` (`education-plans`) | `bep/` | 03 |
| `GoalsController` (`goals`) | `goals/` | 03 |
| `ProgramsController` (`programs`, kayıtlar) | `programs/` | 04 |
| `SchedulingController` (`scheduling`) | `scheduling/` | 04 |
| `PaymentsController` (`payments`) | `finance/` | 05 |
| `CampsController` (`camps`) | `camps/` (kısmi) | 06 |
| `MeetingsController` (`meetings`) | `meetings/` | 07 |
| `LeaveController` (`leave`) | `leave/` | 07 |
| `ConsultancyController` (`consultancy`) | `consultancy/` | 08 |
| `CameraController` (`cameras`) | `cameras/` | 09 |
| `PerformanceKpiController` (`performance-kpi`) | `kpi/`, `DashboardView.vue` | 10 (geri bildirim uçları 11) |
| `SurveysController` (`surveys`) | **yok** | 11 |
| `CareTeamController` | **yok** | 12 |
| `NotificationsController`, `NotificationTemplatesController`, `NotificationTriggersController` | `notifications/` (tetikleyici ekranı yok) | 13 |
| `PortalController` (`portal`) | `portal/` | 14 |
| `EducatorsController` (`educators`, `…/user-account`) | `educators/`, `components/shared/UserAccountLinkCard.vue` | 15 |
| `UsersController`, `RolesController`, `PermissionsController` | `users/`, `roles/`, `permissions/` | 16 |
| `MenusController`, `ReferenceDataController` | `menus/`, `refdata/` | 16 |
| `CampusesController`, `CorporationsController`, `SettingsController` | `campuses/`, `corporations/`, `settings/` | 16 |
| `AuthController` (`auth`: giriş, çıkış, yenileme, e-posta doğrulama, parola sıfırlama/değiştirme, kayıt) | `auth/`, `account/` (şifre değiştirme) | Genel bakış (giriş), 16 (kayıt = kullanıcı davet) |
| `HealthController`, `BaseController` | — (altyapı) | kapsam dışı |

## Frontend'de ekranı olmayan ya da eksik modüller
- **Ekranı hiç yok:** Anketler (11), Bakım ekibi (12).
- **Kısmi:** Kamp (etkinlik, yoklama, rapor, eğitmen atama, tamamlama yok), Hukuk (yalnızca rıza kanıt dosyası ekleme ve silme uçları ekransız), Kamera (izleme/oynatıcı ekranı yok; 404'e giden "Canlı İzle" düğmesi kaldırıldı), Bildirimler (tetikleyici yok), Danışmanlık (gözlem ekleme düğmesi yok), Finans (rapor ekranı yok). Telafi, iade, sözleşme, rıza, şablon, izin bakiyesi, KPI tanımı artık ekranlıdır. Güncel ekransız uç listesi: [ekransiz-endpointler.md](../../ekransiz-endpointler.md).
- **Menü/router uyumu (V49 ile giderildi):** etkin menü öğeleri router'la birebir karşılaştırıldı (parametreli rotalar eşleşme sayılmadan). Sayfası olmayan `camps.activities`, `reports`, `students-care-team` kapatıldı; `students-guardians` `/guardians`'a çevrilip açıldı; `legal.reports`, `notifications-preferences`, `portal-home|children|notifications` eklendi. Önceki listedeki `/camps/definitions|enrollments|attendance|reports`, `/cameras/viewing-logs`, `/scheduling/makeup|recurring`, `/education-plans/*` V31'de düzeltilmiş/kapatılmıştı. Bilerek menüde olmayan sayfalar: `/account/password`, `/403`, `/404`, `/consultancy/follow-ups/open`, `*/new`, `:id` sayfaları.
- **Veli/eğitmen ↔ kullanıcı bağı:** V48 ile API (`…/user-account`, `…/by-user/{userId}`) ve ekran (eğitmen/veli kartında "Kullanıcı hesabı", kullanıcı detayında bağlı kayıt) var (Akış 14, 15, 16).

## Frontend durum etiketleri
`frontend/aynesil-web/src/constants/statuses.ts` ve `i18n/locales/tr.ts` → `statusBadge`.
