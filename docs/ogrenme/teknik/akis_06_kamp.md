# Akış 6 — Kamp: kamp → dönem → kayıt / bekleme listesi → etkinlik → yoklama

**Ne işe yarar:** Yaz/dönem kampı gibi kısa süreli programları tanımlamak, dönemlere kapasiteli kayıt almak, kapasite dolunca bekleme listesi yönetmek, etkinlik ve devam takibi yapmak.

**Kim yapar:** Koordinatör/yönetici. Demo eğitmen rolü kamp izinlerinden `camp:read`, `camp_attendance:*`, `camp_participation:*` alır (kamp yoklaması ve etkinlik katılımı). Menü seed'inde kamp izni `camp:read` (hangi rolün göreceği bu izne bağlı).

**Menü:** `Kamp Yönetimi` (`camps`, rota `/camps`). V21 seed'i alt öğeleri `camps.definitions/enrollments/attendance/reports` olarak koymuştu; **router'daki gerçek ekranlar** `/camps` (pano), `/camps/list`, `/camps/new`, `/camps/:id`, `/camps/:id/edit`, `/camps/periods/:periodId/enrollments` — alt menü rotaları ile router tam örtüşmüyor [doğrulanacak: güncel menüde alt öğeler çalışıyor mu].

## Frontend durumu — bu akışın yarısı yalnızca API'de

