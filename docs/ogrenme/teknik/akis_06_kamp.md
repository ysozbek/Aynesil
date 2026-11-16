# Akış 6 — Kamp: kamp → dönem → kayıt → etkinlik → katılım → yoklama → tamamlama → rapor

**Ne işe yarar:** Yaz/dönem kampı gibi kısa süreli programları tanımlamak, dönemlere kapasiteli kayıt almak, kapasite dolunca bekleme listesi yönetmek, etkinlik ve devam takibi yapmak.

**Kim yapar:** Koordinatör/yönetici. Demo eğitmen rolü kamp izinlerinden `camp:read`, `camp_attendance:*`, `camp_participation:*` alır (kamp yoklaması ve etkinlik katılımı). Menü seed'inde kamp izni `camp:read` (hangi rolün göreceği bu izne bağlı).

**Menü:** `Kamp Yönetimi` (`camps`, rota `/camps`). V21 seed'i alt öğeleri `camps.definitions/enrollments/attendance/reports` olarak koymuştu; **router'daki gerçek ekranlar** `/camps` (pano), `/camps/list`, `/camps/new`, `/camps/:id`, `/camps/:id/edit`, `/camps/periods/:periodId` (etkinlik/katılım/yoklama), `/camps/periods/:periodId/enrollments` (+ rapor), `/camps/performance` (V49'da menü/router uyumu giderildi; yeni sayfalar menü satırına bağlı değil, dönem satırı ve panodan açılır). Rotaların hepsi `meta.module: Modules.Camps` taşır.

## Frontend durumu — akışın tamamı ekranlı

| Adım | Ekran | API |
|---|---|---|
| Kamp CRUD, dönem ekle/düzenle/sil | `CampListView`, `CampFormView`, `CampDetailView` | var |
| Kayıt yap / bekleme listesi / tamamla / dönemi toplu tamamla / kaydı iptal | `CampEnrollmentView` | var |
| Kamp aktif/pasif, kamp silme | `CampListView`, `CampDetailView` (onay penceresi) | var |
| Etkinlik oluştur/düzenle/sil | `CampPeriodDetailView` → Etkinlikler | var |
| Etkinlik katılımı (tekil, toplu, düzeltme) | `CampPeriodDetailView` → etkinlik satırı → Katılım penceresi | var |
| Günlük yoklama (toplu, tekil, düzeltme), devam özeti | `CampPeriodDetailView` → Günlük Yoklama / Devam Özeti | var |
| Kamp sonu öğrenci raporu (yaz/listele) | `CampEnrollmentView` → tamamlanmış satır → Rapor | var |
| Eğitmen atama / rol / kaldırma | `CampDetailView` → Görevli Eğitmenler | var |
| Kamp performansı | `CampPerformanceView` (`/camps/performance`) | var |

Ekran kuralları `utils/campRules.ts`'te (`canRecordForEnrollment`, `canWriteReport`, `planBulkSave`: taslağı mevcut kayıtlarla karşılaştırıp yeni → toplu uç, değişen → tekil düzeltme uçlarına ayırır, `attendanceRate`). Asıl denetim sunucudadır; düğmeler yalnızca kullanıcıyı yönlendirir.

(Menü: V62 ile `camps.reports` → `/camps/performance` (`camp:read`); `camps.attendance` pasif, çünkü yoklama dönem sayfasındaki sekmedir.)

## Durumlar

| Varlık | Değerler |
|---|---|
| Kamp | aktif / pasif (**Aktifleştir/Pasifleştir**; zaten aktifse "Kamp zaten aktif.", pasifse "Kamp zaten pasif.") |
| Kamp kaydı (`CampEnrollmentStatus`) | `enrolled`, `waitlist`, `withdrawn`, `completed` |
| Kamp yoklaması | `present`, `absent`, `late`, `excused` (neden `attendance_reason` ref değeri; dönem sayfasında `absent`/`excused` satırında seçilir, `ATTENDANCE_REASON` listesi normal seans yoklamasıyla aynıdır; `planBulkSave` nedeni de taşır ve yalnız neden değişse bile düzeltme (`PATCH`) gönderir) |
| Etkinlik katılımı | `registered`, `attended`, `absent`, `excused` |
| Kamp eğitmen rolü | `lead`, `assistant`, `observer`, `supervisor` |

Geçişler (hepsi sunucuda denetlenir; geçersizse 409): `waitlist → enrolled` (**Listeye Al** düğmesi = `promote`; API adı *promote from waitlist*) · `enrolled → waitlist` (MoveToWaitlist; **yalnızca `enrolled`**) · `enrolled → completed` · `enrolled/waitlist → withdrawn`. Etkinlik de aktif/pasif yapılabilir ("Etkinlik zaten aktif/pasif.").

## Adım adım

1. **Kamp:** `Kamp Yönetimi → Yeni Kamp` (`camp:create`): kod, ad, tür, konum, **kapasite (pozitif)**, açıklama. Zorunlu alanlar: "{0} zorunludur."
2. **Dönem:** kamp detayı → **Dönem Ekle** (`camp:manage_periods`): dönem adı, başlangıç/bitiş (bitiş başlangıçtan önce olamaz), kapasite (pozitif).
3. **Kayıt:** dönemin kayıt ekranı (`/camps/periods/:periodId/enrollments`) → **Kayıt Yap** (`camp_enrollment:enroll`): öğrenci, durum `enrolled` veya `waitlist`. Kayıt tarihi kaydedilir.
   - Kontenjan doluyken `enrolled` istenirse: "Kamp dönemi kontenjanı dolu. 'Bekleme listesi' durumunu kullanın."
   - Aynı öğrenci aynı dönemde ikinci kez: "Öğrenci bu kamp döneminde zaten kayıtlı veya bekleme listesinde."
4. **Bekleme listesi:** yer açılınca satırda **Listeye Al** (`camp_enrollment:manage`) → `enrolled`. Hâlâ doluysa: "Kamp dönemi kontenjanı hâlâ dolu." **Kaydı İptal Et** (`withdraw`) yer açar.
5. **Etkinlik** (`POST /api/camps/periods/{periodId}/activities`; `camp_activity:create`, `PUT/DELETE …/activities/{id}`): başlangıç/bitiş (bitiş sonra olmalı), kapasite pozitif. Düzenleme `rowVersion` ister (ekran `GET activities/{id}` ile alır).
6. **Katılım:** `POST …/activities/{id}/participations` (tekil) ve `…/participations/bulk` (toplu → eklenen sayı), durum `PATCH …/participations/{id}` (`camp_participation:record`). Kurallar: kayıt etkinliğin dönemine ait ve `enrolled` (409 `EnrollmentActivityPeriodMismatch` / `EnrollmentNotActive`), aynı kayıt tekrar 409 `ParticipationAlreadyRecorded`, kontenjan (`registered`/`attended` sayılır) dolunca 409 `ActivityFull`. Toplu istek tümüyle doğrulanır; biri geçersizse hiçbiri yazılmaz. Düzeltme de `enrolled` ister.
7. **Yoklama:** `POST /api/camps/attendance` (tekil) veya `periods/{id}/attendance/bulk` (toplu → eklenen sayı; aynı gün kaydı olan atlanır; kayıt başka döneme aitse 404 `EnrollmentsNotInPeriod`) (`camp_attendance:record`), `PATCH attendance/{id}` düzeltme; `GET attendance?campPeriodId=&dateFrom=&dateTo=` gün listesi; dönem özeti `attendance-summary`. **`enrolled` olmayan kayıt (tamamlanmış, iptal, bekleyen) için hepsinde 409 `EnrollmentNotActive`.**
8. **Kapanış ve rapor:** tamamlanan kayıt `completed` ("Yalnızca aktif kayıtlı öğrenciler tamamlandı olarak işaretlenebilir."), toplu: `periods/{id}/bulk-complete` (tamamlanan sayıyı döner; dönem yoksa 404, başlamamışsa 409, bekleyenlere dokunmaz). Kayıt başına öğrenci raporu `POST enrollments/{id}/reports` (`camp_report:create`): **yalnızca `completed` kayıt için**, aksi 409 `CampReportNeedsCompleted`; özet ya da dosyadan en az biri ("… alanlarından en az biri verilmelidir"; ekran özet ister). Rapor değiştirilemez/silinemez. Sıra bu yüzden: **etkinlik → katılım → yoklama → tamamla → rapor**.
9. **Pano ve performans:** `/camps`: toplam/aktif kamp, toplam dönem, aktif kamplar listesi; **Performans Raporu** düğmesi `/camps/performance`'a gider (`GET /api/camps/performance`: kamp bazında kayıt [yalnız `enrolled`+`completed`; bekleme ve iptal hariç], tamamlanan, ayrılan, tamamlama %, devam %).

## Dikkat edilecek kurallar ve hata mesajları

| Durum | Mesaj |
|---|---|
| Kapasite dolu | "Kamp dönemi kontenjanı dolu. 'Bekleme listesi' durumunu kullanın." |
| Terfi yeri yok | "Kamp dönemi kontenjanı hâlâ dolu." |
| Çift kayıt | "Öğrenci bu kamp döneminde zaten kayıtlı veya bekleme listesinde." |
| Yanlış durumdan işlem | "Yalnızca bekleme listesindeki kayıtlar 'kayıtlı' durumuna alınabilir." / "Yalnızca aktif kayıtlı öğrenciler tamamlandı olarak işaretlenebilir." / "'{0}' durumundaki bir kayıt değiştirilemez." |
| Yönetim engelleri (409) | "Kayıtlı veya bekleme listesinde öğrencisi olan bir kamp silinemez…" (`CampHasActiveEnrollments`) · "Devam eden veya başlamamış dönemlerinde kayıtlı ya da bekleyen öğrencisi olan bir kamp pasifleştirilemez." (`CampHasOngoingEnrollments`) · "Pasif bir kampa kayıt alınamaz…" (`CampInactive`) · "Kayıtları olan bir kamp dönemi silinemez." · "Etkinlikleri olan bir kamp dönemi silinemez…" (`CampPeriodHasActivities`) · "Dönem kontenjanı, mevcut kayıtlı öğrenci sayısından ({0}) az olamaz." · "Henüz başlamamış bir dönemde toplu tamamlama yapılamaz." · "Yalnızca kayıtlı öğrenciler bekleme listesine alınabilir." |
| Günlük işler (409) | "{0} kaydı bulunamadı veya öğrenci aktif olarak kayıtlı değil." (`EnrollmentNotActive`: yoklama/katılım, toplu dâhil, tamamlanmış/iptal/bekleyen kayıt) · "Kayıt ve etkinlik aynı kamp dönemine ait olmalıdır." · "Bu kayıt ve etkinlik için katılım zaten kaydedilmiş." · etkinlik kontenjanı dolu (`ActivityFull`) · "{0} tarihli yoklama zaten kaydedilmiş…" · "Kamp sonu raporu yalnızca tamamlanmış kayıt için yazılabilir." (`CampReportNeedsCompleted`) |
| Tarih / sayı | "{0}, {1} alanından önce/sonra olmalıdır." / "{0} pozitif olmalıdır." |
| Geçersiz değer | "{0} şu değerlerden biri olmalıdır: {1}." (yoklama durumu, rol, katılım) |

- Kamp kaydı **öğrenci kaydı değildir** (kurum kaydı `education.enrollment` ayrı; Akış 4). Kamp ödemesi/paketi için `UpdatePackage` alanı var; finans bağı ekranda görünmüyor [doğrulanacak].
- Kapasite sayımı `enrolled` olanlara bakar; bekleme listesindekiler sayılmaz [doğrulanacak: sayım filtresi].

## Koddaki yeri

- Vue: `views/camps/*` (`CampPeriodDetailView` etkinlik/katılım/yoklama, `CampPerformanceView`)
- Store/servis: `stores/camp.store.ts`, `campEnrollment.store.ts`; `services/camp.service.ts`; kural yardımcısı `utils/campRules.ts` (testi `tests/campRules.test.mjs`)
- Testler: `tests/Aynesil.Tests/TenantScoping/CampManagementTests.cs` (yönetim), `CampDailyOperationsTests.cs` (etkinlik, katılım, yoklama, rapor, 409 kuralları), `tests/campRules.test.mjs`; smoke-api `campFlows` + `campDailyFlows`
- Controller: `CampsController.cs` (rota `api/camps`)
- Command/Query: `Application/Features/Camps/Commands/` (`CampCommands`, `CampPeriodCommands`, `CampEnrollmentCommands`, `CampActivityCommands`, `CampParticipationCommands`, `CampAttendanceCommands`, `CampReportCommands`, `CampEducatorCommands`)
- Domain: `Domain/Modules/Camps/Entities/`
- DB: `camps.camp`, `camp_period`, `camp_enrollment`, `camp_activity`, `camp_activity_participation`, `camp_attendance`, `camp_educator`, `camp_report`

## Kendin dene (demo)

`demo_a`'da 1 kamp (4 dönem [dönem 1 tam akış: katılım + yoklama + tamamlama + 3 rapor; dönem 3 toplu tamamlanmaya hazır ve iki günlük yoklamalı; dönem 4 boş], 4 etkinlik, 10 kayıt [1 bekleme listesi], 2 eğitmen) var.
1. `koordinator@demo-a.test` → `Kamp Yönetimi → Kamplar` → kamp detayı: dönemler ve görevli eğitmenler.
2. "Temmuz Dönemi" → **Etkinlik ve Yoklama**: etkinlikler, **Katılım** (bir durumu düzeltin), **Günlük Yoklama** (ilk gün toplu girilmiş; bir gün düzeltilmiş), **Devam Özeti**.
3. Aynı dönemin **Kayıtlar** ekranında tamamlanmış satırlarda **Rapor**; raporu olmayan öğrenci için yazın.
4. "Haziran Dönemi (tamamlanacak)" → **Dönemi Tamamla**; ardından aynı dönemde yoklama düzeltmeyi deneyip 409 iletisini görün.
5. Dönem kayıt ekranında bekleme listesindeki kayıtta **Listeye Al**; aynı öğrenciyi tekrar kaydetmeyi deneyin; boş dönemi düzenleyin/silin; bekleyen kayıt varken kampı pasifleştirmeyi deneyin.
6. Panodan **Performans Raporu**.
