# Uyuşmazlık Raporu — Doğrulama

> Tarih: 2026-11-12 · Doğrulanan: `docs/uyusmazlik-raporu.md` (#1–#88, Ek C, Ek D) ve sonrasındaki partiler (#86/#87 DB bütünlüğü, duman testi düzeltmeleri, i18n adım 1–6, giriş/parola sıfırlama sertleştirmesi) · Kodda değişiklik yapılmadı.

## Özet

| | Sayı |
|---|---|
| Kapandığı doğrulanan ✅ bulgu | **85** (✅ işaretli 87 bulgudan; #71 ve #83 kısmen açık. #60, #70, #82, #86'da kapsam notu var) |
| Açık kalan | **3** (#88 ertelenmiş; #71 ve #83 kısmen) |
| Yeni bulgu | **15** — Kritik 1, Yüksek 3, Orta 6, Düşük 5 |

En önemli yeni bulgular:

- **Y1 (Kritik):** Kurum yönetimi kiracıyla sınırlı değil. `core.corporation` tablosunda RLS kapalı ve kurum handler'ları kiracı filtresi uygulamıyor. Seed'deki kurum yöneticisi rolü `corporation:*` izinlerine sahip. Canlı denemede demo_a yöneticisi dört kurumun hepsini listeledi. Kod okumasına göre başka bir kurumu güncelleyebilir, ayarlarını değiştirebilir ve pasifleştirebilir.
- **Y2, Y3, Y4:** Bazı alt tablolarda `corporation_id` kolonu yok ve RLS kapalı. Bu tablolara yazan handler'larda üst kaydın kiracı kontrolü eksik. Etkilenenler: rol izni kaldırma, değerlendirme şablonunun bölüm/maddeleri, program çevirisi. #86 çalışması ve DB değişmez kontrolü yalnızca `corporation_id` kolonu olan tabloları kapsadığı için bu boşluk görünmedi.
- **Y5:** İzin talebi onay/ret kaydındaki `ApproverId` istemciden geliyor. Arayüz bu alanı göndermediği için her onay kaydı **onaylayansız** (NULL) yazılıyor.

## Çalıştırılan komutlar

| Komut | Sonuç |
|---|---|
| `dotnet build Aynesil.sln` | ✅ 0 hata, 12 uyarı. Uyarılar: CS8629/CS8602 `CameraQueries.cs:429-431`, CS8625 `PaymentsController.cs:475`, CS0618 Hangfire, NU1603 xunit runner, **NU1903 AutoMapper 14.0.0 (yüksek)**, **NU1902 MailKit 4.10.0 (orta)**; bkz. Y9 |
| `dotnet test tests/Aynesil.Tests` | ✅ 429/429 |
| `dotnet test tests/Aynesil.IntegrationTests` (Docker, PG17 + Flyway) | ✅ 25/25 |
| `dotnet test Aynesil.sln --filter "Category!=Integration"` | ✅ 429/429 |
| `npm run type-check` | ✅ temiz |
| `npm run build` | ✅ temiz (3,2 sn) |
| `bash db/validation/smoke_test.sh` | ✅ GREEN. Şema kayması yok; `TENANT RLS INVARIANT OK` (istisna: 6 log/menü tablosu) |
| `scripts/smoke-api/run.sh` | ✅ 239 çağrı: 236 başarılı, 4xx 0, 5xx 0, tip uyuşmazlığı 0, izolasyon sızıntısı 0, 3 atlama (boş kaynak liste). 51 başarılı yanıtta liste boş olduğu için öğe alanları doğrulanamadı |

Çalışma ağacı test sonrasında temiz (`git status` boş). `endpoints.json` yeniden üretildi ve içeriği değişmedi.

## Yöntem

İlk raporun yöntemi betiklerle yeniden uygulandı:

- **Rota eşleştirme:** 36 controller'dan 579 backend endpoint'i çıkarıldı (ilk raporla aynı sayı). Frontend'in 515 API çağrısı (`extract-endpoints.mjs`) bunlarla karşılaştırıldı. **Eşleşmeyen çağrı: 0.** Frontend'in çağırmadığı endpoint sayısı 65 (ilk raporda 79).
- **Yanıt DTO alanları:** `ProducesResponseType` ile tipi bilinen 392 çağrıda, frontend tipindeki her alan backend DTO'sunda (iç içe 2 seviyeye kadar) arandı. **Fark: 0.** `Guid`/`int`/NoContent dönen uçlarda frontend'in nesne beklediği yer: **0.** Attribute'u olmayan 114 uç yalnızca API duman testiyle çalışma anında kontrol edildi.
- **İstek gövdesi ve sorgu parametresi:** TypeScript derleyicisiyle `qs(...)` argümanları ve gövde tipleri çıkarıldı. Bunlar controller imzaları ve C# record/sınıf özellikleriyle (`PagedQuery` kalıtımı dahil) karşılaştırıldı. 48 aday çıktı; her biri kodda açıldı. Rotadan doldurulan alanlar ve aynı adlı iki komut gibi yanlış pozitifler elendi. Kalanlar: Y5, Y10, Y12 ve açık kalan #71/#83.
- **Durum değerleri:** `constants/statuses.ts` içindeki 28 sabitin 27'si DB check constraint'leriyle (`pg_constraint`) birebir aynı. 28.si (`ViewingAuthorizationState`) türetilmiş bir değer. Sabit dışında yazılmış ve hiçbir DB check'inde geçmeyen durum literal'i: **0.**
- **İzin kodları:** Frontend 327, `Permissions.cs` 327, seed 327; üçü aynı. Sabit dışında yazılmış izin literal'i: 0.
- **i18n:** `tr.ts` içinde 1774 anahtar var; `t()` ile kullanılıp tanımlı olmayan anahtar: 0.
- **Güvenlik:**
  - İstek tiplerinde aktör/sahiplik alanı taraması (`*By`, `*Id` aktör adları).
  - Canlı DB'de (`aynesil-db`, Flyway V42) RLS ve `aynesil_app` yetki taraması.
  - `ExceptionMiddleware`, `InvalidModelStateResponseFactory` ve `ApiAuthorizationResultHandler` okundu.
  - Giriş ve parola sıfırlama, canlı API'ye karşı süre ve gövde karşılaştırmasıyla ölçüldü.
  - DI kayıtları `ServiceRegistrationTests` ile kontrol edildi (ValidateOnBuild + ValidateScopes, Development ve Production) ve test geçti.

---

## Tablo 1 — Kapanan bulgular

Kanıt kısaltmaları:
- **R:** rota eşleştirmesi (515/515).
- **D:** DTO alan karşılaştırması (392 uç, 0 fark).
- **G:** istek gövdesi/sorgu karşılaştırması.
- **S:** durum sabitleri DB ile aynı, çıplak literal yok.
- **İ:** izin kodları üç tarafta aynı.
- **K:** kod tekrar okundu.
- **T:** test veya duman testi geçti.

| # | Önem | Kısa konu | Doğrulama |
|---|---|---|---|
| 1 | Kritik | Bildirim tercihleri `corporationId` | K: `NotificationsController` `UserId`'yi oturumdan alıyor; komutta `CorporationId` yok. T: `TenantContractTests` |
| 2 | Kritik | Paket tanımı güncellemede `code` | K: `PackageDefinitionFormView.vue:79` `code` gönderiyor. G |
| 3 | Kritik | Promosyon güncellemede `code` | K: `PromotionListView.vue:67,74`. G |
| 4 | Kritik | Finans izin kodları | İ: `credit_ledger:grant/adjust`, `refund:request`, `scholarship:grant` (`constants/permissions.ts:312-342`) |
| 5 | Kritik | Kamera atama `corporationId` | K + T: istek record'larında alan yok; tenant bağlamından geliyor |
| 6 | Kritik | İzleme yetkisi `corporationId` | K + T |
| 7 | Kritik | Kamp kayıt listesi rotası | R: `GET /camps/enrollments?campPeriodId=` |
| 8 | Kritik | Kamp kaydı rota + `corporationId` | R: `POST /camps/periods/{id}/enrollments`. G: `CampPeriodId` rotadan dolduruluyor |
| 9 | Kritik | Kamp dönemi `corporationId` | G + T |
| 10 | Kritik | Kullanıcı durumu PascalCase | S (`UserStatus`) |
| 11 | Kritik | Kurum durumu PascalCase | S (`CorporationStatus`) |
| 12 | Kritik | Derslik güncellemede `code` | K: `RoomListView.vue:90-97` `code`, `roomTypeId`, `meetingUrl` gönderiyor; `isVirtual` gövdede yok |
| 13 | Kritik | Sözleşme iş akışı durumları | S (`ContractStatus`) |
| 14 | Kritik | İmza yöntemi ve imzalayan adı | K: `ContractDetailView.vue:25,40-43` (`wet` varsayılan, `signedByName` zorunlu). G |
| 15 | Kritik | Takip görevi `completed` → `done` | S (`MeetingFollowUpStatus`) |
| 16 | Kritik | Toplantı sonucu/takip `corporationId` | G: `MeetingId` rotadan dolduruluyor. T |
| 17 | Kritik | Portal öğrenci tipleri | D: `PortalStudentDto` alanları eşleşiyor. T: veli turu 12/12 |
| 18 | Kritik | İzin devrinde `educatorId` | K: `LeaveBalanceView.vue:64` |
| 19–23 | Yüksek | CRM ad/etiket alanları, pano, rapor, görüşme | D. K: `useCrmLabels` |
| 24 | Yüksek | Finans panosu parametreleri | G: `periodStart`/`periodEnd` (`FinanceDashboardView.vue:31-32`). D |
| 25 | Yüksek | Fatura satır indirimi | K: `discountAmount` frontend tiplerinden ve view'den kaldırılmış |
| 26–30 | Yüksek | Finans liste ad alanları | D |
| 31, 32 | Yüksek | Kamera oluştur/güncelle yanıtı | D: NoContent ve Guid kontrolünde fark yok |
| 33 | Yüksek | Kamp eğitmenleri rotası | R: `GET /camps/educators?campId=` |
| 34 | Yüksek | Kamp kayıt özeti | R: `GET /camps/{campId}/enrollment-summary`, liste dönüyor |
| 35 | Yüksek | Kamp güncelleme NoContent | D (NoContent kontrolü) |
| 36 | Yüksek | Kamp kayıt durumları | S (`CampEnrollmentStatus`) |
| 37 | Yüksek | Kurum güncelleme NoContent | D |
| 38 | Yüksek | Danışmanlık panosu durumları | S. K: `ConsultancyDashboardView.vue:18-29` sabit kullanıyor |
| 39 | Yüksek | KPI panosu dönem ve `kpiCode` | K: `KpiDashboardView.vue:67-84`. G |
| 40 | Yüksek | İzin kataloğu sayfalama | K: `permission.service.ts:16-30` tüm sayfaları döngüyle çekiyor |
| 41–43 | Yüksek | Yoklama `*Count`, seans etiketleri, derslik kampüsü | D |
| 44 | Yüksek | Bugünkü seanslar | K: `SchedulingDashboardView.vue:25-27` (`startOfDayIso`/`endOfDayIso`) |
| 45 | Yüksek | Hukuk rapor rotaları | R: `/legal/reporting/*` |
| 46 | Yüksek | Sözleşme imzalama NoContent | D |
| 47 | Yüksek | Hukuk panosu sayaçları | S |
| 48, 49 | Yüksek | Toplantı NoContent ve Guid dönüşü | D |
| 50 | Yüksek | Toplantı saat dilimi | K: `MeetingFormView.vue:7` (`localInputToIso`/`isoToLocalInput`) |
| 51 | Yüksek | Portal listeleri düz dizi | D. T: veli turu |
| 52 | Yüksek | İzin trend raporu | K: `LeaveReportsView.vue:53` (`fromYear`/`toYear`) |
| 53 | Yüksek | İzin güncelleme NoContent | D |
| 54 | Orta | Bildirim tarih filtresi | G: backend'e `From`/`To` eklenmiş |
| 55 | Orta | Ödeme tarihi | K: `RecordPaymentCommand.PaidAt` (`PaymentCommands.cs:25`), formdan gönderiliyor |
| 56 | Orta | Burs notu | K: `ScholarshipListView.vue:71` (`note`) |
| 57 | Orta | Kredi hareket türü | S (`CreditEntryType`) |
| 58 | Orta | Ödeme tarih filtresi | G |
| 59 | Orta | Hata mesajı normalizasyonu | K: `api.service.ts` ikinci response interceptor'ı (`resolveErrorMessage`) |
| 60 | Orta | 400/403 zarfı | K: `ApiServiceCollectionExtensions.cs:73`, `ApiAuthorizationResultHandler`. Not: 400 gövdesi iç ayrıntı sızdırıyor, bkz. Y8 |
| 61 | Orta | İzleme yetkisi filtreleri | G (`activeOnly`) |
| 62–64 | Orta | Kullanıcı, kurum ve ziyaret durum filtreleri | S |
| 65, 66 | Orta | Ziyaret tarih filtresi, `overdueOnly` | G |
| 67 | Orta | Seans listesi tarih aralığı | K: `dateRangeToIso` |
| 68, 69 | Orta | Seans ve sözleşme/rıza durum filtreleri | S |
| 70 | Orta | Hukuk listelerinde arama/sıralama | Ekranlar `search`/`sortBy` göndermiyor. **Not:** "tiplerde yok" ifadesi tam doğru değil; alanlar `PagedQuery`'den kalıtımla hâlâ tipte (bkz. Y14). İşlevsel etkisi yok |
| 72 | Orta | İzin durum ve birim filtreleri | S |
| 73, 74 | Düşük | Şablon fazla alanları, gelişim alanı | G, D |
| 75 | Düşük | Finans tip farkları | G (`issueDate` var), D |
| 76 | Düşük | Eksik i18n anahtarları | Tarama: 0 eksik |
| 77–79 | Düşük | İzleme başlat/bitir, kamp ölü servisleri, danışmanlık ölü fonksiyonları | R, G |
| 80, 81 | Düşük | KPI servis tipleri, locale çağrısı | G, R |
| 82 | Düşük | Program/kayıt araması | Ekranlar `search` göndermiyor. #70 ile aynı not (kalıtımla tipte) |
| 84, 85 | Düşük | Rıza rotaları, portal gelişim raporları | R, D |
| 86 | Kritik | Kiracılar arası FK | T: DB duman testi (202 composite FK, 63 tetikleyici, kayma yok) ve entegrasyon testleri 25/25. **Kapsam notu:** yalnızca `corporation_id` kolonu olan tablolar korunuyor; bkz. Y2–Y4 |
| 87 | Kritik | Danışmanlık tablolarında RLS | T: canlı DB'de `corporation_id` kolonu olan 125 tablonun tamamında RLS + politika var; duman testi değişmez kontrolü geçiyor |

**Ek D (kiracı taraması):**
- `TenantContractTests` geçiyor: hiçbir istek tipinde `CorporationId` ve yasaklı aktör alanı yok; 170 handler tenant yokken 401 veriyor.
- `QueryTenantIsolationTests` ve `PortalGuardianTests` geçiyor. API duman testinin izolasyon turu: 15/15.
- **Boşluk:** Yasaklı alan listesinde `ApproverId` yok (Y5).

**Giriş / parola sıfırlama (son commit):** Canlı ölçüm (demo_b):
- **Giriş:** Var olan kullanıcı + yanlış parola ile var olmayan kullanıcı aynı 401 gövdesini (`Auth.InvalidCredentials`) döndürüyor. Medyan süre ikisinde de 199 ms. Kilitli/devre dışı durumu yalnızca parola doğruysa bildiriliyor (`LoginCommand.cs:87-103`).
- **Parola sıfırlama isteği:** Var olan ve olmayan e-posta için gövde aynı (200), medyan süre ikisinde de 2 ms.
- **Kalan riskler:** hız sınırı yok (Y7). Gerçek e-posta sağlayıcısı bağlandığında gönderim senkron yapılırsa süre farkı yeniden oluşur; kod yorumu bunu belirtiyor (`RequestPasswordResetCommand.cs`).

**DI:** `ServiceRegistrationTests` (ValidateOnBuild + ValidateScopes, Development ve Production) geçiyor. API duman testinde çözümlenemeyen servis (5xx) çıkmadı. **Eksik kayıt bulunmadı.**

**Hata yanıtları:** `ExceptionMiddleware` bilinmeyen istisnalarda yalnızca genel mesaj + kod + TraceId döndürüyor. FK ihlallerinde kısıt adını yalnızca log'a yazıyor. `UnauthorizedAccessException` metni istemciye gitmiyor. Tek istisna model bağlama hataları (Y8).

---

## Tablo 2 — Açık kalan bulgular

| # | Önem | Durum | Kanıt | Önerilen düzeltme |
|---|---|---|---|---|
| 88 | Yüksek | **Açık (ertelendi).** Promosyon kullanım limiti uygulanmıyor | `GetPromotionsQuery.cs` doğrulamasında kullanım sayısı yok; şemada promosyon uygulamasını kaydeden tablo veya kolon yok (migration'larda `promotion_redemption`/`promotion_id` yok) | Şema kararı gerekiyor (`finance.promotion_redemption` veya `invoice.promotion_id`). Ardından doğrulamada limit kontrolü ve `redemptionCount` eklenmeli |
| 83 | Düşük | **Kısmen açık.** `CreateCalendarEntryPayload.description` hâlâ tipte; partide "kaldırıldı" deniyordu. `RecurringScheduleListQuery`/`MakeupRequestListQuery` `search`'ü kalıtımla taşıyor | `types/scheduling.types.ts:79`; backend `CreateCalendarEntryRequest`'te alan yok. `store.createCalendarEntry`'yi hiçbir ekran çağırmıyor | `description`'ı tipten kaldır. Liste sorgularında `PagedQuery` yerine yalnızca `page`/`pageSize`/`sortBy` içeren bir taban tip kullan |
| 71 | Düşük | **Kısmen açık.** Tarih ve `draft` düzeltilmiş, ama `MeetingListQuery.participantUserId`/`participantGuardianId` hâlâ tipte | `types/meeting.types.ts:103-104`; `GetMeetingsQuery`'de bu parametreler yok. Hiçbir ekran göndermiyor | İki alanı tipten kaldır (ya da backend'e ekle) |

---

## Tablo 3 — Yeni bulgular

| # | Önem | Modül | Kategori | Konum | Sorun | Etki | Önerilen düzeltme |
|---|---|---|---|---|---|---|---|
| Y1 | **Kritik** | Kurumlar | güvenlik (kiracı) | `Features/Corporations/Queries/GetCorporationsQuery.cs:31`; `Features/Corporations/Commands/UpdateCorporationCommand.cs:58`, `UpdateCorporationSettingsCommand.cs:78`, `ActivateCorporationCommand.cs:40`, `DeactivateCorporationCommand.cs:40` (yalnızca `c.Id == req.Id`); `Api/Controllers/CorporationsController.cs:22-149`; canlı DB'de `core.corporation` RLS kapalı ve `aynesil_app` okuyabiliyor | Kurum listeleme, detay, güncelleme, ayar, etkinleştirme ve pasifleştirme işlemleri kiracıyla sınırlı değil. demo_a yöneticisinin JWT'sinde `corporation:create/read/update/delete` izinleri var. `GET /api/corporations` yanıtında `akran`, `demo_a`, `demo_b` ve `smoke` kurumlarının hepsi döndü (canlı doğrulandı) | Her kurum yöneticisi diğer müşterilerin adlarını, kodlarını ve ayarlarını görebilir. Kod okumasına göre bunları değiştirebilir ve başka bir kurumu pasifleştirebilir. Ayrıca yeni kurum oluşturabilir. Yazma işlemleri yıkıcı olduğu için canlı denenmedi | Kurum oluşturma ve etkinleştirme/pasifleştirme platform işlemi (`devops/new_tenant.py`): bu izinleri kiracı rollerinden kaldırın. Okuma, güncelleme ve ayar handler'larında `c.Id == ITenantContext.RequireCorporationId()` filtresi uygulayın. DB savunması olarak `core.corporation`'a `id = core.current_corporation_id()` RLS politikası ekleyin; giriş SECURITY DEFINER fonksiyonla çalıştığı için etkilenmez. Migration ve izin seed'i değişeceği için karar sizde |
| Y2 | **Yüksek** | Roller / İzinler | güvenlik (kiracı) | `Application/Features/Roles/Commands/RolePermissionCommands.cs:80-91`; `db/layer1_core/04_identity_access.sql:96` (`iam.role_permission`: `corporation_id` yok, RLS kapalı; canlı DB'de `aynesil_app` SELECT/DELETE yetkili) | `RemoveRolePermissionCommand` rolü hiç yüklemiyor ve kiracıyı kontrol etmiyor. Kaydı `FindAsync([RoleId, PermissionId])` ile doğrudan buluyor. Aynı dosyadaki ekleme tarafı (`AssignRolePermission`) `role.CorporationId != tenant` kontrolü yapıyor; kaldırma yapmıyor | `roles:assign_permission` izni olan bir kurum yöneticisi, rol id'sini bildiği başka bir kurumun rolünden izin silebilir. Şema global (sistem) rollere izin veriyor; böyle bir rol eklenirse id'si tüm kiracılara görünür ve tek kiracı onu herkes için bozabilir (yerel DB'de şu an sistem rolü 0). Kod okumasıyla doğrulandı, canlı denenmedi (yıkıcı) | Handler'da rolü `_db.Roles` ile yükle (RLS'li) ve `AssignRolePermission`'daki kontrolün aynısını uygula. Bir test ekle: başka kiracının rolü → 403/404. DB savunması için `role_permission`'a `EXISTS (select 1 from iam.role r where r.id = role_id and …)` RLS politikası eklenmeli; migration olduğu için karar sizde |
| Y3 | **Yüksek** | Değerlendirme | güvenlik (kiracı) | `Features/Assessment/Commands/ManageTemplateSectionsCommands.cs:30,72,99`; `ManageTemplateItemsCommands.cs:44,101,128`; `db/layer2_sped/04_assessment.sql:31,40` (`assessment_section`/`assessment_item`: `corporation_id` yok, RLS kapalı) | Bölüm güncelle/sil ve madde ekle/güncelle/sil handler'ları kaydı yalnızca id ile buluyor; şablonun kiracısına bakmıyor. Bölüm ekleme şablonu RLS ile okuyor, ama `assessment_template` politikası global satırları (`corporation_id IS NULL`) okumaya izin veriyor. Bu yüzden **global şablona bölüm eklenebiliyor**. Şablonun kendisini global satırda güncellemek `WITH CHECK` ile engelli; alt tablolarda engel yok | Kiracı A, global bir şablonun bölüm ve maddelerini bütün kiracılar için değiştirebilir veya silebilir. Id'sini bilirse başka bir kiracının şablon içeriğini de değiştirebilir. Global şablon id'leri tüm kiracılara listeleniyor (yerel DB'de şu an global şablon 0) | Altı handler'da üst şablonu `t.CorporationId == tenant` ile yükle (global şablonda yazmayı reddet). Bölüm ve madde için şablona join'li kiracı kontrolü ekle. `TenantContractTests` listesine bu komutları ekle. DB tarafında üst kayda `EXISTS` ile RLS politikası; migration kararı sizde |
| Y4 | Orta | Programlar | güvenlik (kiracı) | `Features/Programs/Commands/ProgramCommands.cs:162-190`; `db/layer2_sped/05_education.sql:25` (`program_translation`: RLS kapalı) | `SetProgramTranslation` programı yüklemiyor, çeviri kaydını `ProgramId` ile doğrudan buluyor veya oluşturuyor | Program id'sini bilen bir kiracı, başka kurumun program adını veya açıklamasını o kurumun ekranlarında değiştirebilir | Önce programı `_db.Programs` ile yükle (RLS + `CorporationId == tenant`), sonra çeviriyi yaz. Aynı desen diğer `*_translation` yazımlarında da kontrol edilmeli: bildirim şablonu ve menü önce üst kaydı yüklüyor, sorun yok |
| Y5 | **Yüksek** | İzin Yönetimi | güvenlik (aktör) + b | `Features/Leaves/Commands/LeaveCommands.cs:152-156,202,250` (`Guid? ApproverId`); `views/leave/LeaveDetailView.vue:37`; `types/leave.types.ts:179-187`; `tests/Aynesil.Tests/TenantScoping/TenantContractTests.cs:28-32` | Onay/ret kaydının onaylayanı istemciden alınıyor (`req.ApproverId`). Frontend bu alanı hiç göndermiyor | Arayüzden yapılan her onay ve retteki `leave_approval.approver_id` **NULL**: kimin onayladığı kaydedilmiyor. API'yi doğrudan çağıran biri, kiracıdaki başka bir kullanıcının adına onay kaydı yazabilir. Composite FK aynı kiracıyla sınırlıyor | `ApproverId`'yi komuttan kaldır, handler'da `ICurrentUserService.UserId`'den doldur (BEP onaylayan düzeltmesindeki desen). `ForbiddenRequestMembers` listesine `ApproverId` ekle |
| Y6 | Orta | Anketler | güvenlik (kiracı) | `Features/Surveys/Commands/SurveyCommands.cs:232-237`; `db/layer2_sped/16_surveys.sql:97` (`survey_question_response`: RLS kapalı) | Yanıt satırlarındaki `QuestionId`/`AnswerOptionId` değerlerinin gönderilen ankete ait olduğu doğrulanmıyor | Başka bir anketin, id biliniyorsa başka kiracının sorusuna bağlı yanıt yazılabilir. Rapor sorguları soru metnini join'lerse yabancı soru metni görünebilir. Frontend anket modülünü henüz kullanmıyor | Handler'da `Answers` id'lerini `SurveyQuestions.Where(q => q.SurveyId == req.SurveyId)` kümesine karşı doğrula; uymayanlarda 422 dön |
| Y7 | Orta | Auth | güvenlik | `Api/Controllers/AuthController.cs` (anonim `login`, `request-password-reset`, `confirm-password-reset`); `src/Aynesil.Api` içinde `AddRateLimiter` yok | Anonim kimlik doğrulama uçlarında IP veya hedef bazlı hız sınırı yok. Hesap kilitleme yalnızca tek hesaba yönelik denemeyi yavaşlatıyor | Birçok hesaba aynı parolayı deneme (password spraying) sınırsız. Parola sıfırlama isteği sınırsız tekrarlanabilir; e-posta sağlayıcısı bağlanınca kurbana toplu e-posta gönderilebilir. Sıfırlama token'ı da sınırsız denenebilir (token 256 bit, pratik risk düşük) | ASP.NET `RateLimiter` ekle: IP başına ve kullanıcı adı/e-posta başına sabit pencere (ör. giriş 10/dk, sıfırlama 3/saat). 429 için `ApiResponse` zarfı kullan |
| Y8 | Düşük | Genel (API) | güvenlik (bilgi sızıntısı) + i | `Api/ApiServiceCollectionExtensions.cs:73-90` | Model bağlama 400 yanıtı JSON çözümleyicinin ham mesajını döndürüyor. Canlı örnek (`POST /meetings`, geçersiz tarih): `"The JSON value could not be converted to Aynesil.Application.Features.Meetings.Commands.ScheduleMeetingCommand. Path: $.scheduledAt \| LineNumber: 0 \| BytePositionInLine: 42."` | İç tam tip adı ve namespace yapısı sızıyor; metin Türkçe zarfın içinde İngilizce | `AddJsonOptions(o => o.AllowInputFormatterExceptionMessages = false)` ayarla ya da factory'de `e.Exception != null` olan hatalara her zaman `Error.InvalidValue` metnini yaz |
| Y9 | Orta | Bağımlılıklar | güvenlik | `Application/Aynesil.Application.csproj:10` (AutoMapper 14.0.0, NU1903 yüksek); `Infrastructure/Aynesil.Infrastructure.csproj:36` (MailKit 4.10.0, NU1902 orta); `Application/DependencyInjection.cs:24` | NuGet denetimi iki bilinen açık bildiriyor. AutoMapper yalnızca DI'a kaydediliyor (`AddAutoMapper`), kodda `IMapper` kullanılmıyor. MailKit hiç kullanılmıyor | Kullanılmayan paketler açıklı sürümle dağıtıma giriyor | İki paketi ve `AddAutoMapper` kaydını kaldır. E-posta sağlayıcısı eklenirken MailKit güncel sürümle tekrar eklensin |
| Y10 | Orta | Finans | b | `views/finance/InvoiceDetailView.vue:24,59,226`; `services/finance.service.ts:154`; `Api/Controllers/PaymentsController.cs:326,726` (`VoidInvoiceRequest(int RowVersion)`); `InvoiceCommands.cs:192` | Fatura iptal formu iptal nedenini alıp gönderiyor; backend isteğinde de şemada da bu alan yok | Kullanıcının yazdığı iptal nedeni sessizce kayboluyor. Finansal düzeltmelerde gerekçe kaydı yok | Karar gerekiyor: ya alan formdan kaldırılır, ya da `finance.invoice`'a `void_reason` eklenir (migration + layer DDL; şema kararı sizde). `failTransaction`'ın `reason` alanı da aynı durumda (ekrandan çağrılmıyor) |
| Y11 | Orta | DB / RLS | güvenlik (savunma katmanı) | Canlı DB: `core.audit_log_default`, `core.activity_log_default`, `core.integration_log_default`, `media.viewing_log_default`, `core.outbox_event` | Partition'lı log tablolarının üst tablosunda RLS ve `log_read`/`log_write` politikası var. `_default` partition'larında ise RLS kapalı ve `aynesil_app` SELECT yetkisine sahip. Partition doğrudan sorgulanırsa üst tablonun politikası uygulanmıyor | Uygulama kodu partition'ları doğrudan sorgulamıyor. Ancak bir SQL enjeksiyonu ya da ileride yazılacak ham bir sorgu, tüm kiracıların audit kayıtlarını (eski/yeni satır değerleri) okuyabilir | `afterMigrate__app_role_grants.sql` içinde partition'lardan `aynesil_app` yetkisini geri al (`REVOKE`), ya da partition'larda da RLS'i aç. `drift.sql` değişmez kontrolüne partition'ları da ekle |
| Y12 | Düşük | Hukuk, Danışmanlık | a, b | `services/legal.service.ts:59` (`activateContract` gövdesiz; backend `[FromBody] ActivateStudentContractCommand` → 400); `legal.service.ts:97` (`{ fileId }`, backend `EvidenceFileId`); `consultancy.service.ts:190` (`getVisitHistory` `from`/`to`, backend `dateFrom`/`dateTo`) | Ekrana bağlı olmayan üç servis fonksiyonunun sözleşmesi backend'le uyuşmuyor | Şu an etkisi yok; ekrana bağlandığında 400/422 verir veya filtre çalışmaz | Gövdeyi `{}` (ya da `{ signedFileId }`) yap; alanı `evidenceFileId` olarak adlandır; parametreleri `dateFrom`/`dateTo` yap |
| Y13 | Düşük | Genel (API) | i | 63 controller satırı, ör. `CampusesController.cs:121`, `SchedulingController.cs:85`; `AuthController.cs:107,123`; `ConfirmPasswordResetCommand.cs:59,67` | Başarı mesajları İngilizce sabit metin ve `code` alanı boş. Geçersiz sıfırlama token'ı `UnauthorizedAccessException` ile 401 "Oturum açmanız gerekiyor." döndürüyor | i18n adımlarının dışında kalan yanıt metinleri. Sıfırlama ekranı eklendiğinde 401 yanıtı oturum yenileme ve login'e yönlendirme akışını tetikleyebilir | Mesajları `MsgKeys` ile kodla. Geçersiz token için kodlu 422 (`Auth.InvalidResetToken`) kullan |
| Y14 | Düşük | Frontend tipleri | f (tip) | `camp.service.ts:104,111`; `consultancy.service.ts:108`; `legal.service.ts:38,77`; `kpi.service.ts:85` (`Partial<…Dto>` payload); `types/api.types.ts:32` (`PagedQuery.search/sortBy`) | Oluşturma payload'ları DTO'dan türetildiği için tip düzeyinde `corporationId`, `assignedBy`, `authoredBy` gibi alanlara izin veriyor. Hiçbir ekran bu alanları göndermiyor; backend de yok sayıyor (sözleşme testi). D.5'teki "103 tanım kaldırıldı" ifadesi bu altı fonksiyonu kapsamıyor | Güvenlik etkisi yok; yanlış kullanıma açık tip | Her fonksiyona özel `Create…Payload` tipi tanımla. `search`/`sortBy` desteklemeyen listeler için `PagedQuery`'den ayrı bir taban tip kullan (#70, #82, #83 notları) |
| Y15 | Düşük (şüpheli) | Veli Portalı | yetki (ürün kararı) | `Api/Controllers/PortalController.cs:57-63`; `GetStudentSubResourcesQuery.cs:251-274`; `db/layer2_sped/01_students.sql:157` | Gelişim raporları ucu yalnızca portal erişimini doğruluyor. Diğer portal uçlarındaki `CanView*` bayrağına bakmıyor; tabloda "veliye görünür/yayımlandı" alanı da yok | Portal erişimi olan veli, öğrencinin tüm gelişim raporlarını (taslaklar dahil) görür. Bu istenen davranış olabilir; doğrulanmadı | Ürün kararı: bir `CanView*` bayrağı veya yayımlanma durumu gerekiyorsa şema kararı gerekir |

### Kapsam ve sınırlar

- Y2, Y3 ve Y4 kod okumasıyla doğrulandı. Canlı istismar denenmedi, çünkü yerel demo verisini değiştirirdi. Yerel DB'de şu an global rol ve global şablon yok; bu yüzden global kayıt üzerinden etki şimdilik teorik. Başka bir kiracının kaydını hedeflemek için o kaydın UUID'sini bilmek gerekiyor.
- Y2–Y4'ün ortak kök nedeni: DB değişmez kontrolü ve #86 composite FK çalışması yalnızca `corporation_id` kolonu olan tabloları kapsıyor. Canlı DB'de RLS'i kapalı ve `aynesil_app`'in okuyabildiği 28 tablo var; partition'lar dışında hiçbirinde `corporation_id` yok: `core.corporation` (Y1), çeviri tabloları, şablon alt tabloları (bölüm, madde, soru, seçenek), `role_permission`, `notification_delivery`, `user_identity` ve partition'lar. Bunlara yazan handler'ların hepsi tek tek incelenmedi. Kontrol edilenler: rol izni, değerlendirme bölüm/madde, program/bildirim şablonu/menü çevirisi, anket yanıtı, bildirim teslimatı (yalnızca ekleme).
- Yanıt alanı karşılaştırması `ProducesResponseType` attribute'u olmayan 114 uçta statik yapılamadı. Bu uçlar API duman testiyle çalışma anında kontrol edildi; 51 boş listede öğe alanları doğrulanamadı.
- Frontend'in bir buton için kontrol ettiği izin ile endpoint'in istediği iznin aynı olup olmadığı (ilk rapordaki sınır 3) bu doğrulamada da sistematik karşılaştırılmadı.