| Adım | Ekran | API |
|---|---|---|
| Kamp CRUD, dönem ekle | **Var** (`CampListView`, `CampFormView`, `CampDetailView`) | var |
| Kayıt yap / bekleme listesinden al / kaydı iptal | **Var** (`CampEnrollmentView`) | var |
| Dönem düzenle/sil, kamp aktif/pasif, kamp silme | Yok (API'de) | var |
| Bekleme listesine alma (`waitlist`), tamamlama (`complete`, `bulk-complete`) | **Yok — yalnızca API'de** | var |
| **Etkinlik** (oluştur/güncelle/sil) ve **etkinlik katılımı** | **Yok — yalnızca API'de** | var |
| **Kamp yoklaması** (tekli, toplu, düzeltme, özet) | **Yok — yalnızca API'de** | var |
| **Kamp öğrenci raporu** | **Yok — yalnızca API'de** | var |
| Eğitmen atama / rol değiştirme | Yok (detayda "Görevli Eğitmenler" **listelenir**); atama API'de | var |
| Kamp performansı (`GET /api/camps/performance`) | Yok | var |

(Menü seed'indeki `camps.attendance` ve `camps.reports` satırları için ekran yok.)

## Durumlar

| Varlık | Değerler |
|---|---|
| Kamp | aktif / pasif (**Aktifleştir/Pasifleştir**; zaten aktifse "Kamp zaten aktif.", pasifse "Kamp zaten pasif.") |
| Kamp kaydı (`CampEnrollmentStatus`) | `enrolled`, `waitlist`, `withdrawn`, `completed` |
| Kamp yoklaması | `present`, `absent`, `late`, `excused` (mazeret `attendance_reason` ref değeri; absent/excused için gerekli) |
| Etkinlik katılımı | `registered`, `attended`, `absent`, `excused` |
| Kamp eğitmen rolü | `lead`, `assistant`, `observer`, `supervisor` |

Geçişler: `waitlist → enrolled` (**Listeye Al** düğmesi = `promote`; API adı *promote from waitlist*) · `enrolled → waitlist` (MoveToWaitlist) · `enrolled → completed` · `enrolled/waitlist → withdrawn`. Etkinlik de aktif/pasif yapılabilir ("Etkinlik zaten aktif/pasif.").

## Adım adım

1. **Kamp:** `Kamp Yönetimi → Yeni Kamp` (`camp:create`): kod, ad, tür, konum, **kapasite (pozitif)**, açıklama. Zorunlu alanlar: "{0} zorunludur."
2. **Dönem:** kamp detayı → **Dönem Ekle** (`camp:manage_periods`): dönem adı, başlangıç/bitiş (bitiş başlangıçtan önce olamaz), kapasite (pozitif).
3. **Kayıt:** dönemin kayıt ekranı (`/camps/periods/:periodId/enrollments`) → **Kayıt Yap** (`camp_enrollment:enroll`): öğrenci, durum `enrolled` veya `waitlist`. Kayıt tarihi kaydedilir.
   - Kontenjan doluyken `enrolled` istenirse: "Kamp dönemi kontenjanı dolu. 'Bekleme listesi' durumunu kullanın."
   - Aynı öğrenci aynı dönemde ikinci kez: "Öğrenci bu kamp döneminde zaten kayıtlı veya bekleme listesinde."
4. **Bekleme listesi:** yer açılınca satırda **Listeye Al** (`camp_enrollment:manage`) → `enrolled`. Hâlâ doluysa: "Kamp dönemi kontenjanı hâlâ dolu." **Kaydı İptal Et** (`withdraw`) yer açar.
5. **Etkinlik** (`POST /api/camps/periods/{periodId}/activities`; `camp_activity:create`): başlangıç/bitiş (bitiş sonra olmalı), kapasite pozitif; öğrenciyi etkinliğe katılımcı yazma `…/participations` (tekli/toplu), durum `PATCH`. [Ekran yok.]
6. **Yoklama:** `POST /api/camps/attendance` veya `periods/{id}/attendance/bulk` (`camp_attendance:record`), `PATCH attendance/{id}` ile düzeltme; dönem özeti `attendance-summary`. [Ekran yok.]
7. **Kapanış:** tamamlanan kayıt `completed` ("Yalnızca aktif kayıtlı öğrenciler tamamlandı olarak işaretlenebilir."), toplu: `periods/{id}/bulk-complete`. Kayıt başına öğrenci raporu (`camp_report:create`; "… alanlarından en az biri verilmelidir").
8. **Pano:** `/camps`: toplam/aktif kamp, toplam kayıt, toplam dönem, aktif kamplar listesi.

## Dikkat edilecek kurallar ve hata mesajları

| Durum | Mesaj |
|---|---|
| Kapasite dolu | "Kamp dönemi kontenjanı dolu. 'Bekleme listesi' durumunu kullanın." |
| Terfi yeri yok | "Kamp dönemi kontenjanı hâlâ dolu." |
| Çift kayıt | "Öğrenci bu kamp döneminde zaten kayıtlı veya bekleme listesinde." |
| Yanlış durumdan işlem | "Yalnızca bekleme listesindeki kayıtlar 'kayıtlı' durumuna alınabilir." / "Yalnızca aktif kayıtlı öğrenciler tamamlandı olarak işaretlenebilir." / "'{0}' durumundaki bir kayıt değiştirilemez." |
| Tarih / sayı | "{0}, {1} alanından önce/sonra olmalıdır." / "{0} pozitif olmalıdır." |
| Geçersiz değer | "{0} şu değerlerden biri olmalıdır: {1}." (yoklama durumu, rol, katılım) |

- Kamp kaydı **öğrenci kaydı değildir** (kurum kaydı `education.enrollment` ayrı; Akış 4). Kamp ödemesi/paketi için `UpdatePackage` alanı var; finans bağı ekranda görünmüyor [doğrulanacak].
- Kapasite sayımı `enrolled` olanlara bakar; bekleme listesindekiler sayılmaz [doğrulanacak: sayım filtresi].

## Koddaki yeri

- Vue: `views/camps/*`
- Store/servis: `stores/camp.store.ts`, `campEnrollment.store.ts`; `services/camp.service.ts`
- Controller: `CampsController.cs` (rota `api/camps`)
- Command/Query: `Application/Features/Camps/Commands/` (`CampCommands`, `CampPeriodCommands`, `CampEnrollmentCommands`, `CampActivityCommands`, `CampParticipationCommands`, `CampAttendanceCommands`, `CampReportCommands`, `CampEducatorCommands`)
- Domain: `Domain/Modules/Camps/Entities/`
- DB: `camps.camp`, `camp_period`, `camp_enrollment`, `camp_activity`, `camp_activity_participation`, `camp_attendance`, `camp_educator`, `camp_report`

## Kendin dene (demo)

`demo_a`'da 1 kamp (2 dönem, 4 etkinlik, 8 kayıt [1 bekleme listesi], 2 eğitmen, yoklama) var.
1. `koordinator@demo-a.test` → `Kamp Yönetimi → Kamplar` → kamp detayı: dönemler ve görevli eğitmenler.
2. Dönem kayıt ekranında bekleme listesindeki kayıtta **Listeye Al**: kapasite doluysa "hâlâ dolu" mesajını, yer açtıktan (başka kaydı iptal edince) sonra başarıyı görün.
3. Aynı öğrenciyi aynı döneme tekrar kaydetmeyi deneyin.
4. Etkinlik/yoklama verisi demo'da var ama ekranı yok: görmek için API (`GET /api/camps/activities`, `/api/camps/attendance`) [doğrulanacak: Swagger/Postman ile].
