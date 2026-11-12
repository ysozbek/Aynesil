# Backend ↔ Frontend Uyuşmazlık Raporu

> Tarih: 2026-11-12 · Kapsam: `src/` (.NET API), `db/`, `frontend/aynesil-web/src` · Kodda hiçbir değişiklik yapılmadı; build/test çalıştırılmadı (statik inceleme).
>
> **Tekrarlayan dört kök neden** bulguların çoğunu açıklıyor: (1) durum değerlerinin frontend'de PascalCase, DB'de küçük harf olması; (2) gövdesiz (NoContent) dönen PUT/POST'larda store'un `res.data` beklemesi; (3) backend'in zorunlu tuttuğu `corporationId`/`code` gibi alanların payload'da olmaması; (4) liste DTO'larında ad/etiket alanlarının bulunmaması (yalnızca id/kod).


## 1. Özet

Toplam **87** bulgu: Kritik: **20**, Yüksek: **35**, Orta: **19**, Düşük: **13**, Şüpheli: **0**.

| Modül | Kritik | Yüksek | Orta | Düşük | Şüpheli | Toplam |
|---|---|---|---|---|---|---|
| Finans | 3 | 7 | 4 | 1 | 0 | 15 |
| Kamplar | 3 | 4 | 0 | 1 | 0 | 8 |
| Sözleşme / Rıza | 2 | 3 | 2 | 1 | 0 | 8 |
| Toplantılar | 2 | 3 | 1 | 0 | 0 | 6 |
| Kameralar | 2 | 2 | 1 | 1 | 0 | 6 |
| Seans Planlama | 1 | 4 | 2 | 1 | 0 | 8 |
| İzin Yönetimi | 1 | 2 | 1 | 0 | 0 | 4 |
| Veli Portalı | 1 | 1 | 0 | 1 | 0 | 3 |
| Bildirimler | 1 | 0 | 1 | 1 | 0 | 3 |
| Kullanıcılar | 1 | 0 | 1 | 0 | 0 | 2 |
| Kurumlar | 1 | 0 | 1 | 0 | 0 | 2 |
| CRM (Aday) | 0 | 5 | 0 | 0 | 0 | 5 |
| Okul Danışmanlığı | 1 | 2 | 3 | 1 | 0 | 7 |
| Performans / KPI | 0 | 1 | 0 | 1 | 0 | 2 |
| Roller / İzinler | 0 | 1 | 0 | 0 | 0 | 1 |
| Genel | 1 | 0 | 2 | 1 | 0 | 4 |
| Değerlendirme | 0 | 0 | 0 | 1 | 0 | 1 |
| Platform | 0 | 0 | 0 | 1 | 0 | 1 |
| Programlar / Kayıtlar | 0 | 0 | 0 | 1 | 0 | 1 |
| **Toplam** | **20** | **35** | **19** | **13** | **0** | **87** |

## 2. Bulgular

| # | Önem | Modül | Kategori | Ekran / Akış | Frontend (dosya:satır) | Backend veya DB (dosya:satır) | Sorun | Kullanıcıya etkisi | Önerilen düzeltme |
|---|---|---|---|---|---|---|---|---|---|
| 1 ✅ | Kritik | Bildirimler | f | Bildirim tercihleri → Kaydet | types/notification.types.ts:153-159; stores/notificationPreference.store.ts:35-37; services/notification.service.ts:56 | Features/Notifications/Commands/NotificationTriggerCommands.cs:105 (`Guid CorporationId`) ve validator (`RuleFor(x => x.CorporationId).NotEmpty()`); Api/Controllers/NotificationsController.cs:68-73 (yalnızca UserId set ediliyor) | Backend `corporationId` zorunlu tutuyor; frontend yalnızca `preferences` gönderiyor. | Tercihler kaydedilemez (422). | Payload'a `corporationId` ekle veya controller'da JWT'den doldur. |
| 2 | Kritik | Finans | f | Paket tanımı düzenle | views/finance/PackageDefinitionFormView.vue:70-80; types/finance.types.ts:60-70 | Features/Finance/Commands/PackageDefinitionCommands.cs:75 (UpdatePackageDefinitionCommand: `string Code` zorunlu) | Güncelleme payload'ında `code` yok (backend'in tanımadığı `description` var). Non-nullable `Code` eksik olduğundan istek model doğrulamasında 400 alır. | Paket tanımı güncellenemez. | Payload'a `code` ekle, `description`ı kaldır (ya da backend'e ekle). |
| 3 | Kritik | Finans | f | Promosyon düzenle | views/finance/PromotionListView.vue:56-61; types/finance.types.ts:475-483 | Features/Finance/Commands/PromotionCommands.cs:71,86 (`RuleFor(x => x.Code).NotEmpty()`) | Güncelleme payload'ı `code` göndermiyor; backend zorunlu tutuyor. Gönderilen `rowVersion` ise komutta yok. | Promosyon güncelleme her zaman reddedilir. | Payload'a `code` ekle. |
| 4 | Kritik | Finans | g | Kredi ver / Kredi düzelt / İade oluştur / Burs ekle butonları | views/finance/CreditLedgerView.vue:143,150 (`credit:grant`, `credit:adjust`); views/finance/PaymentDetailView.vue:80 (`refund:create`); views/finance/ScholarshipListView.vue:105 (`scholarship:create`) | Shared/Constants/Permissions.cs:409-410 (`credit_ledger:grant`, `credit_ledger:adjust`), :435 (`refund:request`), :450 (`scholarship:grant`) | Frontend'in kontrol ettiği dört izin kodu backend'de ve seed'de yok; doğru kodlar farklı. | Hiçbir kullanıcıda (admin dahil) bu butonlar görünmez; kredi verme/düzeltme, iade ve burs ekleme arayüzden yapılamaz. | İzin kodlarını Permissions.cs'teki değerlerle değiştir. |
| 5 ✅ | Kritik | Kameralar | f | Kamera detayı → Dersliğe ata | views/cameras/CameraDetailView.vue:28; types/camera.types.ts:146-152; services/camera.service.ts:58,66 | Api/Controllers/CameraController.cs:102-106,127-131,239-240 (`AssignCameraToRoomRequest(Guid CorporationId, Guid RoomId)`); Features/Cameras/Commands/CameraAssignmentCommands.cs:20,98 (`CorporationId` NotEmpty) | Frontend yalnızca `{ roomId }` / `{ sessionId }` gönderiyor; backend gövdede `corporationId` zorunlu tutuyor. | Kamera–derslik (ve seans) ataması her zaman 422 ile reddedilir. | Payload'a `corporationId` ekle. |
| 6 ✅ | Kritik | Kameralar | f | İzleme yetkileri → Yetki ver | views/cameras/ViewingAuthorizationsView.vue:80; types/camera.types.ts:154-162; services/camera.service.ts:82 | Features/Cameras/Commands/ViewingCommands.cs:20 (`Guid CorporationId`) ve validator (`RuleFor(x => x.CorporationId).NotEmpty()`) | Payload'da zorunlu `corporationId` yok. | Veliye izleme yetkisi verilemez (422). | Payload'a `corporationId` ekle. |
| 7 | Kritik | Kamplar | a | Kamp dönemi → Kayıt listesi (CampEnrollmentView) | services/camp.service.ts:56-59; stores/camp.store.ts:92 | Api/Controllers/CampsController.cs:168 (GET /api/camps/enrollments), :184 (aynı yol yalnızca POST) | Frontend `GET /camps/periods/{periodId}/enrollments` çağırıyor; backend'de bu yolun GET'i yok (yalnızca POST). Liste endpoint'i `GET /camps/enrollments?campPeriodId=…`. | 405 döner; store hatayı yutar, kayıt tablosu her zaman "veri yok" gösterir. | Servisi `GET /camps/enrollments` + `campPeriodId` query parametresine çevir (ya da backend'e GET route'u ekle). |
| 8 ✅ | Kritik | Kamplar | a, f | Kamp dönemi → Öğrenci kaydet | services/camp.service.ts:64-65; views/camps/CampEnrollmentView.vue:155 (`{ studentId }`); types/camp.types.ts:252-255 | Api/Controllers/CampsController.cs:184 (POST periods/{periodId}/enrollments); Features/Camps/Commands/CampEnrollmentCommands.cs:12,19 (CorporationId NotEmpty) | Frontend `POST /camps/periods/{id}/enroll` çağırıyor, backend yolu `/enrollments`. Ayrıca payload'da zorunlu `corporationId` yok. | Kayıt işlemi 404 ile hiç çalışmıyor; yol düzeltilse bile 422 alır. | Yolu `/camps/periods/{id}/enrollments` yap, payload'a `corporationId` ekle. |
| 9 ✅ | Kritik | Kamplar | f | Kamp detayı → Dönem ekle | types/camp.types.ts:245-250; views/camps/CampDetailView.vue:175,181 | Features/Camps/Commands/CampPeriodCommands.cs:12,25 (`RuleFor(x => x.CorporationId).NotEmpty()`) | Backend `corporationId` zorunlu tutuyor; form/payload bu alanı hiç göndermiyor (Guid.Empty olarak bağlanır). | Dönem oluşturma her zaman 422 Validation failed ile reddedilir. | Payload'a `corporationId` ekle veya handler'da kampın corporation'ından türet. |
| 10 ✅ | Kritik | Kullanıcılar | e | Kullanıcı listesi/detayı → Askıya al / Etkinleştir | views/users/UsersView.vue:139-140; views/users/UserDetailView.vue:71,157-158; stores/user.store.ts:93-94,100-101 | db/layer1_core/04_identity_access.sql:16 (`status in ('invited','active','suspended','disabled')`); Domain/Modules/Iam/Entities/UserAccount.cs:88,99; Features/Users/Dtos/UserMappings.cs:10,14 | Frontend durumu `'Active'`/`'Suspended'` (PascalCase) ile karşılaştırıyor; backend küçük harf döndürüyor. | Listede satır aksiyonları hiç çıkmaz; detayda buton hep "Etkinleştir" görünür ve aktif kullanıcıya activate çağrılır — kullanıcı arayüzden askıya alınamaz. | Karşılaştırmaları ve store'daki atamaları küçük harfe çevir. |
| 11 ✅ | Kritik | Kurumlar | e | Kurum listesi → Pasifleştir / Etkinleştir düğmesi | views/corporations/CorporationListView.vue:165,255,258; stores/corporation.store.ts:100-101,107-108 | db/layer1_core/02_tenancy.sql:16 (`status in ('active','suspended','closed')`); Domain/Modules/Core/Entities/Corporation.cs:64,75 | Frontend `row.status === 'Active'` kontrol ediyor; backend `active` döndürüyor. Store ayrıca yerel durumu DB'de olmayan `'Inactive'` yapıyor. | Aktif kurumda düğme "Etkinleştir" olarak görünür ve activate çağırır; kurum arayüzden pasifleştirilemez. | Küçük harf DB değerlerini (`active`/`suspended`) kullan. |
| 12 | Kritik | Seans Planlama | f | Derslikler → Derslik düzenle | views/scheduling/RoomListView.vue:94-99; types/scheduling.types.ts:49-54 | Features/Scheduling/Commands/RoomCommands.cs:67 (UpdateRoomCommand.Code), :81 (`RuleFor(x => x.Code).NotEmpty()`) | Güncelleme payload'ı `code` göndermiyor (name, capacity, isVirtual, rowVersion); backend `Code` alanını zorunlu tutuyor. `isVirtual` ise backend komutunda yok, yok sayılır. | Derslik düzenleme her zaman 400/422 ile reddedilir. | Payload'a `code` (ve varsa roomTypeId/meetingUrl) ekle; `isVirtual`ı kaldır. |
| 13 ✅ | Kritik | Sözleşme / Rıza | e | Sözleşme detayı → Gönder / İmzala / Feshet butonları | views/legal/ContractDetailView.vue:85,93,101 | db/layer2_sped/08_legal.sql:62 (`status in ('draft','sent','signed','active','expired','terminated')`) | Frontend durumu `'Draft'`, `'Sent'`, `'Active'` (PascalCase) ile karşılaştırıyor; backend/DB küçük harf döndürüyor. | İş akışı butonları hiçbir zaman görünmez; sözleşme arayüzden gönderilemez, imzalanamaz, feshedilemez. | Karşılaştırmaları küçük harfli DB değerlerine çevir. |
| 14 | Kritik | Sözleşme / Rıza | e, f | Sözleşme imzala modalı | views/legal/ContractDetailView.vue:21,52-53,191-192; types/legal.types.ts:242-246 | Features/Legal/Commands/StudentContractCommands.cs:166 (`string SignedByName`, `string SignatureMethod`), :176 (`["wet","e_sign","click_wrap"]`); db/layer2_sped/08_legal.sql:65 | Form `signatureMethod` için `Manual`/`Electronic` gönderiyor (geçerli değerler wet/e_sign/click_wrap) ve zorunlu `signedByName` alanını hiç göndermiyor. | İmzalama isteği her zaman doğrulamadan döner (buton görünür hale getirilse bile). | Seçenekleri DB değerleriyle değiştir, `signedByName` alanını forma ekle. |
| 15 ✅ | Kritik | Toplantılar | e | Takip görevini tamamla butonu (toplantı detayı ve takip listesi) | views/meetings/MeetingDetailView.vue:147; views/meetings/FollowUpListView.vue:36,65,93,108-111 | Features/Meetings/Commands/MeetingCommands.cs:499 + validator (`["open","in_progress","done","cancelled"]`); db/layer2_sped/10_ops.sql:58 | Frontend `status: 'completed'` gönderiyor ve `pending/completed` ile filtreliyor; geçerli değerler `open/in_progress/done/cancelled`. | Tamamla işlemi 422 alır; "Bekliyor"/"Tamamlandı" filtreleri boş döner, biten görevler gecikmiş görünür. | `completed`→`done`, `pending`→`open`. |
| 16 ✅ | Kritik | Toplantılar | f | Toplantı detayı → Sonuç ekle / Takip görevi ekle | views/meetings/MeetingDetailView.vue:132,138; types/meeting.types.ts:159-162,169-173; services/meeting.service.ts:73,81 | Features/Meetings/Commands/MeetingCommands.cs:325,414 ve validator'lar (`RuleFor(x => x.CorporationId).NotEmpty()`); Api/Controllers/MeetingsController.cs:148,175 (yalnızca MeetingId set ediliyor) | Backend `corporationId` zorunlu tutuyor; frontend payload'larında yok ve controller da doldurmuyor (katılımcı ekleme servisi için de aynı, :62). | Sonuç ve takip görevi ekleme her zaman 422 ile reddedilir. | Payload'a `corporationId` ekle veya handler'da toplantıdan türet. |
| 17 | Kritik | Veli Portalı | c | Çocuklarım → çocuk detayına geçiş; portal panosu öğrenci seçimi | types/portal.types.ts:9-23; views/portal/MyChildrenView.vue:23,25,59; views/portal/PortalDashboardView.vue:30-32,129 | Features/Students/Dtos/StudentProjection.cs:238-251 (PortalStudentDto: `StudentId`, `BirthDate`, `CanViewPlan`, `CanViewAttendance`…) | Frontend öğrenci kimliğini `id` alanından okuyor; backend `studentId` gönderiyor. `dateOfBirth`↔`birthDate`, `canViewGoals`/`canViewDocuments`/`enrollmentStatus` backend'de yok (`canViewPlan`, `canViewAttendance`, `canViewCamera` var). | Çocuk kartına tıklayınca `studentId: undefined` ile yönlenir; pano ilk öğrenciyi seçemez — portalın tüm öğrenci ekranları açılmaz. Hedefler ve Belgeler sekmeleri hiç görünmez, doğum tarihi boş. | TS tipini backend DTO'suna göre yeniden yaz (`studentId`, `birthDate`, `canViewPlan`…). |
| 18 | Kritik | İzin Yönetimi | f | İzin bakiyeleri → Devret (carry forward) | views/leave/LeaveBalanceView.vue:61-63; types/leave.types.ts:195-200; services/leave.service.ts:91 | Features/Leaves/Commands/LeaveCommands.cs:389 (`Guid EducatorId`), :401 (`RuleFor(x => x.EducatorId).NotEmpty()`) | Backend devri eğitmen bazında yapıyor ve `educatorId` zorunlu; frontend payload'ında bu alan yok. | Devir işlemi her zaman 422 ile reddedilir. | Forma eğitmen seçimi ekle ya da backend'e toplu devir desteği ekle. |
| 19 | Yüksek | CRM (Aday) | c | Aday listesi → Kaynak, Aşama, Durum sütunları; Pipeline kartları; LeadPicker | views/crm/LeadsView.vue:92-94,221,228; views/crm/PipelineView.vue:207; components/pickers/LeadPicker.vue:44; types/crm.types.ts:3 | Features/Leads/Dtos/LeadListItemDto.cs:4 (yalnızca SourceCode, StatusCode, PipelineStageCode) | Frontend `sourceName`, `statusName`, `pipelineStageName` okuyor; backend yalnızca `*Code` alanlarını gönderiyor. | Üç sütun boş, rozetler görünmez. | DTO'ya ad alanlarını ekle ya da `*Code` + ref-data etiketi kullan. |
| 20 | Yüksek | CRM (Aday) | c | Aday detayı → durum/aşama rozetleri, kaynak, aktivite türü, durum geçmişi | views/crm/LeadDetailView.vue:214-218,319,360,422-428; views/crm/LeadConvertView.vue:206-208; types/crm.types.ts:25,38,52 | Features/Leads/Dtos/LeadDto.cs:4; LeadActivityDto.cs:4 (ActivityTypeCode); LeadStatusHistoryDto.cs:4 (StatusCode, PipelineStageCode, ChangedBy) | `statusName/pipelineStageName/sourceName`, `activityTypeName`, geçmişte `previousStatusCode/newStatusCode/newPipelineStageCode/changedByName` backend'de yok. | Rozetler ve kaynak boş; aktivite satırı "undefined · …" ile başlar; durum geçmişi her satırda "— → —" gösterir. | TS tiplerini backend alan adlarına (statusCode, activityTypeCode…) çevir veya DTO'ları genişlet. |
| 21 | Yüksek | CRM (Aday) | c | CRM panosu ve Pipeline özeti | views/crm/CrmDashboardView.vue:33-34,108,140; views/crm/PipelineView.vue:169-170; types/crm.types.ts:78,85 | Features/Leads/Dtos/PipelineSummaryDto.cs:7 (TotalConverted, TotalLost), :15 (StageId, StageCode, Count) | Frontend `convertedLeads`, `lostLeads`, `stages[].stageName/sortOrder` okuyor; backend `totalConverted`, `totalLost` gönderiyor, aşama adı yok. | Dönüşen/Kaybedilen sayıları 0 veya boş; aşama adları boş. | Alan adlarını eşitle. |
| 22 | Yüksek | CRM (Aday) | c | Dönüşüm raporu (ReportsView) | views/crm/ReportsView.vue:111,132,134,140; types/crm.types.ts:93,103 | Features/Leads/Dtos/ConversionReportDto.cs:7 (TotalConverted), :17 (SourceCode, TotalLeads, Converted, ConversionRate) | Frontend `convertedLeads`, `bySource[].sourceName/total/rate` okuyor; backend `totalConverted`, `sourceCode/totalLeads/conversionRate` gönderiyor. | Dönüşen aday sayısı boş; kaynak bazlı satırlarda ad boş, "x/undefined", oran NaN. | Alan adlarını eşitle. |
| 23 | Yüksek | CRM (Aday) | c | Aktiviteler listesi ve görüşmeler | views/crm/ActivitiesView.vue:94; types/crm.types.ts:38,63 (InterviewDto.completedAt) | Features/Leads/Dtos/LeadActivityDto.cs:4; InterviewDto.cs:4 (ConductedAt) | `activityTypeName` yok (backend `activityTypeCode`); görüşmede `completedAt` yok (backend `conductedAt`). | Aktivite türü hep "—"; görüşme tamamlanma tarihi boş. | Alan adlarını eşitle. |
| 24 | Yüksek | Finans | b, c | Finans panosu → Gelir ve paket raporu kartları | views/finance/FinanceDashboardView.vue:34-35,51-52,157-196; types/finance.types.ts:496-543 | Api/Controllers/PaymentsController.cs:643-668 (`periodStart`, `periodEnd`); Features/Finance/Dtos/FinanceDtos.cs:257 (PaymentCount), :267 (MethodName), :273 (PackageReportDto), :285 (UnitsSold) | Frontend `from`/`to` gönderiyor, backend `periodStart`/`periodEnd` bekliyor (bağlanmayınca 0001-01-01 kalır). Ayrıca okunan alanlar farklı: `totalTransactions`↔`paymentCount`, `byMethod[].paymentMethodLabel`↔`methodName`, `packageReport.totalPackagesSold/totalRevenue` backend'de yok, `topPackages[].soldCount`↔`unitsSold`. | Gelir toplamı hep 0; işlem sayısı, yöntem adları, satılan paket sayısı ve paket geliri boş görünür. | Parametre adlarını ve TS tiplerini backend DTO'larıyla eşitle. |
| 25 | Yüksek | Finans | b, c | Fatura detayı → Satır indirimi | views/finance/InvoiceDetailView.vue:52,166,215; types/finance.types.ts:195,253-258 | Features/Finance/Dtos/FinanceDtos.cs:139 (InvoiceLineDto); Features/Finance/Commands/InvoiceCommands.cs:55 (AddInvoiceLineCommand) | `discountAmount` ne istek komutunda ne yanıt DTO'sunda var. | Girilen satır indirimi kaydedilmez, indirim sütunu boş görünür. | Alanı backend'e ekle ya da formdan kaldır. |
| 26 | Yüksek | Finans | c | Ödeme listesi/detayı → Fatura no ve Ödeme yöntemi | views/finance/PaymentListView.vue:57,59; views/finance/PaymentDetailView.vue:101,109; views/finance/FinanceDashboardView.vue:251; types/finance.types.ts:262,280 | Features/Finance/Dtos/FinanceDtos.cs:151 (PaymentDto), :167 (PaymentListItemDto) — yalnızca InvoiceId, PaymentMethodId | Frontend `invoiceNo` ve `paymentMethodLabel` okuyor; backend bu alanları göndermiyor. | İki sütun boş / "—". | DTO'lara ekle veya id'den çöz. |
| 27 | Yüksek | Finans | c | Paket tanımı listesi → Tür, Program, Geçerlilik günü sütunları | views/finance/PackageDefinitionListView.vue:63,64,66; types/finance.types.ts:27 | Features/Finance/Dtos/FinanceDtos.cs:25 (PackageDefinitionListItemDto) | `packageTypeLabel`, `programName`, `validityDays` liste DTO'sunda yok. | Üç sütun boş. | Liste DTO'sunu genişlet. |
| 28 | Yüksek | Finans | c | Promosyon listesi → Kullanım sayısı; düzenlemede maks. kullanım | views/finance/PromotionListView.vue:83,133; types/finance.types.ts:432,447 | Features/Finance/Dtos/FinanceDtos.cs:233 (PromotionDto), :245 (PromotionListItemDto) | `redemptionCount` hiçbir DTO'da yok; `maxRedemptions` liste DTO'sunda yok. | Kullanım sütunu boş; düzenle formunda maks. kullanım boş gelir ve kaydedilirse sıfırlanır. | DTO'lara ekle. |
| 29 | Yüksek | Finans | c | Burs listesi → Burs türü ve para birimi | views/finance/ScholarshipListView.vue:88,131,133; types/finance.types.ts:392 | Features/Finance/Dtos/FinanceDtos.cs:221 (ScholarshipListItemDto) | `scholarshipTypeLabel` ve `currency` backend DTO'sunda yok. | Tür sütunu hep "—", tutarın yanında "undefined" yazabilir. | DTO'ya ekle veya view'den kaldır. |
| 30 | Yüksek | Finans | c | Kredi defteri → Seans ve Kaydeden sütunları | views/finance/CreditLedgerView.vue:88,90,224; types/finance.types.ts:136 | Features/Finance/Dtos/FinanceDtos.cs:79 (CreditLedgerEntryDto: SessionId, CreatedBy) | `sessionTitle` ve `recordedByName` backend'de yok. | İki sütun boş. | DTO'ya ekle. |
| 31 | Yüksek | Kameralar | c | Yeni kamera kaydet | views/cameras/CameraFormView.vue:40; stores/camera.store.ts:71-73; services/camera.service.ts:44 | Api/Controllers/CameraController.cs:43-47; Features/Cameras/Commands/CameraCommands.cs:11 (`IRequest<Guid>`) | Backend yalnızca Guid döner; frontend `CameraDto` bekleyip `result.id` ile yönlendiriyor (aynı durum yetki oluşturmada da var, CameraController.cs:176-180). | Kamera oluşur ama detay sayfasına `undefined` id ile gidilir. | Dönen değeri id olarak kullan. |
| 32 | Yüksek | Kameralar | c | Kamera düzenle | stores/camera.store.ts:82-85; views/cameras/CameraFormView.vue:31; services/camera.service.ts:47 | Api/Controllers/CameraController.cs:56-60 (NoContentResult) | Gövdesiz yanıt; store `!res.data` ise "Kamera güncellenemedi." fırlatıyor. Ayrıca payload'daki `code` komutta yok. | Güncelleme kaydedilir ama hata gösterilir. | `res.success` kontrol et, sonra yeniden çek. |
| 33 | Yüksek | Kamplar | a | Kamp detayı → Eğitmenler bölümü | services/camp.service.ts:101-102; views/camps/CampDetailView.vue:190 | Api/Controllers/CampsController.cs:374 (GET /api/camps/educators?campId=), :382 (aynı yol yalnızca POST) | Frontend `GET /camps/{campId}/educators` çağırıyor; backend'de GET yalnızca `/camps/educators` (campId query). | 405; eğitmen listesi her zaman boş. | Servisi `GET /camps/educators?campId=` yap. |
| 34 | Yüksek | Kamplar | a, d | Kamp dönemi → Özet kartları | services/camp.service.ts:84-85; views/camps/CampEnrollmentView.vue:24-58 | Api/Controllers/CampsController.cs:98-102 (GET /api/camps/{id}/enrollment-summary); Features/Camps/Queries/CampReportQueries.cs:40 (IReadOnlyList döner) | Frontend `GET /camps/periods/{periodId}/enrollment-summary` çağırıyor; backend yolu kamp bazlı (`/camps/{campId}/enrollment-summary`) ve tek obje değil dönem listesi döner. | 404; özet kartları (kayıtlı/bekleme/tamamlanan/ayrılan) hiç görünmez. | Kamp id'si ile çağırıp listeden ilgili dönemi seç veya backend'e dönem bazlı endpoint ekle. |
| 35 | Yüksek | Kamplar | c | Kamp düzenle (CampFormView) | stores/camp.store.ts:73-76; services/camp.service.ts:45-46 | Api/Controllers/CampsController.cs:54-58 (NoContentResult) | Backend PUT /camps/{id} gövdesiz (`data` null) döner; store `!res.data` ise hata fırlatıyor. | Güncelleme veritabanına yazılır ama kullanıcı "Kamp güncellenemedi." hatası görür, detay sayfasına yönlenmez; tekrar denerse rowVersion çakışması alır. | Store'da yalnızca `res.success` kontrol et ve ardından `fetchCamp(id)` çağır (ya da backend güncel CampDto dönsün). |
| 36 ✅ | Yüksek | Kamplar | e | Kamp dönemi → Kayıt satırı durum rozeti ve aksiyon butonları | views/camps/CampEnrollmentView.vue:87,92,148-149 (`'Waitlist'`, `'Enrolled'`, `'Completed'`, `'Withdrawn'`) | db/layer2_sped/11_camps.sql:43 (`check (status in ('enrolled','waitlist','withdrawn','completed'))`); Features/Camps/Queries/CampEnrollmentQueries.cs:43 | Frontend durumları PascalCase karşılaştırıyor; backend/DB küçük harf döndürüyor. | "Bekleme listesinden al" ve "Kaydı sil/çek" butonları hiç görünmez, rozet rengi varsayılana düşer. | Karşılaştırmaları küçük harfli değerlere çevir. |
| 37 | Yüksek | Okul Danışmanlığı | c | Kurum düzenle (InstitutionFormView) | views/consultancy/InstitutionFormView.vue:48; stores/consultancy.store.ts:87-90; services/consultancy.service.ts:50 | Api/Controllers/ConsultancyController.cs:52-61 (NoContentResult) | PUT /consultancy/institutions/{id} gövdesiz döner; store `!res.data` ise "Kurum güncellenemedi." hatası fırlatıyor. | Kayıt güncellenir ama kullanıcı hata görür ve sayfada kalır. | Store'da yalnızca `res.success` kontrol edip `fetchInstitution(id)` çağır. |
| 38 ✅ | Yüksek | Okul Danışmanlığı | e | Danışmanlık panosu → aktif planlar, açık takip görevleri, yaklaşan ziyaretler | views/consultancy/ConsultancyDashboardView.vue:17,20,25-27,34-37,46,48 | db/layer2_sped/12_consultancy.sql:31,45 (`draft/active/completed/cancelled`; `planned/completed/cancelled`); db/migrations/V26__consultancy_agreements_and_followups.sql:63 (`pending/in_progress/completed/cancelled`) | Pano `status: 'Active'` ve `status: 'Open'` filtreleriyle sorgu atıyor ve `'Open'`/`'InProgress'`/`'Completed'` ile karşılaştırıyor; DB değerleri küçük harf ve takip görevinde `open` diye bir durum yok (`pending`). | Aktif plan ve açık görev sayıları 0, listeler boş; rozet renkleri varsayılana düşer. | `active`, `pending`, `in_progress`, `planned` değerlerini kullan. |
| 39 | Yüksek | Performans / KPI | b, f | KPI panosu (yönetici + üst yönetim kartları, sıralama) | views/kpi/KpiDashboardView.vue:16,53-58; types/kpi.types.ts:243-250; services/kpi.service.ts:94,99,116 | Features/PerformanceKpi/Queries/DashboardQueries.cs:152-156,257-260 (`DateOnly PeriodStart`, `PeriodEnd` zorunlu); TrendAndRankingQueries.cs:131 (`string KpiCode`, `PeriodStart`, `PeriodEnd` zorunlu) | Frontend yalnızca `corporationId` + `periodType` (Monthly/Quarterly/Annual) gönderiyor; backend `periodType` tanımıyor, `periodStart`/`periodEnd` (ve sıralama için `kpiCode`) bekliyor. | Pano kartları ve sıralama tablosu veri getirmez (400 ya da boş dönem). | Seçilen dönem tipinden `periodStart/periodEnd` hesaplayıp gönder; sıralama için `kpiCode` seç. |
| 40 | Yüksek | Roller / İzinler | h | Rol detayı izin matrisi; İzin gezgini | services/permission.service.ts:16-20 (`pageSize: 500`); stores/permission.store.ts (loadCatalog) | Shared/PagedQuery.cs:18-22 (PageSize en fazla 200'e kırpılır); Api/Controllers/PermissionsController.cs (GET, `PageSize = pageSize`); Shared/Constants/Permissions.cs (327 izin) | Katalog tek istekte 500 kayıt istiyor; backend sayfa boyutunu sessizce 200'e düşürüyor ve frontend sonraki sayfaları çekmiyor. | 327 izinden yalnızca ilk 200'ü görünür; kalan izinler rollere arayüzden atanamaz/kaldırılamaz. | Tüm sayfaları döngüyle çek veya sayfalamasız bir katalog endpoint'i ekle. |
| 41 | Yüksek | Seans Planlama | c | Yoklama panosu → Özet kartları | types/scheduling.types.ts:361-371; views/scheduling/AttendanceDashboardView.vue:77,81,85 | Features/Scheduling/Dtos/SchedulingDtos.cs:184-193 (PresentCount, AbsentCount, LateCount, ExcusedCount, LeftEarlyCount) | Frontend `present/absent/late/excused/leftEarly` okuyor; backend `presentCount/absentCount/lateCount/excusedCount/leftEarlyCount` gönderiyor. | Katıldı/Gelmedi/Geç kaldı kartları boş görünür. | TS tipini ve view'i `*Count` adlarına çevir. |
| 42 | Yüksek | Seans Planlama | c | Seans listesi "Tür" sütunu; Seans detayı tür/kampüs/not yazarı/devamsızlık nedeni | views/scheduling/SessionListView.vue:81; views/scheduling/SessionDetailView.vue:246,248,400,461; views/scheduling/AttendanceDashboardView.vue:124; types/scheduling.types.ts:224,234,248,272 | Features/Scheduling/Dtos/SchedulingDtos.cs:96 (SessionDto), :120 (SessionListItemDto), :160 (SessionNoteDto: AuthoredBy), :172 (AttendanceDto: RecordedBy) | Frontend `sessionTypeLabel`, `campusName`, `note.authorName`, `reasonLabel` alanlarını okuyor; backend DTO'larında bu alanlar yok (yalnızca id'ler: sessionTypeId, campusId, authoredBy, reasonId). | Seans listesinde Tür sütunu boş; detayda tür "—", kampüs boş, not yazarı boş, devamsızlık nedeni hep "—". | Backend projeksiyonlarına etiket/ad alanlarını ekle ya da frontend'de id→etiket eşlemesi yap (RecurringScheduleListView.vue:66-70 örneğindeki gibi). |
| 43 | Yüksek | Seans Planlama | c | Derslik listesi "Kampüs" sütunu; RoomPicker alt etiketi | views/scheduling/RoomListView.vue:67; components/pickers/RoomPicker.vue:41; types/scheduling.types.ts:9,22 | Features/Scheduling/Dtos/SchedulingDtos.cs:8 (RoomDto), :23 (RoomListItemDto) — CampusName yok | Frontend `campusName` okuyor; backend yalnızca `campusId` gönderiyor. | Kampüs sütunu boş. | DTO'ya CampusName ekle veya frontend'de kampüs listesinden eşle. |
| 44 | Yüksek | Seans Planlama | d, h | Planlama panosu → "Bugünkü seanslar" | views/scheduling/SchedulingDashboardView.vue:20,26-32 | Features/Scheduling/Queries/GetSessionsQuery.cs:43-44 (`StartsAt >= From && StartsAt <= To`, DateTimeOffset) | Frontend `from` ve `to` için aynı gün değerini saat olmadan (`YYYY-MM-DD`) gönderiyor; backend bunları gece yarısı anı olarak karşılaştırıyor (from=to=00:00). | Bugünkü seanslar listesi pratikte her zaman boş görünür. | `to` için gün sonunu (ya da ertesi günü) gönder; veya backend'de `To`yu gün sonuna genişlet. |
| 45 | Yüksek | Sözleşme / Rıza | a | Hukuki raporlar sayfası (LegalReportsView) | services/legal.service.ts:103,106,109; views/legal/LegalReportsView.vue:82-84 | Api/Controllers/LegalController.cs:368,379,390 (`reporting/contracts`, `reporting/consents`, `reporting/signatures`) | Frontend `/legal/reports/*` çağırıyor; backend yolu `/legal/reporting/*`. | Üç rapor da 404 alır, sayfa boş kalır. | Servis yollarını `/legal/reporting/...` yap. |
| 46 | Yüksek | Sözleşme / Rıza | c | Sözleşme imzala sonrası (store) | stores/contract.store.ts:89-91; services/legal.service.ts:57 | Api/Controllers/LegalController.cs (POST contracts/{id}/sign → NoContentResult); StudentContractCommands.cs:166 (`IRequest`) | Endpoint gövdesiz döner; store `!res.data` ise "İmzalanamadı." fırlatıyor. | Başarılı imzada bile hata mesajı gösterilir. | `res.success` kontrol edip sözleşmeyi yeniden çek. |
| 47 ✅ | Yüksek | Sözleşme / Rıza | e | Hukuk panosu sayaçları; bekleyen imzalar | views/legal/LegalDashboardView.vue:19,22,25,30; stores/signature.store.ts:23 | db/layer2_sped/08_legal.sql:62,87 (küçük harf durumlar) | Sayaçlar `'Sent'`, `'Active'`, `'Granted'` ile filtreliyor; veriler küçük harf. | "İmza bekleyen", "Aktif sözleşme", "Verilen rıza", "Süresi dolacak" sayıları hep 0. | Küçük harfli değerlerle karşılaştır. |
| 48 | Yüksek | Toplantılar | c | Toplantı düzenle / tamamla / iptal | stores/meeting.store.ts:78-81,101-103,112-114; views/meetings/MeetingFormView.vue:49; views/meetings/MeetingDetailView.vue:113,117 | Api/Controllers/MeetingsController.cs:54-58,80-83,92-95 (NoContentResult) | Üç endpoint gövdesiz döner; store `!res.data` ise hata fırlatıyor. | İşlem başarılı olduğu halde "güncellenemedi / tamamlanamadı / iptal edilemedi" hatası görünür, ekran yenilenmez. | `res.success` kontrol edip `fetchMeeting(id)` çağır. |
| 49 | Yüksek | Toplantılar | c, d | Yeni toplantı kaydet (MeetingFormView) | views/meetings/MeetingFormView.vue:57-65; stores/meeting.store.ts:67-69; services/meeting.service.ts:45 | Api/Controllers/MeetingsController.cs:41-45; Features/Meetings/Commands/MeetingCommands.cs:12 (`IRequest<Guid>`) | Backend yalnızca yeni kaydın Guid'ini döner; frontend `MeetingDto` bekleyip `created.id` okuyor. | Toplantı oluşur ama yönlendirme `id: undefined` ile yapılır (detay sayfası açılmaz / router hatası). | `res.data`yı id olarak kullan. |
| 50 | Yüksek | Toplantılar | d | Toplantı tarih-saat alanı | views/meetings/MeetingFormView.vue:52,61,81,125 (`datetime-local` değeri olduğu gibi gönderiliyor; düzenlemede `slice(0,16)`) | Features/Meetings/Commands/MeetingCommands.cs:12,92 (`DateTimeOffset? ScheduledAt`) | Saat dilimi bilgisi olmadan gönderilen değer sunucu saat dilimine (konteynerde UTC) göre yorumlanır; düzenleme formu ise UTC değerin ilk 16 karakterini yerel saat sanıyor. | Toplantı saati listede/detayda 3 saat kaymış görünür; her düzenlemede saat tekrar kayar. | Gönderirken `new Date(v).toISOString()`, forma yüklerken yerel saate çevir (SessionFormView.vue:85,98 örneği). |
| 51 | Yüksek | Veli Portalı | h | Çocuk detayı → Seanslar, Devam, Belgeler sekmeleri; portal bildirimleri | services/portal.service.ts:48-52,57-61,71-75,100-103; stores/parentPortal.store.ts:106,116,136,186; views/portal/tabs/ChildSessionsTab.vue; views/portal/tabs/ChildDocumentsTab.vue; views/portal/PortalNotificationsView.vue | Api/Controllers/PortalController.cs:67-74,77-84,94-98,127-133 (sorgular `IReadOnlyList<…>` döner) | Frontend `PaginatedResult` (`items`, `totalCount`) bekliyor; backend düz dizi döndürüyor. Ayrıca `from`/`to`/`status` filtreleri backend'de yok. | Listeler boş görünür veya `.items` okunurken hata oluşur; sayfalama ve tarih filtresi çalışmaz. | Store'da diziyi doğrudan kullan ya da backend'i PaginatedResult döndürecek şekilde değiştir. |
| 52 | Yüksek | İzin Yönetimi | b | İzin raporları → Trend sekmesi | types/leave.types.ts:147-152; services/leave.service.ts:101; views/leave/LeaveReportsView.vue (trend sekmesi) | Features/Leaves/Queries/LeaveQueries.cs:413-418 (`FromYear`, `ToYear`) | Frontend `periodYear`/`educatorId` gönderiyor; backend `fromYear`/`toYear` bekliyor (gönderilmeyince 0). | Trend raporu boş döner. | `fromYear`/`toYear` gönder. |
| 53 | Yüksek | İzin Yönetimi | c | İzin talebi düzenle | stores/leave.store.ts:93-96; views/leave/LeaveFormView.vue:49; services/leave.service.ts:55 | Api/Controllers/LeaveController.cs (PUT requests/{id} → NoContentResult); Features/Leaves/Commands/LeaveCommands.cs:83 (`IRequest`) | Endpoint gövdesiz döner; store `!res.data` ise "İzin talebi güncellenemedi." fırlatıyor. | Güncelleme kaydedilir ama hata gösterilir, yönlendirme olmaz. | `res.success` kontrol edip `fetchLeave(id)` çağır. |
| 54 | Orta | Bildirimler | h | Bildirim listesi tarih filtresi; şablon listesi kategori filtresi | views/notifications/NotificationListView.vue:25,103; types/notification.types.ts:112-123 | Features/Notifications/Queries/NotificationQueries.cs:11-16 (CategoryId, Status, IsRead); NotificationTemplateQueries.cs:11-17 (CategoryId, TypeId) | Frontend `from`/`to`/`categoryCode` gönderiyor; backend bu parametreleri tanımıyor. | Tarih filtresi etkisiz. | Backend'e ekle ya da UI'dan kaldır. |
| 55 | Orta | Finans | b | Ödeme kaydet formu → Ödeme tarihi ve Notlar | views/finance/PaymentFormView.vue:79-80,144,149; types/finance.types.ts:300-310 | Features/Finance/Commands/PaymentCommands.cs:12 (RecordPaymentCommand) | Form `paidAt` ve `notes` gönderiyor; komutta bu alanlar yok. | Kullanıcının girdiği ödeme tarihi ve not sessizce atılır. | Komuta ekle veya formdan kaldır. |
| 56 | Orta | Finans | b | Burs ekle → Not ve para birimi | views/finance/ScholarshipListView.vue:39,74; types/finance.types.ts:409-428 | Features/Finance/Commands/ScholarshipCommands.cs:12,96 (`Note`) | Frontend `notes` ve `currency` gönderiyor; backend alan adı `note`, `currency` yok. | Burs notu kaydedilmez. | `notes`→`note`. |
| 57 ✅ | Orta | Finans | e, h | Kredi defteri → hareket türü filtresi ve rozet rengi | views/finance/CreditLedgerView.vue:98-101,197-201 | db/layer2_sped/07_finance.sql:51 (`entry_type in ('grant','consume','refund','adjustment','expire')`); Features/Finance/Queries/GetCreditLedgerQuery.cs:34 | Seçenekler `consumed/granted/refunded/adjusted/purchase`; DB değerleri `consume/grant/refund/adjustment/expire`. | Tür filtresi seçilince liste boşalır, rozet renkleri uygulanmaz. | Seçenekleri DB değerleriyle eşitle. |
| 58 | Orta | Finans | h | Ödeme listesi → Tarih aralığı filtresi | views/finance/PaymentListView.vue:50-51,114-115; types/finance.types.ts:291-298 | Api/Controllers/PaymentsController.cs (GET transactions: `paidFrom`, `paidTo`, `paymentMethodId`) | Frontend `from`/`to`/`search` gönderiyor; backend `paidFrom`/`paidTo` bekliyor, `search` yok. | Tarih filtresi hiçbir etki yapmaz. | Parametre adlarını eşitle. |
| ✅ 59 | Orta | Genel | i | Tüm form ve listelerde hata mesajı gösterimi | 95 dosyada 220 kullanım, ör. stores/camp.store.ts:48; views/consultancy/AgreementFormView.vue:78; stores/consent.store.ts:99 (`(e as Error).message`); services/api.service.ts:68-74 | Api/Middleware/ExceptionMiddleware.cs:49-98 (hata gövdesi `{ success:false, message, errors }`) | Axios 2xx dışı yanıtlarda reject eder; kod `AxiosError.message` ("Request failed with status code 422") gösteriyor, backend'in `message`/`errors` alanlarını okumuyor. `if (!res.success) throw new Error(res.message)` dalları yalnızca 2xx'te çalıştığından fiilen ölü kod. (Doğru örnek: views/students/StudentDetailView.vue:535-537.) | Kullanıcı doğrulama/iş kuralı hatalarının nedenini göremez; İngilizce genel bir metin görür. | Response interceptor'da hatayı `response.data.message` (+ `errors`) ile normalize et. |
| ✅ 60 | Orta | Genel | i | Model bağlama (400) ve yetki (403) hataları | services/api.service.ts:68-74; ör. views/corporations/CorporationListView.vue:150-157 | Api/Program.cs:79 (`AddControllers()` — `InvalidModelStateResponseFactory` özelleştirilmemiş); Api/Controllers/BaseController.cs:15 (`[ApiController]`) | Eksik zorunlu alan / tip dönüşüm hatalarında ASP.NET varsayılan `ValidationProblemDetails` (`{title, status, errors}`) döner; `ApiResponse` zarfı (`success`, `message`) yoktur. `[HasPermission]` reddi (403) ise gövdesizdir. | `response.data.message` okuyan az sayıdaki ekran da bu durumlarda boş/varsayılan mesaj gösterir. | `ConfigureApiBehaviorOptions` ile 400'leri ApiResponse'a çevir; 403 için ortak mesaj üret. |
| 61 | Orta | Kameralar | h | İzleme yetkileri filtreleri; kamera panosu "aktif yetki" sayısı | views/cameras/ViewingAuthorizationsView.vue:26-27,60-61; views/cameras/CameraDashboardView.vue:30; types/camera.types.ts:108-114 | Features/Cameras/Queries/CameraQueries.cs:218-225 (`ActiveOnly`, `SessionId`) | Frontend `isRevoked`/`isCurrentlyValid` gönderiyor; backend yalnızca `activeOnly` tanıyor. | İptal/geçerlilik filtreleri sunucuda uygulanmaz (yalnızca ilk sayfadaki kayıtlar istemcide ayrışır). | `activeOnly` kullan ya da backend'e ekle. |
| 62 ✅ | Orta | Kullanıcılar | e, h | Kullanıcı listesi durum filtresi | views/users/UsersView.vue:172-174 | Features/Users/Queries/GetUsersQuery.cs:41 (birebir eşitlik); db/layer1_core/04_identity_access.sql:16 | Seçenekler `Active/Suspended/Inactive`; DB değerleri `active/suspended/disabled/invited`. | Durum seçilince liste boşalır; davetli/devre dışı kullanıcılar filtrelenemez. | Seçenekleri DB değerleriyle eşitle. |
| 63 ✅ | Orta | Kurumlar | e, h | Kurum listesi durum filtresi | views/corporations/CorporationListView.vue:208-209 | Features/Corporations/Queries/GetCorporationsQuery.cs:34; db/layer1_core/02_tenancy.sql:16 | Seçenekler `Active/Inactive`; DB değerleri `active/suspended/closed`. | Filtre seçilince liste boşalır. | Seçenekleri DB değerleriyle eşitle. |
| 64 ✅ | Orta | Okul Danışmanlığı | e, h | Ziyaret ve gözlem listesi durum filtresi / durum etiketi | views/consultancy/VisitListView.vue:42-44,51-53,97-99; views/consultancy/ObservationListView.vue:87-89 | db/layer2_sped/12_consultancy.sql:45 (`status in ('planned','completed','cancelled')`) | Seçenek ve etiket haritaları `Scheduled/Completed/Cancelled` (ya da `scheduled`) kullanıyor; DB'de planlanan ziyaretin kodu `planned` ve tüm değerler küçük harf. | Filtre seçilince liste boşalır; durum sütununda çeviri yerine ham kod görünür. | Değerleri DB check listesiyle eşitle. |
| 65 | Orta | Okul Danışmanlığı | h | Ziyaret listesi → Tarih aralığı filtresi | views/consultancy/VisitListView.vue:25-26; types/consultancy.types.ts (VisitListQuery.from/to); services/consultancy.service.ts:78 | Features/Consultancy/Queries/SchoolVisitQueries.cs:18-19 (`DateFrom`, `DateTo`) | Frontend `from`/`to` gönderiyor; backend `dateFrom`/`dateTo` bekliyor. | Tarih filtresi etkisiz. | Parametre adlarını eşitle. |
| 66 | Orta | Okul Danışmanlığı | h | Takip görevleri → "Yalnız gecikenler" filtresi | views/consultancy/FollowUpListView.vue:26,174; types/consultancy.types.ts (FollowUpListQuery.overdueOnly, observationRecordId) | Features/Consultancy/Queries/FollowUpActivityQueries.cs:11-20 (yalnızca DueBefore/DueAfter) | `overdueOnly` ve `observationRecordId` parametreleri backend sorgusunda yok. | Gecikenler kutusu işaretlense de liste değişmez. | `overdueOnly` yerine `dueBefore=bugün` gönder ya da backend'e ekle. |
| 67 | Orta | Seans Planlama | d, h | Seans listesi tarih aralığı filtresi | views/scheduling/SessionListView.vue:66-67,181,187 (`type="date"`) | Features/Scheduling/Queries/GetSessionsQuery.cs:43-44; Api/Controllers/SchedulingController.cs:253-271 | `to` tarihi saat içermediği için backend `StartsAt <= to 00:00` uyguluyor; ayrıca saat dilimi bilgisi gönderilmediğinden sunucu saatine göre yorumlanıyor. | Bitiş tarihindeki seanslar listede çıkmaz; UTC+3 farkı nedeniyle gün sınırındaki seanslar kayabilir. | Tarihleri ISO (offset'li) gönder ve `to`yu gün sonu yap. |
| 68 ✅ | Orta | Seans Planlama | e | Seans listesi durum filtresi | views/scheduling/SessionListView.vue:106-110,167-171 | db/layer2_sped/06_scheduling.sql:111 (`status in ('scheduled','completed','cancelled','no_show','rescheduled')`) | Filtre `in_progress` seçeneği sunuyor (DB'de böyle bir durum yok); DB'deki `rescheduled` durumu ise filtrede ve etiket haritasında yok. | "Devam ediyor" filtresi hep boş döner; ertelenmiş seanslar filtrelenemez ve durum ham kod (`rescheduled`) olarak görünür. | Seçenekleri DB check listesiyle eşitle. |
| 69 ✅ | Orta | Sözleşme / Rıza | e, h | Sözleşme ve rıza listesi durum filtreleri | views/legal/ContractListView.vue:99-103; views/legal/ConsentListView.vue:84-86 | Features/Legal/Queries/StudentContractQueries.cs:35; StudentConsentQueries.cs:38 (birebir eşitlik); db/layer2_sped/08_legal.sql:62,87 | Filtre değerleri PascalCase (`Draft`, `Sent`, `Granted`…) ve rıza için DB'de olmayan `Pending`; backend büyük/küçük harfe duyarlı eşitlik yapıyor. `signed` (sözleşme) ve `expired` (rıza) seçenekleri yok. | Herhangi bir durum seçilince liste boşalır. | Seçenek değerlerini DB check listesiyle eşitle. |
| 70 | Orta | Sözleşme / Rıza | h | Hukuk listelerinde arama/sıralama | types/legal.types.ts (ContractListQuery, ConsentListQuery, *TemplateListQuery: search/sortBy/sortDirection); services/legal.service.ts:31,43,70,82 | Features/Legal/Queries/StudentContractQueries.cs, StudentConsentQueries.cs, ContractTemplateQueries.cs, ConsentTemplateQueries.cs (PagedQuery'den türemiyor) | Bu dört liste sorgusu `search`, `sortBy`, `sortDirection` kabul etmiyor. | Arama kutusu ve sütun sıralaması etkisiz. | Sorguları PagedQuery'ye taşı veya UI'dan kaldır. |
| 71 ✅ | Orta | Toplantılar | h, e | Toplantı listesi tarih ve durum filtreleri | views/meetings/MeetingListView.vue:25-26,125; types/meeting.types.ts (MeetingListQuery.from/to/participantUserId/participantGuardianId) | Features/Meetings/Queries/MeetingQueries.cs:15-23 (`ScheduledFrom`, `ScheduledTo`); db/layer2_sped/10_ops.sql:19 (`status in ('scheduled','completed','cancelled')`) | Frontend `from`/`to` gönderiyor, backend `scheduledFrom`/`scheduledTo` bekliyor; durum filtresinde DB'de olmayan `draft` var. | Tarih filtresi etkisiz; "Taslak" seçilince liste boş. | Parametre adlarını eşitle, `draft` seçeneğini kaldır. |
| 72 ✅ | Orta | İzin Yönetimi | e, h | İzin listesi durum ve birim filtreleri | views/leave/LeaveListView.vue:118-121,128-129 | Features/Leaves/Queries/LeaveQueries.cs:47-51 (birebir eşitlik); db/layer2_sped/10_ops.sql:70,76 (`'day','hour'`; `'pending','approved','rejected','cancelled'`) | Seçenek değerleri `Pending/Approved/Rejected/Cancelled` ve `Day/Hour`; DB değerleri küçük harf. | Filtre seçilince liste boşalır (panodan gelen `?status=pending` linki ise çalışır). | Seçenek değerlerini küçük harfe çevir. |
| 73 | Düşük | Bildirimler | b | Şablon oluştur/güncelle fazladan alanlar | types/notification.types.ts:125-136; services/notification.service.ts:69,72 | Features/Notifications/Commands/NotificationTemplateCommands.cs:13,88 | Create'te `isActive`, update'te `code` gönderiliyor; komutlarda bu alanlar yok. | Oluştururken seçilen aktiflik durumu dikkate alınmaz. | Tipleri eşitle. |
| 74 | Düşük | Değerlendirme | c | Şablon/seans detayı → bölüm başlığındaki gelişim alanı adı | views/assessment/SessionDetailView.vue:359; views/assessment/TemplateDetailView.vue:280; types/assessment.types.ts:36 | Features/Assessment/Dtos/AssessmentTemplateDto.cs:31 (DevelopmentAreaId, DevelopmentAreaCode) | `developmentAreaName` backend'de yok. | Gelişim alanı etiketi hiç görünmez (v-if ile gizli). | `developmentAreaCode` kullan veya DTO'ya ad ekle. |
| 75 | Düşük | Finans | b, d | View'e bağlı olmayan/etkisiz sözleşme farkları | services/finance.service.ts:128,144,180,199,228; types/finance.types.ts:180,246,328,365,485 | Features/Finance/Commands/InvoiceCommands.cs:12 (IssueDate zorunlu DateOnly); CreditLedgerCommands.cs:127; DiscountCommands.cs:12; Api/Controllers/PaymentsController.cs:591 (PromotionDto? döner) | `createInvoice` payload'ında `issueDate` yok (0001-01-01 kaydedilir); `refundCredits.sessionId` ve iade/kredi listesi `search` yok sayılır; `validatePromotion` `ValidatePromotionResult` yerine `PromotionDto` döner; refunds listesi `RefundDto` döner (`currency` yok); TS'de `rowVersion: string`, backend `int`. | Bu çağrılar şu an hiçbir view'den tetiklenmiyor veya etkisi görünmüyor; ekran bağlanınca hatalı veri üretir. | Tipleri backend'le eşitle. |
| 76 | Düşük | Genel | — | tr.ts'te tanımlı olmayan i18n anahtarları (43 anahtar, 20 view) | ör. views/educators/EducatorDetailView.vue:343,580,630; views/goals/StudentGoalDetailView.vue:45-48,58,62-64; views/students/StudentDashboardView.vue:131-139,222,232 (tam liste: Ek A) | frontend/aynesil-web/src/i18n/locales/tr.ts (anahtar yok) | `t()` ile kullanılan 43 anahtar tr.ts'te yok (ör. `common.inactive`, `common.deleteConfirmTitle`, `goal.studentGoal.status.*`, `goal.progress.trend.*`, `scheduling.session.status.rescheduled`). | Ekranda çeviri yerine ham anahtar metni görünür. | Anahtarları tr.ts'e ekle veya mevcut anahtarlara yönlendir. |
| 77 ✅ | Düşük | Kameralar | f | İzleme başlat / bitir (view'e bağlı değil) | stores/camera.store.ts:202-204,214; services/camera.service.ts:90,93; types/camera.types.ts:164-168 | Features/Cameras/Commands/ViewingCommands.cs:164,180 (`CorporationId` NotEmpty); Api/Controllers/CameraController.cs:215-219,241 (`EndViewingSessionRequest(StartedAt)` gövdesi zorunlu) | `startViewing` payload'ında `corporationId` yok; `endViewing` gövde göndermiyor ama backend `startedAt` bekliyor. | Şu an ekrandan çağrılmıyor; bağlanınca 422/400. | Payload'ları tamamla. |
| 78 | Düşük | Kamplar | a | Kullanılmayan store/servis fonksiyonları (recordAttendance, getAttendanceSummary, getPerformance, getCampReport, listActivities, listParticipations) | services/camp.service.ts:77-78, 80-81, 87-88, 90-91, 94-95, 108-109 | Api/Controllers/CampsController.cs:105 (GET performance), :258 (periods/{id}/attendance-summary), :265 (POST attendance), :300 (enrollments/{id}/reports), :321 (GET activities), :419 (GET participations) | Bu servis fonksiyonlarının yolları backend'le uyuşmuyor (ör. `/camps/enrollments/{id}/attendance`, `/camps/{id}/performance`, `/camps/enrollments/{id}/report`, `/camps/periods/{id}/activities` GET, `/camps/activities/{id}/participations` GET). Hiçbir view çağırmıyor. | Şu an etkisi yok; ekran bağlandığında 404/405 verecek. | Yolları backend'e göre düzelt ya da ölü kodu kaldır. |
| 79 ✅ | Düşük | Okul Danışmanlığı | f, c | Kullanılmayan store fonksiyonları (addObservation, updatePlan) | services/consultancy.service.ts:65,96; types/consultancy.types.ts:387-392 | Features/Consultancy/Commands/ObservationCommands.cs:12,25 (CorporationId NotEmpty); Api/Controllers/ConsultancyController.cs (PUT plans/{id} NoContent) | `addObservation` payload'ında zorunlu `corporationId` yok; `updatePlan` gövdesiz yanıtı `ConsultancyPlanDto` sanıyor. | View'ler çağırmıyor; bağlanınca 422 / boş veri. | Tipleri düzelt. |
| 80 | Düşük | Performans / KPI | b | View'e bağlı olmayan KPI servisleri (trend, kpi-values, educator kpi-values) | services/kpi.service.ts:47,53,106,111 | Features/PerformanceKpi/Queries/TrendAndRankingQueries.cs (`kpiCode`, `fromPeriodStart`, `maxPeriods`); KpiValueQueries.cs:14,70 (`periodStart`, `periodEnd`, `kpiCodePrefix`, `subjectId`) | Servis parametre adları (`kpiId`, `educatorId`, `from`, `to`, `periodType`) backend sorgularıyla uyuşmuyor. | Şu an çağrılmıyor. | Tipleri eşitle. |
| 81 | Düşük | Platform | a | Dil mesajlarını yükleme (locale store) | stores/locale.store.ts:24-26 | — (Api/Controllers altında `localization` route'u yok) | `GET /localization/messages` backend'de yok. | Çağrı 404 alır, hata yutulur; şu an yalnızca `setLocale` içinden tetiklenir. | Endpoint'i ekle ya da çağrıyı kaldır. |
| 82 | Düşük | Programlar / Kayıtlar | h | Kayıt ve öğrenci-program listeleri arama kutusu | types/program.types.ts (EnrollmentListQuery.search, StudentProgramListQuery.search); services/program.service.ts:81,115 | Api/Controllers/ProgramsController.cs (GET enrollments, GET student-programs — `search` parametresi yok) | Frontend `search` gönderiyor; backend kabul etmiyor. | Arama metni listeyi daraltmaz. | Backend'e search ekle ya da tipten kaldır. |
| 83 | Düşük | Seans Planlama | b | Tip tanımlarında backend'in tanımadığı alanlar | types/scheduling.types.ts (CreateCalendarEntryPayload.description, CompleteSessionPayload.notes, BulkReassignRoomPayload.to, SessionListQuery.educatorId/studentId, RecurringScheduleListQuery.search, MakeupRequestListQuery.search) | Features/Scheduling/Commands/CalendarEntryCommands.cs:12; SessionCommands.cs:158; RecurringScheduleCommands.cs:544; Api/Controllers/SchedulingController.cs (sessions, recurring-schedules, makeup-requests GET parametreleri) | Bu alan/parametreler backend komut ve sorgularında yok; gönderilse bile yok sayılır. | Şu an view'ler kullanmıyor; kullanılırsa sessizce etkisiz kalır. | Tipleri backend sözleşmesiyle eşitle. |
| 84 | Düşük | Sözleşme / Rıza | a, c | Rıza oluştur / ver / geri al servisleri (view'e bağlı değil) | services/legal.service.ts:90,93,96; stores/consent.store.ts:70-71,79-80,88-89 | Api/Controllers/LegalController.cs:322 (POST consents/grant), :337-345 (withdraw → NoContent) | `POST /legal/consents` ve `POST /legal/consents/{id}/grant` backend'de yok (tek endpoint `POST /legal/consents/grant`); withdraw gövdesiz döner ama store `res.data` bekler. | Şu an ekran yok; bağlanınca 404/405 ve yanlış hata mesajı. | Servisi backend yollarına göre düzelt. |
| 85 | Düşük | Veli Portalı | d | Gelişim raporları listesi | services/portal.service.ts:95-96 (`PortalDocumentDto[]`) | Api/Controllers/PortalController.cs:57-63 (`GetDevelopmentReportsQuery` → DevelopmentReportDto listesi) | Frontend yanıtı `PortalDocumentDto` (fileId, originalName…) sanıyor; backend `DevelopmentReportDto` döner. | Şüpheli: rapor satırlarında dosya adı/tarih boş çıkabilir (kullanım yeri doğrulanmadı). | Doğru tipi tanımla. |

## 3. Kategori (j) — Backend'de olup frontend'in hiç çağırmadığı endpoint'ler

Hata sayılmaz; bilgi amaçlıdır. Toplam **79** endpoint (579 backend endpoint'inden).

| Controller | Metot | Route | Satır | İzin |
|---|---|---|---|---|
| Auth | POST | `/api/auth/register` | 66 | Users.Create |
| Auth | POST | `/api/auth/verify-email` | 85 | anonim |
| Auth | POST | `/api/auth/request-password-reset` | 101 | anonim |
| Auth | POST | `/api/auth/confirm-password-reset` | 115 | anonim |
| Auth | POST | `/api/auth/change-password` | 132 | yalnızca oturum |
| Camera | GET | `/api/cameras/assignments` | 89 | CameraAssignment.Read |
| Camps | POST | `/api/camps/{id}/activate` | 62 | Camps.Activate |
| Camps | POST | `/api/camps/{id}/deactivate` | 73 | Camps.Activate |
| Camps | DELETE | `/api/camps/{id}` | 84 | Camps.Delete |
| Camps | GET | `/api/camps/{id}/enrollment-summary` | 98 | Camps.Read |
| Camps | GET | `/api/camps/performance` | 105 | Camps.Read |
| Camps | GET | `/api/camps/{campId}/periods` | 115 | Camps.Read |
| Camps | PUT | `/api/camps/periods/{periodId}` | 142 | Camps.ManagePeriods |
| Camps | DELETE | `/api/camps/periods/{periodId}` | 154 | Camps.ManagePeriods |
| Camps | GET | `/api/camps/enrollments` | 168 | CampEnrollments.Read |
| Camps | POST | `/api/camps/periods/{periodId}/enrollments` | 184 | CampEnrollments.Enroll |
| Camps | POST | `/api/camps/enrollments/{enrollmentId}/waitlist` | 196 | CampEnrollments.Manage |
| Camps | POST | `/api/camps/periods/{periodId}/bulk-complete` | 241 | CampEnrollments.Complete |
| Camps | GET | `/api/camps/attendance` | 250 | CampAttendance.Read |
| Camps | GET | `/api/camps/periods/{periodId}/attendance-summary` | 258 | CampAttendance.Read |
| Camps | POST | `/api/camps/attendance` | 265 | CampAttendance.Record |
| Camps | POST | `/api/camps/periods/{periodId}/attendance/bulk` | 277 | CampAttendance.Record |
| Camps | PATCH | `/api/camps/attendance/{attendanceId}` | 286 | CampAttendance.Record |
| Camps | GET | `/api/camps/enrollments/{enrollmentId}/reports` | 300 | CampReports.Read |
| Camps | POST | `/api/camps/enrollments/{enrollmentId}/reports` | 307 | CampReports.Create |
| Camps | GET | `/api/camps/activities` | 321 | CampActivities.Read |
| Camps | GET | `/api/camps/activities/{activityId}` | 329 | CampActivities.Read |
| Camps | PUT | `/api/camps/activities/{activityId}` | 349 | CampActivities.Update |
| Camps | DELETE | `/api/camps/activities/{activityId}` | 361 | CampActivities.Delete |
| Camps | GET | `/api/camps/educators` | 374 | CampEducators.Read |
| Camps | PATCH | `/api/camps/educators/{assignmentId}/role` | 394 | CampEducators.Manage |
| Camps | DELETE | `/api/camps/educators/{assignmentId}` | 406 | CampEducators.Manage |
| Camps | GET | `/api/camps/participations` | 419 | CampParticipation.Read |
| Camps | POST | `/api/camps/activities/{activityId}/participations` | 427 | CampParticipation.Record |
| Camps | POST | `/api/camps/activities/{activityId}/participations/bulk` | 439 | CampParticipation.Record |
| Camps | PATCH | `/api/camps/participations/{participationId}` | 448 | CampParticipation.Record |
| CareTeam | GET | `/api/students/{studentId}/care-team` | 30 | CareTeam.Read |
| CareTeam | GET | `/api/students/{studentId}/care-team/{assignmentId}` | 44 | CareTeam.Read |
| CareTeam | POST | `/api/students/{studentId}/care-team` | 58 | CareTeam.Assign |
| CareTeam | PUT | `/api/students/{studentId}/care-team/{assignmentId}` | 85 | CareTeam.Assign |
| CareTeam | DELETE | `/api/students/{studentId}/care-team/{assignmentId}` | 114 | CareTeam.Assign |
| CareTeam | GET | `/api/care-team/my-students` | 133 | CareTeam.Read |
| Consultancy | DELETE | `/api/consultancy/institutions/{id}` | 65 | Institutions.Delete |
| Consultancy | DELETE | `/api/consultancy/plans/{id}` | 158 | ConsultancyPlans.Delete |
| Consultancy | PUT | `/api/consultancy/visits/{id}` | 202 | SchoolVisits.Update |
| Consultancy | DELETE | `/api/consultancy/visits/{id}` | 239 | SchoolVisits.Delete |
| Consultancy | GET | `/api/consultancy/visits/{visitId}/observations` | 255 | Observations.Read |
| Consultancy | GET | `/api/consultancy/observations/{id}` | 262 | Observations.Read |
| Consultancy | PUT | `/api/consultancy/observations/{id}` | 282 | Observations.Update |
| Consultancy | DELETE | `/api/consultancy/observations/{id}` | 295 | Observations.Delete |
| Consultancy | DELETE | `/api/consultancy/reports/{id}` | 338 | ConsultancyReports.Delete |
| Health | GET | `/Health` | 10 | yalnızca oturum |
| Leave | POST | `/api/leave/balances` | 144 | LeaveBalances.Manage |
| Legal | GET | `/api/legal/contract-templates/{corporationId}/{code}/versions` | 49 | ContractTemplates.Read |
| Legal | PUT | `/api/legal/contract-templates/{id}` | 69 | ContractTemplates.Update |
| Legal | POST | `/api/legal/contract-templates/{id}/new-version` | 81 | ContractTemplates.Version |
| Legal | DELETE | `/api/legal/contract-templates/{id}` | 93 | ContractTemplates.Delete |
| Legal | PUT | `/api/legal/contracts/{id}` | 140 | StudentContracts.Update |
| Legal | DELETE | `/api/legal/contracts/{id}` | 210 | StudentContracts.Delete |
| Legal | GET | `/api/legal/consent-templates/{corporationId}/{code}/versions` | 241 | ConsentTemplates.Read |
| Legal | PUT | `/api/legal/consent-templates/{id}` | 261 | ConsentTemplates.Update |
| Legal | POST | `/api/legal/consent-templates/{id}/new-version` | 273 | ConsentTemplates.Version |
| Legal | DELETE | `/api/legal/consent-templates/{id}` | 285 | ConsentTemplates.Delete |
| Legal | POST | `/api/legal/consents/grant` | 322 | StudentConsents.Grant |
| Legal | GET | `/api/legal/reporting/contracts` | 368 | LegalReports.Read |
| Legal | GET | `/api/legal/reporting/consents` | 379 | LegalReports.Read |
| Legal | GET | `/api/legal/reporting/signatures` | 390 | LegalReports.Read |
| PerformanceKpi | POST | `/api/performance-kpi/definitions` | 54 | Kpi.Manage |
| PerformanceKpi | PUT | `/api/performance-kpi/definitions/{id}` | 66 | Kpi.Manage |
| ReferenceData | GET | `/api/reference-data/{typeCode}/default` | 46 | RefData.Read |
| Surveys | GET | `/api/surveys` | 21 | Surveys.Read |
| Surveys | GET | `/api/surveys/{id}` | 29 | Surveys.Read |
| Surveys | POST | `/api/surveys` | 37 | Surveys.Create |
| Surveys | PUT | `/api/surveys/{id}` | 49 | Surveys.Update |
| Surveys | DELETE | `/api/surveys/{id}` | 62 | Surveys.Delete |
| Surveys | GET | `/api/surveys/responses` | 75 | Surveys.Read |
| Surveys | POST | `/api/surveys/responses` | 83 | Surveys.Respond |
| Surveys | GET | `/api/surveys/parent-feedback` | 97 | ParentFeedback.Read |
| Surveys | POST | `/api/surveys/parent-feedback` | 105 | ParentFeedback.Create |

Notlar: `SurveysController` ve `CareTeamController` için frontend'de hiç servis yok. Kamp modülünde listelenenlerin bir kısmı, frontend'in yanlış yolla çağırmaya çalıştığı endpoint'lerin doğru karşılıklarıdır (bkz. Kamplar bulguları). `/Health` altyapı içindir.

## Ek A — tr.ts'te eksik i18n anahtarları

| Anahtar | Kullanım sayısı | İlk kullanım |
|---|---|---|
| `common.campus` | 2 | views/bep/EducationPlanDetailView.vue:445 |
| `bep.revision.changeSummary` | 1 | views/bep/EducationPlanDetailView.vue:789 |
| `bep.title2` | 4 | views/bep/EducationPlanFormView.vue:62 |
| `common.deleteConfirmTitle` | 10 | views/bep/EducationPlanListView.vue:306 |
| `common.deleteConfirmMessage` | 8 | views/bep/EducationPlanListView.vue:307 |
| `common.unsavedChanges` | 1 | views/consultancy/FollowUpFormView.vue:150 |
| `common.inactive` | 11 | views/educators/EducatorDashboardView.vue:74 |
| `common.primary` | 4 | views/educators/EducatorDashboardView.vue:97 |
| `common.assignedStudents` | 1 | views/educators/EducatorDashboardView.vue:122 |
| `common.comingSoon` | 4 | views/educators/EducatorDashboardView.vue:123 |
| `common.utilization` | 1 | views/educators/EducatorDashboardView.vue:130 |
| `common.activePrograms` | 1 | views/educators/EducatorDashboardView.vue:137 |
| `common.total` | 1 | views/educators/EducatorDashboardView.vue:143 |
| `common.availability` | 1 | views/educators/EducatorDetailView.vue:343 |
| `common.unlink` | 3 | views/educators/EducatorDetailView.vue:580 |
| `common.type` | 1 | views/educators/EducatorDetailView.vue:630 |
| `common.startDate` | 1 | views/educators/EducatorDetailView.vue:665 |
| `common.relationship` | 1 | views/educators/EducatorDetailView.vue:684 |
| `common.confirmAction` | 6 | views/educators/EducatorDetailView.vue:704 |
| `common.all` | 6 | views/educators/EducatorListView.vue:178 |
| `common.other` | 2 | views/goals/GoalDashboardView.vue:134 |
| `goal.studentGoal.status.active` | 4 | views/goals/StudentGoalDetailView.vue:45 |
| `goal.studentGoal.status.achieved` | 4 | views/goals/StudentGoalDetailView.vue:46 |
| `goal.studentGoal.status.discontinued` | 4 | views/goals/StudentGoalDetailView.vue:47 |
| `goal.studentGoal.status.on_hold` | 4 | views/goals/StudentGoalDetailView.vue:48 |
| `goal.studentGoal.horizon.longTerm` | 1 | views/goals/StudentGoalDetailView.vue:58 |
| `goal.studentGoal.horizon.shortTerm` | 1 | views/goals/StudentGoalDetailView.vue:58 |
| `goal.progress.trend.improving` | 2 | views/goals/StudentGoalDetailView.vue:62 |
| `goal.progress.trend.declining` | 2 | views/goals/StudentGoalDetailView.vue:63 |
| `goal.progress.trend.stable` | 2 | views/goals/StudentGoalDetailView.vue:64 |
| `common.custody` | 1 | views/guardians/GuardianDetailView.vue:272 |
| `common.noAccess` | 1 | views/guardians/GuardianDetailView.vue:340 |
| `student.dateOfBirth` | 1 | views/portal/ChildDetailView.vue:36 |
| `scheduling.session.status.rescheduled` | 1 | views/scheduling/SessionDetailView.vue:109 |
| `scheduling.attendance.status` | 1 | views/scheduling/SessionDetailView.vue:378 |
| `common.activeGoals` | 2 | views/students/StudentDashboardView.vue:131 |
| `common.upcomingSessions` | 1 | views/students/StudentDashboardView.vue:135 |
| `common.recentAssessments` | 2 | views/students/StudentDashboardView.vue:139 |
| `common.assignedEducators` | 1 | views/students/StudentDashboardView.vue:222 |
| `common.attendanceSummary` | 1 | views/students/StudentDashboardView.vue:232 |
| `common.error` | 1 | views/students/StudentDetailView.vue:607 |
| `common.date` | 2 | views/students/StudentDetailView.vue:1796 |
| `common.phone` | 1 | views/students/StudentDetailView.vue:1843 |

## Ek B — Yöntem, kapsam ve sınırlar

- Envanter betiklerle çıkarıldı: 579 backend endpoint'i (36 controller), 517 frontend API çağrısı (tamamı `services/` ve 5 store), 327 izin sabiti, 1979 i18n anahtarı. Rota eşleştirme, aynı adlı DTO alan karşılaştırması, istek gövdesi/sorgu parametresi karşılaştırması otomatik yapıldı; tabloya giren her bulgu için iki taraftaki kod ayrıca açılıp okundu.
- Build, test veya uygulama çalıştırılmadı; bulgular statik okumaya dayanır. HTTP durum kodları (400/422/405) koddan çıkarımdır.
- **Uyuşmazlık bulunmayan modüller:** Öğrenciler/Veliler, Eğitmenler, BEP (eğitim planları), Hedefler, Değerlendirme (1 düşük bulgu dışında), Roller, Menü yönetimi, Kampüsler, Referans veri, Auth (login/refresh/logout ve JWT claim adları).
- **İzin adları:** frontend'de kullanılan 224 izin kodundan yalnızca 4'ü backend/seed'de yok (Finans). Permissions.cs'teki 327 sabitin tamamı db/ altında seed edilmiş.
- **Tam doğrulanamayanlar:** (1) `iam.menu_item.route` değerlerinin router ile uyumu — V8–V25 seed'lerinde router'da olmayan 42 rota var, ancak V27/V31/V37 düzeltme migration'ları sonrası nihai durum veritabanında kontrol edilmeli. (2) Nullable farkları (backend `?`, TS'de zorunlu) tek tek incelenmedi. (3) Frontend'in buton için kontrol ettiği izin ile endpoint'in istediği iznin aynı olup olmadığı sistematik karşılaştırılmadı. (4) "Şüpheli" olarak ayrıca işaretlenen satır yok; emin olunamayan noktalar ilgili satırın Sorun/Etki sütununda "Şüpheli" diye belirtildi.

## İlerleme notu

Tüm modüller tarandı; yarım kalan modül yok.

### Düzeltme partisi — CorporationId backend'de belirleniyor (2026-11-12)

✅ işaretli satırlar: **#1, #5, #6, #8, #9, #16, #77, #79** (+ aynı kök nedenli toplantı katılımcısı ekleme, `meeting.service.ts:62`).

- **Karar:** İstemcinin gönderdiği `corporationId` alanına güvenilmiyor. İlgili komutlardan `CorporationId` kaldırıldı; handler'lar değeri `ITenantContext` üzerinden (JWT `corporation_id`) alıyor. Yardımcı metot: `Application/Common/Interfaces/RequestContextExtensions.cs` (`RequireCorporationId`, `RequireUserId`). Tenant yoksa 401 dönüyor (mevcut `RefValueCommands` deseniyle aynı). Platform yöneticisi gibi `corporation_id` claim'i olmayan bir kullanıcı tipi yok: `iam.user_account.corporation_id NOT NULL` ve token bu claim'i her zaman içeriyor.
- **Üst kayıt doğrulaması:** Kamp, dönem, toplantı, okul ziyareti, kamera/derslik/seans, izleme yetkisi, veli portal erişimi ve izleme kaydı sorguları artık açıkça `CorporationId == tenant` filtresi içeriyor. Bu filtre RLS'e ek bir savunma katmanı.
- **Aktör id'leri:** `GrantedBy` (izleme yetkisi), `RevokedBy`, `StartViewing.UserId`, `EndViewing.UserId`, `CreatedBy` (toplantı sonucu, gözlem) ve bildirim tercihlerindeki `UserId` istek bağlamından (`ICurrentUserService`) dolduruluyor ve komutlardan kaldırıldı. "İşlemi yapan kişi" anlamı taşımadığı için dokunulmayanlar: `AddMeetingFollowUp.AssigneeId` (sorumlu kişi), `AddMeetingParticipant.UserId/GuardianId/LeadId` (katılımcı), `GrantViewingAuthorization.GuardianId` (yetki verilen veli), `StartViewingSession.GuardianId` (izleyen veli; portal kullanıcısından türetilmesi ayrı bir iş).
- **Request record'ları:** `AssignCameraToRoomRequest` ve `AssignCameraToSessionRequest` içinden `CorporationId` kaldırıldı. Eski istemciler alanı gönderirse sessizce yok sayılıyor.
- **#8 rota düzeltmesi bu partide yapıldı:** `camp.service.ts` artık `POST /camps/periods/{id}/enrollments` çağırıyor (önceden `/enroll`). Sonraki partide tekrarlanmasına gerek yok.
- **#77 ikinci kısmı da yapıldı:** `endViewing` artık `startViewing` yanıtındaki `startedAt` değerini `{ startedAt }` gövdesiyle gönderiyor; `StartViewingPayload.authorizationId` zorunlu hale geldi.
- **#79 ikinci kısmı da yapıldı:** `consultancy.service.ts` içinde `updatePlan` dönüş tipi gövdesiz yanıta (`null`) göre düzeltildi. Store'da `updatePlan` fonksiyonu yok; değişiklik yalnızca servis tipinde.
- **Testler:** `tests/Aynesil.Tests/TenantScoping/` altında 22 handler testi var. Her handler için CorporationId'nin tenant bağlamından geldiği ve başka kiracıya ait üst kayıtla yapılan çağrının reddedildiği doğrulanıyor. Testler RLS'siz InMemory sağlayıcısıyla koşuyor, dolayısıyla handler'ın kendi filtresini ölçüyorlar.
- **Doğrulama:** `dotnet build Aynesil.sln` başarılı, `dotnet test` 22/22 geçti, `npm run type-check` temiz.

## Ek C — Sonradan eklenen bulgular

| # | Önem | Modül | Kategori | Ekran / Akış | Frontend (dosya:satır) | Backend veya DB (dosya:satır) | Sorun | Kullanıcıya etkisi | Önerilen düzeltme |
|---|---|---|---|---|---|---|---|---|---|
| 86 ✅ | Kritik | Genel (çok kiracılık) | güvenlik | İstemcinin alt kayıt id'si gönderdiği tüm oluşturma akışları (kamp kaydı, toplantı katılımcısı/takip görevi, izleme yetkisi vb.) | — | db/layer2_sped/11_camps.sql, 10_ops.sql, 09_media.sql; ilgili handler'lar (Features/Camps, Meetings, Cameras) | PostgreSQL FK kontrolleri RLS'e takılmaz. İstemci başka kiracıya ait bir `student_id`, `user_id`, `guardian_id` vb. gönderirse FK doğrulaması geçer ve satır mevcut kiracı altında yazılır. Handler'lar yalnızca üst kaydı (kamp dönemi, toplantı vb.) kiracıya göre doğruluyor; alt kimlikleri doğrulamıyor. | Kiracılar arası veri bağlantısı kurulabilir (ör. A kurumunun toplantısına B kurumunun kullanıcısını katılımcı eklemek). Bu kayıtlar üzerinden yabancı kiracının kimlikleri ve ilişkili veriler sızabilir. | (1) DB: üst tablolara `UNIQUE (corporation_id, id)`, alt tablolara `FOREIGN KEY (corporation_id, x_id) REFERENCES parent (corporation_id, id)` composite FK eklenmeli (Flyway migration + layer DDL senkronu). (2) Uygulama: handler'larda alt kimlikler `CorporationId == tenant` ile doğrulanmalı ve anlamlı bir 404/422 dönmeli. |
| 87 ✅ | Kritik | Okul Danışmanlığı | güvenlik | Okul danışmanlığı → Sözleşmeler, Takip faaliyetleri | — | db/migrations/V26__consultancy_agreements_and_followups.sql; db/99_triggers_rls_policies.sql | V26 `consultancy.consultancy_agreement` ve `consultancy.follow_up_activity` tablolarını oluşturdu ve RLS ile updated_at tetikleyicisinin 99 tarafından "otomatik" uygulanacağını varsaydı. 99 ise yalnızca V1 baseline'ında çalışıyor. Migration yoluyla kurulan her veritabanında bu iki tabloda RLS kapalıydı, politika yoktu ve `aynesil_app` okuma yetkisine sahipti. | Her kurum, diğer kurumların danışmanlık sözleşmelerini ve takip faaliyetlerini görebiliyordu (kiracı izolasyonu yok). | ✅ V42: RLS açıldı; `tenant_isolation` politikası ve `trg_set_updated_at` tetikleyicisi diğer tablolarla birebir aynı tanımla eklendi. `db/validation/drift.sql` içine "her kiracı tablosunda RLS + tenant_isolation" değişmez kontrolü eklendi. Entegrasyon testi: A kiracısı B'nin satırlarını görmüyor. |

**#86 — etkilenen tablolar ve FK'lar** (bu partide ele alınan komutlarla sınırlı tarama; aynı desen başka modüllerde de muhtemel):

| Tablo | İstemciden gelen FK sütunu → hedef | Not |
|---|---|---|
| `camps.camp_enrollment` | `student_id` → `students.student(id)`; `student_package_id` → `finance.student_package(id)` | `camp_period_id` artık handler'da kiracıya göre doğrulanıyor |
| `ops.meeting_participant` | `user_id` → `iam.user_account(id)`; `guardian_id` → `students.guardian(id)`; `lead_id` → `crm.lead(id)` | `meeting_id` artık handler'da doğrulanıyor |
| `ops.meeting_follow_up` | `assignee_id` → `iam.user_account(id)` | `meeting_id` artık handler'da doğrulanıyor |
| `media.viewing_authorization` | `session_id` → `scheduling.session(id)`; `consent_id` → `legal.student_consent(id)` | `guardian_id`/`student_id` dolaylı olarak doğrulanıyor (kiracı filtreli portal erişimi + rıza kontrolü) |
| `media.viewing_log` | `session_id`, `camera_id`, `guardian_id` (DB'de FK **yok**) | Composite FK yerine uygulama seviyesinde doğrulama gerekli |
| `core.notification_preference`, `consultancy.observation_record`, `media.viewing_authorization` | `category_id`, `channel_id`, `observation_type_id`, `access_type_id` → `ref.ref_value(id)` | Ref değerleri global olabiliyor (`corporation_id IS NULL`), bu yüzden composite FK uygun değil. Doğrulama "global ya da kendi kiracısı" şeklinde olmalı |

Composite FK, üst tabloda `(corporation_id, id)` üzerinde unique kısıt gerektiriyor. `id` zaten PK olduğu için bu kısıt veri açısından risksiz; yalnızca ek bir index maliyeti var.

**#86 çözüm notu (2026-11-12):** Tarama, yukarıdaki örneklemle sınırlı kalmadı; şemanın tamamına uygulandı.

- **Composite FK'lar:** İki kiracı tablosu arasındaki **202 FK**'nın tamamı `(corporation_id, x) → parent(corporation_id, id)` biçimine çevrildi. 39 hedef tabloya unique `(corporation_id, id)` index'i eklendi.
  - V38: index'ler, `CONCURRENTLY` ile.
  - V39: FK'lar, `NOT VALID` olarak.
  - V40: `VALIDATE` adımı.
  - `ON DELETE` davranışları korundu: 125 NO ACTION, 74 CASCADE, 1 RESTRICT, 2 SET NULL. SET NULL için `SET NULL (col)` biçimi kullanıldı; bu yüzden **PG15+ zorunlu** oldu.
- **Global olabilen tablolar:** Global satır da tutan tablolara giden **72 FK** için genel bir tetikleyici eklendi (`core.enforce_global_or_tenant_ref`, V41, 63 tetikleyici). Kural: hedef satır global olmalı ya da aynı kiracıya ait olmalı.
- **`media.viewing_log`:** DB'de FK yok. `StartViewingSession` kamera ve seansı kiracıya göre doğruluyor; yetki belirli bir seansa bağlıysa başka bir seans için kullanılamıyor.
- **CRM:** `CompleteInterview.ConductedBy` ve `LogLeadActivity.PerformedBy` istemciden gelmeye devam ediyor, ama seçilen kullanıcı mevcut kiracıya ait olmak zorunda (`TenantUserGuard`).
- **API hata eşlemesi:** 23503 ikiye ayrıldı:
  - ekleme/güncelleme ihlali → 422, "İlişkili kayıt bulunamadı veya bu kuruma ait değil."
  - silme ihlali → 409, "Bu kayıt başka kayıtlarda kullanıldığı için silinemez."

  Npgsql `Detail` alanını varsayılan olarak gizlediği için ayrım, gizlenmeyen `MessageText` üzerinden yapılıyor; `Detail` doluysa o da kullanılıyor (`PostgresErrorClassifier`).
- **Geri alma:** `db/rollback/V38-V41__tenant_integrity_down.sql`.
- **Doğrulama:**
  - Seed verisinde kiracılar arası referans yoktu (275 FK kontrol edildi).
  - PG17 + Flyway üzerinde V38–V41 uygulandı; down betiğinden sonra FK envanteri V38 öncesiyle birebir aynı çıktı; yeniden uygulama da çalıştı.
  - `tests/Aynesil.IntegrationTests` 8/8 geçti.
  - Layer kaynağı: `db/98_tenant_integrity.sql`. Migration'larla aynı sonucu üretiyor: 202 composite FK, 72 FK için 63 tetikleyici.
  - Bunun için migration'larda olup layer'da eksik olan şu nesneler layer dosyalarına eklendi:
    - `consultancy_agreement` ve `follow_up_activity` tabloları (V26),
    - `core.notification_trigger_config` tablosu (V17),
    - `consultancy_plan.consultancy_type_id` ve `observation_record.observation_type_id` kolonları (V24),
    - `camera.camera_type_id` ve `viewing_authorization.access_type_id` kolonları (V20).
  - Smoke testteki sabit tablo sayısı kontrolü kaldırıldı. Yerine `db/validation/drift.sql` geldi: aynı container'da layer DDL + seed ve Flyway ile iki veritabanı kuruluyor ve katalogları karşılaştırılıyor. Karşılaştırılanlar: tablolar, kolonlar (tip, nullability), FK'lar, unique index'ler, tetikleyiciler, RLS bayrakları ve politikalar.
  - Ayrıca iki veritabanında da bir değişmez kontrolü çalışıyor: `corporation_id` kolonu olan her tabloda RLS açık olmalı ve `tenant_isolation` politikası bulunmalı. İstisna listesindeki 6 tablonun (`menu_item`, dört log tablosu, `outbox_event`) kendi beklenen kurulumları da doğrulanıyor.
  - Kontrolün bulduğu kaymalar kapatıldı. Kural: migration'lar doğru kaynak; migration'ın hatalı olduğu yerler V42 ile düzeltildi.
    - **V42 (migration hatası):**
      - #87 (iki consultancy tablosunda RLS ve updated_at tetikleyicisi yoktu).
      - `media.viewing_log.ip_address` tekrar `inet` oldu. Entity `IPAddress` ve EF `HasColumnType("inet")` eşlemesi kullanıyor; V29'un text'e çevirmesi hatalıydı.
    - **Layer migration'lara uyduruldu:**
      - `iam.auth_session.ip_address` ve `core.activity_log.ip_address` text yapıldı (entity'ler `string?`).
      - Eklenenler: `core.notification_trigger_channel` (V17), `kpi_definition.is_active` (V23), `ref_value.deleted_by` (V30), `survey_definition.trg_audit` (V17).
      - Sonradan eklenen 6 tablodaki sıkı `tenant_isolation` politikası 99'a yansıtıldı (V17, V22, V35).
  - `bash db/validation/smoke_test.sh` yeşil bitiyor: kayma yok, değişmez kontrolü geçiyor.

## Ek D — Kiracı taraması (2026-11-12)

2. partide kurulan desen tüm backend'e yayıldı:
- `CorporationId` ve işlemi yapan kişinin id'si `ITenantContext` / `ICurrentUserService` üzerinden alınıyor.
- Üst kayıtlar kiracıya göre doğrulanıyor.
- Veli kimliği oturumdaki portal kullanıcısından türetiliyor.

İstemci artık bu alanları gönderemiyor. Alanlar request/command tiplerinden, controller'ların `[FromQuery]`/route parametrelerinden ve frontend payload tiplerinden kaldırıldı.

### D.1 — İstemciden gelen `CorporationId` kaldırıldı (158 istek tipi: 62 komut, 96 sorgu)

Handler'lar değeri `ITenantContext.RequireCorporationId()` ile alıyor. Tenant yoksa 401 dönüyor. Sorgulardaki isteğe bağlı "kurum filtresi" kaldırıldı; sorgular her zaman oturumdaki kurumla filtreleniyor. Global satır içerebilen tablolarda (şablonlar, KPI tanımları, ref değerleri) filtre `kiracı || corporation_id IS NULL` biçiminde.

| Modül | Komutlar | Sorgular |
|---|---|---|
| Anketler | `CreateParentFeedback`, `CreateSurveyDefinition`, `SubmitSurveyResponse` | `GetSurveys` |
| BEP / Planlar | `CreateAcademicPeriod`, `CreateEducationPlan` | `GetAcademicPeriods`, `GetEducationPlans`, `GetGuardianVisiblePlan`¹, `GetStudentGoalSummaryReport`¹, `GetTrendReport`¹ |
| Bakım ekibi | `AssignCareTeamMember` | — |
| Bildirimler | `CreateNotificationTemplate`, `UpsertNotificationTriggerConfig` | `GetNotificationTemplates`, `GetTriggerConfigs` |
| CRM (Aday) | `CreateLead` | `GetConversionReport`¹, `GetFollowUpsDue`¹, `GetLeads`, `GetPipelineSummary`¹ |
| Değerlendirme | `CreateAssessmentReport`, `CreateAssessmentSession`, `CreateAssessmentTemplate`, `CreateProgramRecommendation` | `GetAssessmentSessions`, `GetAssessmentTemplates` |
| Eğitmenler | `CreateEducator` | `GetEducatorUtilization`¹, `GetEducators` |
| Finans | `ApplyDiscount`, `CreateInvoice`, `CreatePackageDefinition`, `CreatePromotion`, `GrantScholarship`, `PurchaseStudentPackage`, `RecordPayment` | `GetCreditLedger`¹, `GetCreditSummary`, `GetCreditUsageReport`¹, `GetInvoices`, `GetPackageDefinitions`, `GetPackageReport`¹, `GetPayments`, `GetPromotions`, `GetRefunds`¹, `GetRevenueReport`¹, `GetScholarships`, `GetStudentPackages`, `ValidatePromotionCode`¹ |
| Hedefler | `CreateGoalLibrary`, `CreateGoalTemplate`, `CreateStudentGoal` | `GetGoalLibraries`, `GetGoalSuccessRates`¹, `GetGoalTemplates`, `GetStudentGoalSummary`¹, `GetStudentGoals` |
| Kameralar | `RegisterCamera` | `GetCameras`, `GetViewingAuthorizations`, `GetViewingLogs` |
| Kamplar | `AssignCampEducator`, `BulkRecordAttendance`, `BulkRegisterParticipation`, `CreateCampActivity`, `CreateCamp`, `CreateCampReport`, `RecordAttendance`, `RegisterParticipation` | `GetCampPerformance`¹, `GetCamps` |
| Kampüsler | `CreateCampus` | `GetCampuses` |
| Okul Danışmanlığı | `CreateConsultancyAgreement`, `CreateConsultancyPlan`, `CreateConsultancyReport`, `CreateFollowUpActivity`, `CreateInstitution`, `ScheduleSchoolVisit` | `GetAgreementSummary`¹, `GetConsultancyAgreements`, `GetConsultancyOutcomes`¹, `GetConsultancyPlans`, `GetConsultancyReports`, `GetFollowUpActivities`, `GetInstitutionReport`¹, `GetInstitutions`, `GetOpenFollowUpsReport`¹, `GetSchoolVisits`, `GetVisitHistory`¹ |
| Performans / KPI | `BulkComputeSnapshots`, `ComputePerformanceSnapshot`, `CreateKpiDefinition`, `SubmitParentFeedback` | `GetCorporationKpiTrend`¹, `GetEducatorDashboard`¹, `GetEducatorKpiTrend`¹, `GetEducatorKpiValues`¹, `GetEducatorRanking`¹, `GetExecutiveDashboard`¹, `GetKpiCategories`¹, `GetKpiDefinitions`, `GetKpiReport`¹, `GetKpiValues`¹, `GetManagerDashboard`¹, `GetParentFeedback`, `GetPerformanceSnapshots`¹ |
| Programlar | `AssignStudentToProgram`, `CreateEnrollment`, `CreateProgram` | `GetPrograms`, `GetStudentEnrollments`, `GetStudentPrograms` |
| Seans Planlama | `CreateCalendarEntry`, `CreateRecurringSchedule`, `CreateRoom`, `RequestMakeup`, `ScheduleSession` | `GetAttendanceSummary`¹, `GetCalendarEntries`, `GetCampusCalendar`¹, `GetEducatorCalendar`¹, `GetMakeupRequests`, `GetRecurringSchedules`, `GetRooms`, `GetSchoolCalendar`¹, `GetSessions`, `GetStudentAttendance`¹, `GetStudentCalendar`¹ |
| Sözleşme / Rıza | `CreateConsentTemplate`, `CreateContractTemplate`, `GenerateStudentContract`, `GrantConsent` | `GetConsentReport`¹, `GetConsentTemplateVersions`, `GetConsentTemplates`, `GetContractReport`¹, `GetContractTemplateVersions`, `GetContractTemplates`, `GetSignatureReport`¹, `GetStudentConsents`, `GetStudentContracts` |
| Toplantılar | `ScheduleMeeting` | `GetMeetingCalendar`¹, `GetMeetings` |
| Öğrenciler / Veliler | `CreateGuardian`, `CreateStudent` | `GetGuardians`, `GetStudents` |
| İzin Yönetimi | `CarryForwardLeaveBalance`, `InitializeLeaveBalance`, `SubmitLeaveRequest` | `GetLeaveBalances`, `GetLeaveCalendar`¹, `GetLeaveRequests`, `GetLeaveTrends`¹, `GetLeaveUsageReport`¹ |

¹ Bu sorgular yalnızca sözleşme testiyle kapsanıyor (bkz. D.6). Bunlar rapor, dashboard ve trend sorguları; seed kurulumu ağır olduğu için açık isolation testi yazılmadı.

2. partide düzeltilen 12 komut (`UpdateNotificationPreferences`, kamera atama/izleme, kamp dönemi/kaydı, toplantı katılımcı/sonuç/takip, gözlem) da aynı sözleşme testine dahil.

**API sözleşmesi değişiklikleri:**
- **Kaldırılan parametreler:** Yukarıdaki endpoint'lerden `corporationId` sorgu parametresi ve gövde alanı kaldırıldı. Eski istemci bu alanları gönderirse sessizce yok sayılıyor.
- **Rota değişikliği:** `GET /legal/contract-templates/{corporationId}/{code}/versions` ve `…/consent-templates/…` rotalarının yeni hali `GET /legal/contract-templates/{code}/versions` ve `GET /legal/consent-templates/{code}/versions`. Frontend bu rotaları çağırmıyordu.
- **Global kayıt oluşturma kapandı:** `CreateAssessmentTemplate`, `CreateGoalLibrary`, `CreateGoalTemplate` ve `CreateKpiDefinition` komutlarında istemci `corporationId: null` göndererek global (platform) kayıt açabiliyordu. Artık kayıtlar her zaman kiracıya ait. Global kayıtlar yalnızca seed/migration ile oluşturulabiliyor.

### D.2 — Üst kayıt doğrulaması

- Komut handler'larında zaten var olan 48 üst kayıt sorgusuna `CorporationId == tenant` filtresi eklendi. Etkilenen kayıtlar: öğrenci, program, kayıt, fatura, öğrenci paketi, paket tanımı, kamp dönemi/etkinliği/kaydı, kurum, danışmanlık planı, okul ziyareti, sözleşme/rıza şablonu, değerlendirme oturumu, anket, izin bakiyesi/talebi, bakım ekibi ataması.
- Değerlendirme şablonu (`CreateAssessmentSession`) ve hedef şablonu (`CreateStudentGoal`) sorgularına "global ya da kendi kiracısı" filtresi eklendi.
- `GetCreditSummaryQuery` öğrenciyi yalnızca id ile buluyordu; kiracı filtresi eklendi.
- **Tarama sırasında bulunan açık:** Kamp `BulkRecordAttendanceCommand` hiçbir üst kayıt kontrolü yapmıyordu. Başka kiracının dönem ve kayıt id'leriyle çağrıldığında, mevcut kiracı altında yoklama satırı yazıyordu. Artık dönemin kiracıya ait olduğu ve tüm kayıtların o döneme ait olduğu doğrulanıyor; aksi halde 404 dönüyor. Açığı test yakaladı.
- **Kapsam dışı (bkz. #86):** Handler'ların hiç kontrol etmediği alt kimlikler (`studentId`, `educatorId`, `guardianId`, `roomId` vb.) için yeni doğrulama eklenmedi. Bu iş #86'nın composite FK çalışmasıyla birlikte yapılmalı.
  - **Sonradan çözüldü (#86 ✅):**
    - Alt kimlikler artık DB'de korunuyor: iki kiracı tablosu arasındaki 202 FK composite `(corporation_id, x)` oldu; global satırlı tablolara giden 72 FK tetikleyiciyle kontrol ediliyor (V38–V41).
    - Böyle bir ihlal API'de 422 dönüyor.
    - Uygulama düzeyinde ayrıca kontrol ediliyor: `ConductedBy`/`PerformedBy` (`TenantUserGuard`) ve `StartViewingSession`'ın kamera/seans alanları.

### D.3 — Aktör alanları istek bağlamından dolduruluyor

**Komutlardan kaldırılıp `ICurrentUserService.UserId` ile doldurulan 70 alan:**
- `CreatedBy` (18)
- `UpdatedBy` (31)
- `DeletedBy` (8)
- `AuthoredBy` (5: öğrenci vaka notu ve gelişim raporu, kamp raporu, danışmanlık raporu)
- `RecordedBy` (5: kamp yoklaması ve katılımı)
- `AssignedBy` (1: kamp eğitmeni)
- `CompletedBy` (1: takip faaliyeti)
- `SubmitSurveyResponse.RespondentUserId`

**Notlar:**
- Sekiz alan handler tarafından hiç kullanılmıyordu: hukuk şablonları ve KPI tanımları için `CreatedBy`/`UpdatedBy`. Bunlar sadece kaldırıldı; bu tablolarda değeri audit interceptor dolduruyor.
- `UpdateDevelopmentReport` artık yazarı değiştirmiyor. Önceden istemci değeri `AuthoredBy` alanına yazılıyordu. Yazar oluşturma anında bir kez belirleniyor.
- Gelişim raporu formundaki `authoredBy` serbest metin durumu kaldırıldı. Backend bu alanı Guid olarak bekliyordu, yani alan zaten hatalıydı.

**"İşlemi yapan kişi" anlamı taşımadığı için dokunulmayan alanlar:**
- `PreparedBy` (BEP) ve `RecommendedBy` (program önerisi): FK'ları `educators.educator` tablosuna gidiyor, yani bir eğitmeni gösteriyorlar.
- `CompleteInterview.ConductedBy` ve `LogLeadActivity.PerformedBy`: görüşmeyi ya da aktiviteyi başka bir personel yapmış olabilir. Ürün kararı gerekiyor.
- Hedef kullanıcıyı gösteren `UserId` alanları: kullanıcı yönetimi, rol atama, aday atama, toplantı katılımcısı.

### D.4 — Veli portalı

`PortalController` zaten oturumdaki kullanıcıyla çalışıyordu. Velilerin de çağırabildiği dört komutta veli kimliği artık oturumdan türetiliyor. Yardımcı sınıf: `Application/Common/Portal/PortalGuardian.cs`.

- **`StartViewingSession`:** `GuardianId` komuttan kaldırıldı. Kullanıcı veliyse yetkinin o veliye ait olması ve velinin öğrenciye aktif, kamera izinli portal erişiminin bulunması zorunlu; aksi halde 403 dönüyor. Kullanıcı personelse `GuardianId = null` ve davranış değişmedi.
- **`SubmitSurveyResponse`, `SubmitParentFeedback`, `CreateParentFeedback`:** Kullanıcı veliyse kendi veli id'si kullanılıyor; başka bir veli id'si gönderirse 403 dönüyor. `StudentId` verilmişse o veliye bağlı olmalı. Personel veli adına giriş yapabilir, ama verilen `GuardianId` kiracıya ait olmalı.
- **`StartViewingSession.IpAddress`:** Güvenlik kaydındaki IP artık istemciden alınmıyor; sunucudaki bağlantı adresi (`RemoteIpAddress`) yazılıyor.

### D.5 — Frontend

- Payload/sorgu tiplerinden `corporationId` ve aktör alanlarına ait 103 tanım kaldırıldı. `StartViewingPayload.guardianId` de kaldırıldı.
- Servislerdeki konumsal `corporationId` parametreleri kaldırıldı ve ilgili store'lar güncellendi. Etkilenen servisler: takvim, yoklama, BEP raporları, hedef özeti, CRM pipeline, promosyon doğrulama, KPI kategorileri, bildirim tetikleyicileri.
- View'lerdeki `corporationId` kullanımları temizlendi, kullanılmaz kalan auth store değişkenleri ve importları kaldırıldı (79 dosya).
- Kampüs listesindeki kurum filtresi ve formdaki kurum seçici kaldırıldı. Kampüs artık her zaman oturumdaki kuruma ekleniyor.
- DTO'lardaki `corporationId` alanlarına dokunulmadı; yanıtlar bu alanı hâlâ döndürüyor.

### D.6 — Testler (`tests/Aynesil.Tests/TenantScoping/`)

**Sözleşme testleri (`TenantContractTests`):**
- Uygulamadaki hiçbir istek tipi `CorporationId` ya da aktör alanı taşımıyor. Tek istisna, sunucu içinden çağrılan `SendNotificationCommand`.
- Kiracıya bağlı 170 handler'ın her biri, tenant yokken veriye dokunmadan önce 401'e eşlenen `UnauthorizedAccessException` fırlatıyor.

**Komut testleri:** Kiracıya bağlı tüm komutlar için iki tür test yazıldı:
1. Kaydedilen `CorporationId` (ilgili yerlerde aktör id'si de) bağlamdan geliyor.
2. Başka kiracıya ait üst kayıtla yapılan çağrı reddediliyor.

İkinci test, handler'ın bir üst kayıt sorgusu olduğu yerlerde yazıldı. Her ret testi, tek farkı kiracı olan bir başarı testiyle eşleşiyor. Böylece ret, domain doğrulama hatası gibi alakasız bir nedenle geçemiyor.

**Sorgu testleri:** 51 sorguda A ve B kiracısına aynı kayıt seed ediliyor; sorgunun yalnızca A'nın kaydını döndürdüğü doğrulanıyor (`QueryTenantIsolationTests`).

**Portal testleri:** Velinin oturumdan türetildiği ve başka velinin yetkisi, öğrencisi ya da id'siyle yapılan çağrının reddedildiği 10 test (`PortalGuardianTests`).

**Mutasyon kontrolü:** Toplantı ve öğrenci listesi filtreleri geçici olarak kaldırıldığında ilgili testler düştü.

### D.7 — Doğrulama

- `dotnet build Aynesil.sln`: başarılı. Yeni uyarı yok; kalan uyarılar önceden vardı.
- `dotnet test`: 344/344 geçti.
- `npm run type-check`: temiz.
- ESLint çalıştırılamadı, çünkü projede `eslint.config.*` dosyası yok.

**Güncel durum (#86 ve #87 sonrası):**
- `dotnet test tests/Aynesil.Tests`: 354/354. Eklenenler: `ClientChosenReferenceTests` (CRM kullanıcı alanları, izleme kamerası/seansı) ve sözleşme listesine iki CRM komutu.
- `dotnet test tests/Aynesil.IntegrationTests` (Docker, `Category=Integration`): 10/10.
- `bash db/validation/smoke_test.sh`: yeşil. Kapsamı: layer DDL ile Flyway migration'ları arasında kayma yok; her kiracı tablosunda RLS + `tenant_isolation` değişmez kontrolü geçiyor.
