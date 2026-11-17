# Ekranı olmayan endpoint'ler

Tarih: 2026-11-14 · Kaynak: `docs/uyusmazlik-raporu.md` §3 (79 endpoint, eski) → güncel envanter.

## 1. Yöntem

İlk liste yalnızca "frontend'in hiç çağırmadığı" uçları sayıyordu. Servis dosyasında metodu tanımlı ama **hiçbir ekranın çağırmadığı** uçlar (ör. telafi atama, iade işleme) orada "kullanılıyor" görünüyordu. Bu turda iki seviyeli bir tarama yapıldı:

1. **Frontend çağrısı yok:** `src/Aynesil.Api/Controllers/*` yönlendirmeleri (HttpGet/Post/…, sınıf `[Route]`'u, `[HasPermission]`) çıkarıldı; `frontend/aynesil-web/src` altında `apiService`/`api` ile çağrılan tüm yollarla eşlendi (şablonlu yollar normalize edildi).
2. **Servis var, ekran yok:** her servis metodunun (`services/*.ts`) adı; `views/`, `components/`, `composables/`, `router/`, `layouts/` içinde, ya da bir store fonksiyonu üzerinden bunlardan birinde kullanılıp kullanılmadığına bakıldı (store → ekran zinciri bir seviye).

Sonuç: **başlangıçta 150**, ilk turda **27 uç kapatıldı** (123 kaldı), ikinci turda **8 uç daha kapatıldı** (§2, "İkinci tur"), üçüncü turda **15 uç daha kapatıldı** (§2, "Üçüncü tur"; ek olarak bu turun karar kapanışıyla `DELETE /api/payments/discounts/{id}` ve `GET …/calendar-entries/impact` ekran dahil yazıldı, B listesinde olmadıkları için sayıya girmez); dördüncü turda **6 uç daha kapatıldı** (§2, "Dördüncü tur": bakım ekibi); beşinci turda **18 uç daha kapatıldı** (§2, "Beşinci tur": okul danışmanlığı); altıncı turda **9 uç daha kapatıldı** (§2, "Altıncı tur": anketler; ek olarak sonuç özeti, bekleyen anketler ve "benim geri bildirimlerim" uçları ekran dahil yazıldı); yedinci turda **11 uç daha kapatıldı** (§2, "Yedinci tur": kamp yönetimi); sekizinci turda **14 uç daha kapatıldı** (§2, "Sekizinci tur": kampın günlük işleri); kalan **42** (bu belge). Tarama ad tabanlıdır; yanlış pozitif/negatif olabilir (ör. bir çeviri anahtarındaki aynı ad bir ucu "kullanılıyor" gösterebilir; kamera `POST /api/cameras/{id}/sessions` böyle elle düzeltildi). Eski "79" sayısı yalnızca 1. seviyeyi kapsıyordu; fark buradan.

Gruplar: **(A)** yarım kalmış iş akışı — canlıdan önce ekran gerekli · **(B)** belirli bir modül kullanılırsa gerekli · **(C)** kullanılmayan/ölü/ekranı açılmaması gereken.

## 2. (A) Yarım kalmış iş akışı — bu turda kapatılanlar

Hepsi mevcut ekran desenleriyle (StatusBadge, `utils/datetime`, `constants/permissions`, form/hata gösterimi) yapıldı. Kalan A grubu: **yok** (aşağıdaki B/C gerekçeleri tek tek yazıldı; B'den taşınanlar için "İkinci tur" tablosuna bakın).

| Akış | Uç(lar) | Ekran |
|---|---|---|
| İlk girişte şifre değiştirme zorunluluğu | `POST /api/auth/change-password` (+ V50 `must_change_password`, JWT `mcp`, `PasswordChangeRequiredMiddleware`) | `/account/password` (diğer sayfalar kilitli) |
| Şifremi unuttum | `POST /api/auth/request-password-reset`, `/confirm-password-reset` | `/forgot-password`, `/reset-password` (e-posta henüz gitmediği ekranda yazıyor) |
| Sözleşme/onam şablonu oluştur, düzenle, yeni sürüm | `POST/PUT /api/legal/{contract,consent}-templates`, `…/{id}/new-version` | `/legal/contract-templates`, `/legal/consent-templates` |
| Öğrenci rızası verme / geri alma | `POST /api/legal/consents/grant`, `…/{id}/withdraw` | Öğrenci detayı → Rızalar sekmesi |
| Telafi: seans atama, tamamlama | `POST /api/scheduling/makeup-requests/{id}/assign-session`, `/complete` | Telafi talepleri listesi |
| İade: işleme alma, başarısız işaretleme | `POST /api/payments/refunds/{id}/process`, `/fail` (+ `GET /refunds`) | `/finance/refunds` |
| İzin bakiyesi tanımlama | `POST /api/leave/balances` | İzin bakiyeleri → "Bakiye Tanımla" |
| KPI tanımı oluştur/düzenle | `POST/PUT /api/performance-kpi/definitions[/{id}]` | KPI tanımları |

**Envanterden çıkan ek A işleri (yapıldı):**

| Akış | Uç(lar) | Neden A | Ekran |
|---|---|---|---|
| Öğrenci sözleşmesi oluşturma | `POST /api/legal/contracts` | "Yeni Sözleşme" düğmesi listeye geri dönüyordu; sözleşme ekrandan açılamıyordu | `/legal/contracts/new` form |
| İmzalı sözleşmeyi etkinleştirme, sona erdirme | `POST /api/legal/contracts/{id}/activate`, `/expire` | `signed` durumundan `active`'e geçiren ekran yoktu; sözleşme imzalanıp takılı kalıyordu | Sözleşme detayı |
| Fatura oluşturma | `POST /api/payments/invoices` | `/finance/invoices/new` rotası detay ekranını kimliksiz açıyordu | Fatura listesi → form |
| Aday görüşmesi erteleme / gelmedi | `POST /api/interviews/{id}/reschedule`, `/no-show` | Planlı görüşmede yalnızca tamamla/iptal vardı | Aday detayı → Görüşmeler |
| Toplu KPI hesaplama | `POST /api/performance-kpi/compute/bulk` | "Dönem Hesapla" düğmesi hiçbir şey yapmıyordu (yalnızca yeniliyordu); otomatik hesaplama işi de kayıtlı değil | KPI anlık görüntüleri |

Kapanışla birlikte düzeltilen backend boşlukları: telafi seansı atanırken seansın katılımcısı talep sahibi öğrenci olmalı (`Rule.MakeupSessionStudentMismatch`); KPI `spec` geçerli JSON nesnesi olmalı, güncellemede platform tanımı salt okunur + `rowVersion` kontrolü.

**İkinci tur — B'den A'ya taşınıp ekranı yapılanlar (2026-11-14):**

| Akış | Uç(lar) | Kural | Ekran |
|---|---|---|---|
| Seans notu düzenleme | `PUT /api/scheduling/sessions/{sessionId}/notes/{noteId}` | Yalnızca notu yazan eğitmen düzenler (`authored_by` = oturumdaki kullanıcıya bağlı eğitmen); başkasının notunu `session_note:delete` (yönetici) yetkisi olan düzenleyebilir, aksi halde 403 `Rule.SessionNoteAuthorOnly`. Eski `rowVersion` → 409. | Seans detayı → Notlar → kalem simgesi |
| Seans hedefi çıkarma | `DELETE /api/scheduling/sessions/{sessionId}/goals/{studentGoalId}` | Öğrencinin hedefi silinmez, yalnızca seansla bağı (ilerleme notu/ölçüm) kalkar; seansta olmayan hedef → 404 `Rule.GoalNotInSession`. Onay penceresi var. | Seans detayı → Hedefler → ✕ |
| Akademik dönem: liste, oluştur, düzenle, güncel yap, sil | `GET/PUT/DELETE /api/education-plans/academic-periods/{id}`, `POST …/{id}/set-current` (+ mevcut `GET`/`POST` liste/oluştur) | Silme: eğitim planı bağlıysa `Rule.AcademicPeriodInUse`; **güncel dönem silinemez** (yeni: `Rule.AcademicPeriodCurrentDelete`, önce başka dönem güncel yapılır). Düzenleme `rowVersion` ister. | `/bep/academic-periods` (menü `plans-academic`: V31'de pasifti, **V54** ile açıldı ve yola hizalandı; eski `/education-plans/academic-periods` yönlendirilir) |
| Taslak sözleşmeyi düzenleme ve silme | `PUT /api/legal/contracts/{id}`, `DELETE /api/legal/contracts/{id}` | **Yalnızca `draft`:** gönderilmiş/imzalı/aktif sözleşmede düzenleme 409 `Rule.ContractModifyStatus`, silme 409 `Rule.OnlyDraftContractDelete` (önceden düzenleme `sent` için de açıktı; sıkılaştırıldı). Düzenleme `rowVersion` ister; yeni veli sözleşmenin öğrencisine bağlı olmalı. Silme soft delete. | Sözleşme detayı → Düzenle / Sil (yalnızca taslakta görünür) |

**Üçüncü tur — B'den A'ya taşınıp ekranı yapılanlar: kurumların günlük işi (2026-11-15):**

| Akış | Uç(lar) | Kural | Ekran |
|---|---|---|---|
| Takvim kayıtları (tatil / kapalı gün / dönem arası / etkinlik): liste, oluştur, sil | `GET/POST /api/scheduling/calendar-entries`, `DELETE …/{id}` | Kurum geneli ya da kampüse özel; tüm gün veya saatli. Takvimin okul/kampüs/oda/eğitimci/öğrenci görünümlerinin hepsinde gösterilir (çok günlü kayıt her güne, "Tüm gün" satırı). **Tekrar eden plandan seans üretimi `holiday/closure/term_break` günlerini atlar** (kontrol edildi: **atlamıyordu**, eklendi) ve atlanan tarih sayısı mevcut sonuç mesajında yazılır (`BulkOperationResultDto.ClosureSkipped`: "{count} tarih, tatil/kapalı gün olduğu için atlandı."). `event` engellemez. Kayıt eklenirken aralıktaki planlı seans sayısı gösterilir (`GET …/calendar-entries/impact`) ve "bu seansları iptal et" seçeneği sunulur (`cancelPlannedSessions`; gerekçe = kaydın adı; seçilmezse seanslar kalır); kaydı silmek iptalleri geri getirmez (ekranda yazar). | `/scheduling/calendar-entries` (takvim → "Tatil ve Kapalı Günler") |
| Tekrar eden plan: toplu iptal | `POST /api/scheduling/recurring-schedules/{id}/bulk-cancel` | Tarih aralığı + zorunlu gerekçe (ekranda); `from ≤ to` sunucuda; ertelenmiş (`rescheduled`) seanslar da dahil; yapılmış/iptal seanslara dokunulmaz; plan yoksa 404. | Tekrar Eden Planlar → satır simgesi |
| Tekrar eden plan: toplu oda değişikliği | `POST …/{id}/bulk-reassign-room` | Yalnızca oda değişir (`Session.ChangeRoom`; önceden seans "ertelendi" oluyordu), plan da yeni odayla devam eder, çakışan tarihler atlanır ve sayılır (`RoomConflictSkipped`); oda aktif olmalı. | aynı |
| Tekrar eden plan: tek tarih istisnası | `POST …/{id}/exceptions` | skip/cancel/reschedule; o tarih için seans zaten üretilmişse istisna ona da uygulanır, etkilenen sayı döner (`NoContent` yerine `BulkOperationResultDto`); aynı tarihe ikinci istisna reddedilir. | aynı |
| Seans detayında toplu yoklama | `POST /api/scheduling/sessions/{id}/attendance/bulk` | "Tümünü Geldi İşaretle", kayıtlı yoklama varsa onay; yalnızca seans katılımcıları (`Rule.ParticipantNotInSession`); aynı öğrenci tekrarında sonuncusu; ön yüz listeyi birleştirir (eskiden ezerdi). | Seans detayı → Devam |
| Toplantı sonucu düzenleme | `PUT /api/meetings/outcomes/{outcomeId}` | Özet/karardan en az biri dolu; tamamlanmış toplantıda ekleme/düzenleme serbest, iptal edilmişte 409 `Rule.MeetingOutcomeCancelled` (ekleme için de). | Toplantı detayı → Sonuçlar → kalem |
| Değerlendirme önerisi düzenleme | `PUT /api/assessment-sessions/{id}/recommendations/{recId}` | Yoğunluk + gerekçe; sunucu `rowVersion`'ı denetler (önceden denetlemiyordu) ve öneri yoldaki oturuma ait olmalı (404). | Değerlendirme detayı → Öneriler → kalem |
| Burs ayrıntı ve düzenleme | `GET/PUT /api/payments/scholarships/{id}` | Yüzde ⊕ tutar; düzenleme doğrulayıcısı oluşturmayla hizalandı (bitiş ≥ başlangıç, not ≤ 1000); `rowVersion` → 409; onaylayan (`approved_by`) düzenlemede değişmez. | Burslar → satır → Ayrıntı/Düzenle |
| Taslak faturaya indirim: uygula, kaldır | `POST /api/payments/discounts`, `DELETE /api/payments/discounts/{id}` | Yalnızca `draft` (kesilmişte kaldırma 409 `Rule.DiscountRemoveNeedsDraftInvoice`); gerekçe ekranda zorunlu; **hata düzeltildi:** indirim toplamda iki kez sayılıyordu; yeni kural `Rule.DiscountExceedsSubtotal`. Kaldırınca toplamlar yeniden hesaplanır. | Fatura detayı → İndirim Uygula / Kaldır |
| Veli portalı BEP özeti | `GET /api/portal/students/{studentId}/bep` | `CanViewPlan` bayrağına bağlı; **`approved` ve `active`** + veliye açık plan (önceden yalnızca `approved`: plan aktifleşince veliden kayboluyordu). | Portal → çocuk → BEP Özeti |
| Oda takvimi görünümü | `GET /api/scheduling/calendar/room/{roomId}` | `room:read`; açık kurum süzgeci eklendi; kapalı gün kayıtları da gösterilir. | Takvim → "Oda" |
| Eğitmen hiyerarşisinde ilişkiyi sonlandırma | `PATCH /api/educators/{id}/hierarchy/{edgeId}/end` | Kayıt silinmez (`active_to`); `{id}` ilişkinin alt eğitmeni olmalı (404); bitiş ≥ başlangıç; zaten sona ermiş ilişki tekrar sonlandırılamaz. | Eğitmen detayı → Hiyerarşi → Sonlandır |

**Dördüncü tur — B'den A'ya taşınıp ekranı yapılanlar: bakım ekibi (2026-11-16):**

| Akış | Uç(lar) | Kural | Ekran |
|---|---|---|---|
| Bakım ekibi: listele, ekle, düzenle, ekipten çıkar | `GET/POST /api/students/{id}/care-team`, `GET/PUT/DELETE …/{assignmentId}` | Kaldırma kaydı silmez (bugün itibarıyla `ended`); öğrencide tek birincil atama; `rowVersion` → 409 (**liste yanıtına `rowVersion` ve `reason` eklendi**: önceden yoktu, ekrandan güncelle/kaldır yapılamazdı). Ekran yalnızca `care_team` modülü açıkken ve izin varken görünür (`GET /api/modules/me` → `enabledModules`). | Öğrenci detayı → Bakım Ekibi sekmesi (`care_team:read`; ekle/düzenle/çıkar `care_team:assign`) |
| Benim öğrencilerim | `GET /api/care-team/my-students` | Hesabı bir eğitmen kaydına bağlı değilse liste boş. | `/care-team/my-students` (menü: "Benim Öğrencilerim", V59 ile yeniden açıldı) |
| Pano: atanmış eğitmenler | `GET /api/students/{id}/session-educators` (**yeni**, `student:read`) | Önceki alan kaynaksız bir "Yakında" yer tutucusuydu (seans/plan/başka bir yerden türetilmiyordu). Şimdi bakım ekibini gösterir; ekip boşsa öğrencinin iptal/gelmedi olmayan seanslarındaki eğitmenleri "seanslardan" etiketiyle gösterir ve "Ekibe ekle" sunar (erişim vermez, yalnızca atama kısayolu). | Öğrenci panosu |
| Tekrar eden plan: ekip önerisi | (yeni uç yok; `POST …/care-team`) | Sorumlu eğitimci öğrencinin ekibinde değilse "Ekibe ekle" önerilir; zorunlu değildir, plan yine kaydedilir. | Plan oluşturma formu |

Öğrenci bazlı erişim kısıtı (ABAC) **arayüzden açılmaz**; platform ekibi `care_team_abac_enabled` ayarını SQL ile açar (KURULUM_KILAVUZU B.1.3). Entegrasyon testi (`CareTeamAccessTests`): kısıt açıkken ekipte olmayan eğitmen öğrencinin hedeflerini göremez, ekibe eklenince görür, çıkarılınca kaybeder.

Aynı turda bulunup düzeltilen koddaki hatalar: `ExpandWeekly` pencerenin ortasında biten haftanın günlerini üretmiyordu (ör. 1–14 Aralık penceresinde 14 Aralık Pazartesi atlanıyordu); indirim toplama iki kez yazılıyordu; portal BEP aktif planı gizliyordu; öneri güncellemede `rowVersion`/oturum denetimi yoktu.

**Beşinci tur — B'den A'ya taşınıp ekranı yapılanlar: okul danışmanlığı (2026-11-16):**

Tüm modül rotaları (kamp, kamera, KPI, bakım ekibi, danışmanlık) `meta.module` taşır; kural `router/moduleGuard.ts` içinde, testi `npm test` (`tests/moduleGuard.test.mjs`: kapalı modül → `forbidden`/`/403`, kaynakta her modül rotasının bayrağı). Anket rotaları (`surveys`, `portal/surveys`, `portal/feedback`) da `meta.module: Modules.Surveys` taşır; test, bayraklı rotanın tanımlı modül öneki dışında olmasını yakalar: yeni önek `expected` listesine eklenmeli. Ekranlar `useModule(Modules.X).visible(izin)` kullanır. Danışmanlık ekranları `consultancy` modülüne bağlıdır: rotalar `meta.module` ile korunur (kapalıysa `/403`; durum `GET /api/modules/me`), düğmeler `useModule(Modules.Consultancy).visible(izin)` ile görünür (modül açık **ve** izin var).

| Akış | Uç(lar) | Kural | Ekran |
|---|---|---|---|
| Ziyaret ayrıntısı, düzenleme | `GET/PUT /api/consultancy/visits/{id}` | Yalnızca `planned` ziyaret düzenlenir. Plan değişirse aynı kurumun **aktif** planı olmalı (Schedule ile aynı kural; **eklendi**, önceden herhangi bir plan yazılabiliyordu). Başka kiracının ziyareti 404. | `/consultancy/visits/:id` (ziyaret listesi artık ayrıntıya gider; önceden aynı liste ekranı açılıyordu) |
| Ziyaret tamamla / iptal | `POST …/visits/{id}/complete`, `/cancel` | `planned` dışında 409; tamamlanan/iptal edilen ziyaret düzenlenemez. İptal edilmiş ziyarete gözlem eklenemez. | Ziyaret ayrıntısı → üst düğmeler (onaylı) |
| Ziyaret silme | `DELETE …/visits/{id}` | Yalnızca `planned`. Gözlemi (`Rule.VisitHasObservations`), raporu (`Rule.VisitHasReports`) ya da takibi (`Rule.VisitHasFollowUps`) varsa 409 — **rapor/takip için eklendi**: rapor FK'si ham hata verirdi, takipler sessizce cascade silinirdi. | Ziyaret ayrıntısı → "Ziyareti Sil"; sunucu mesajı sayfada gösterilir |
| Gözlem: listele, oku, ekle, düzenle, sil | `GET …/visits/{visitId}/observations`, `GET/PUT/DELETE …/observations/{id}`, `POST …/visits/{visitId}/observations` | **Gözlem kilidi:** planlanmış ziyarette düzenlenir ve silinir; **tamamlanmış ziyarette düzenlenir, silinemez** (409 `ObservationDeleteNeedsPlannedVisit`); iptal edilmiş ziyarette ikisi de kapalı (`ObservationVisitCancelled`, silmede yine `ObservationDeleteNeedsPlannedVisit`); **ziyarete rapor yazılmışsa** düzenleme ve silme kapanır (`ObservationVisitHasReport`). `GET visits/{id}` yanıtı `hasReports` döner, ekran düğmeleri buna göre gizlenir. Gözlem metni zorunlu; tür `observation_type` referansından. Ayrıntı ve liste ziyaret yanıtındaki `observations` ile gelir; düzenleme/silme sonrası ziyaret yeniden okunur. | Ziyaret ayrıntısı → Gözlemler (her gözlemden "Takip Ekle" → `follow-ups/new?visitId&observationId`) |
| Plan silme | `DELETE …/plans/{id}` | Yalnızca `draft`. Ziyaret (`PlanHasVisits`), sözleşme (`PlanHasAgreements` — FK RESTRICT; **soft-delete edilmiş sözleşme de sayılır**), rapor (`PlanHasReports`) ya da takip (`PlanHasFollowUps`) varsa 409. | Plan detayı → "Planı Sil" |
| Kurum silme | `DELETE …/institutions/{id}` | Aktif plan varsa `InstitutionHasActivePlans`; **plan, ziyaret ya da sözleşme varsa `InstitutionHasRecords`** (soft-delete bağlı kayıtları yetim bırakırdı). | Kurumlar listesi → satır çöp kutusu |
| Rapor silme (+ ayrıntı) | `DELETE …/reports/{id}` (+ ekranda `GET …/reports/{id}`) | Bağlı kayıt yok; doğrudan silinir. Rapor oluşturma ziyareti de alır (`schoolVisitId`); ziyaret ayrıntısından "Rapor Yaz" formu açık gelir. | Raporlar listesi (satıra tıkla → özet; sil) |
| Takip silme | `DELETE …/follow-ups/{id}` | Tamamlanmış takip silinemez (`FollowUpDeleteCompleted`). | Takip ayrıntısı → "Görevi Sil" |
| Raporlar sayfası | `GET …/reporting/agreements`, `…/institutions`, `…/outcomes`, `…/visit-history` | Dört sekme; her sekme kendi ucunun iznine bağlı (sözleşme özeti `consultancy_agreement:read`, okul raporu `institution:read`, sonuçlar `consultancy_plan:read`, ziyaret geçmişi `school_visit:read`). | `/consultancy/analytics` (menü: "Raporlar ve Özetler", V60; panodan da bağlantı) |

Uçtan uca demo akışı (demo-seed + smoke-api `consultancyFlows`): okul → plan (taslak) → sözleşme → plan aktif → sözleşme gönder/imzala → ziyaret → gözlem → ziyaret tamamla → rapor → takip. Silme engelleri her biri için denenir.

**Altıncı tur — B'den A'ya taşınıp ekranı yapılanlar: anketler ve veli geri bildirimi (2026-11-16):**

`surveys` modülüne bağlı (rotalar `meta.module`, düğmeler `useModule(Modules.Surveys).visible(izin)`; menü V61).

| Akış | Uç(lar) | Kural | Ekran |
|---|---|---|---|
| Anket listesi, oluşturma, düzenleme, silme | `GET/POST /api/surveys`, `GET/PUT/DELETE /api/surveys/{id}` | Başlık ≤500, hedef `guardian/educator/student`, en az bir soru; çoktan seçmeli soruda en az iki seçenek (**eklendi**, `Validation.ChoiceNeedsOptions`). **Düzenleme artık soru/seçenek de kapsar** (`PUT` gövdesinde `questions`; kimlikli olan güncellenir, kimliksiz eklenir, listede olmayan silinir); **yanıt alınmış anketin soruları kilitli** (409 `Rule.SurveyQuestionsLocked`; yalnızca başlık/açıklama/durum değişir). Başka ankete ait soru/seçenek kimliği 422 `NotPartOfSurvey`. Silme: yanıt yoksa soft-delete, yanıt varsa pasife alır (yanıtlar korunur). Tüm işlemler açık kurum süzgecinden geçer. Liste ve ayrıntı `responseCount` döner. | `/surveys`, `/surveys/new`, `/surveys/:id/edit` (menü: Öğrenciler → Anketler) |
| Sonuçlar (soru bazında özet) | `GET /api/surveys/{id}/results` (**yeni**, `survey:read`) + `GET /api/surveys/responses` (veli/öğrenci adı **eklendi**) | Yalnızca gönderilmiş yanıtlar sayılır. Puan/ölçek: ortalama + dağılım; çoktan seçmeli: seçenek başına sayı (seçenek değerleri sayısalsa ortalama); evet/hayır: dağılım; metin: son 50 yazılı yanıt. | `/surveys/:id/results` |
| Velide bekleyen anketler | `GET /api/surveys/pending` (**yeni**, `survey:respond`) | Velinin kendi (erişimi açık) çocukları için, aktif ve hedefi `guardian` olan, henüz yanıtlanmamış anketler; sorular/seçeneklerle birlikte gelir (veli `survey:read` iznine sahip değildir). Veli olmayan kullanıcıya boş liste. | `/portal/surveys` (menü: Veli Portalı → Anketler) |
| Veli anketi yanıtlar | `POST /api/surveys/responses` | Veli kimliği oturumdan türetilir; başkasının çocuğu 403 `GuardianNoActiveAccess`; seans verilirse çocuklarından birinin seansı olmalı. **Eklendi:** aynı anket × veli × çocuk × seans için ikinci gönderim 409 `Rule.SurveyAlreadySubmitted` (kural dokümanda vardı ama yalnızca "iki kez Submit" olarak kodlanmıştı, fiilen hiç tetiklenmiyordu); zorunlu soru boşsa 422; puan 1–5; aynı soruya iki yanıt 422; veliye yönelik olmayan anket 409 `Rule.SurveyTargetMismatch`. | `/portal/surveys` |
| Veli geri bildirimi: oluştur, listele | `POST/GET /api/surveys/parent-feedback`, `GET …/parent-feedback/mine` (**yeni**, `parent_feedback:create`) | Puan 1–5 ve/veya yorum (≤2000). **Eklendi:** veli yalnızca **kendi çocuğunun** seansını/eğitmenini puanlayabilir (aksi 403); yalnızca seans verilirse puan **seansın baş eğitmenine** yazılır (KPI veli memnuniyeti eğitmen bazlıdır); eğitmen/seans kurum içinde var olmalı (404). `GET parent-feedback` bir veli tarafından çağrılırsa gönderilen süzgeç ne olursa olsun yalnızca kendi kayıtlarını döner; liste veli ve eğitmen adı içerir. | Yönetici: `/surveys/feedback` (`parent_feedback:read`); veli: `/portal/feedback` |

**Yedinci tur — B'den A'ya taşınıp ekranı yapılanlar: kamp yönetimi (2026-11-16):**

`camps` modülüne bağlı (rotalar `meta.module: Modules.Camps`, düğmeler `useModule(Modules.Camps).visible(izin)`; kural yardımcıları `utils/campRules.ts`, testi `npm test`). Bağlı kayıt varsa sunucu 409 `Error.BusinessRule` + anlamlı ileti döner; ekran iletiyi kırmızı uyarıda gösterir.

| Akış | Uç(lar) | Kural (409 = `BusinessRuleViolationException`) | Ekran |
|---|---|---|---|
| Kamp etkinleştir / pasifleştir | `POST /api/camps/{id}/activate`, `/deactivate` (`camp:activate`) | Zaten aktif/pasifse 409. **Pasifleştirme:** bitmemiş (bitiş ≥ bugün) dönemlerinde `enrolled`/`waitlist` kayıt varsa 409 `CampHasOngoingEnrollments`; bitmiş dönemlerin geçmişi engellemez. Pasif kampa kayıt alınamaz (409 `CampInactive`). | `/camps/list` satır işlemi, `/camps/:id` başlık düğmesi (onay penceresi) |
| Kamp silme | `DELETE /api/camps/{id}` (`camp:delete`) | `enrolled` **veya** `waitlist` kaydı varsa 409 `CampHasActiveEnrollments` (eskiden yalnızca `enrolled`); tamamlanmış/ayrılmış kayıtlar engellemez (soft delete). | aynı |
| Dönem düzenle | `PUT /api/camps/periods/{periodId}` (`camp:manage_periods`) | Kontenjan, kayıtlı öğrenci sayısından az olamaz: 409 `CampPeriodCapacityBelowEnrolled`. | `/camps/:id` → Dönemler → Düzenle |
| Dönem sil | `DELETE /api/camps/periods/{periodId}` | Herhangi bir kaydı varsa 409 `CampPeriodHasEnrollments`; etkinliği varsa 409 `CampPeriodHasActivities` (eskiden etkinlikleri sessizce cascade siliyordu). | aynı (kayıtlı/bekleyen varken düğme kapalı) |
| Bekleme listesine alma | `POST /api/camps/enrollments/{id}/waitlist` (`camp_enrollment:manage`) | Yalnızca `enrolled` → 409 `OnlyEnrolledToWaitlist` (eskiden `waitlist`/herhangi aktif durum sessizce geçiyordu). | `/camps/periods/:periodId/enrollments` satırı |
| Kaydı tamamla | `POST /api/camps/enrollments/{id}/complete` (`camp_enrollment:complete`) | Yalnızca `enrolled` (mevcut kural); başlamamış dönemde 409 `CampEnrollmentCompleteNotStarted` (2026-11-17). | aynı (başlamamış dönemde düğme gizli) |
| Dönem sonu toplu tamamla | `POST /api/camps/periods/{periodId}/bulk-complete` → tamamlanan sayı | Dönem yoksa 404 (eskiden 0 dönerdi); henüz başlamamış dönemde 409 `CampPeriodNotStarted`; yalnızca `enrolled` olanlar tamamlanır, bekleyenler etkilenmez. | aynı ("Dönemi Tamamla", başlamamış dönemde/kayıtlı yokken kapalı) |
| Kamp eğitmeni: ata / rol değiştir / kaldır | `POST /api/camps/{campId}/educators`, `PATCH /api/camps/educators/{id}/role`, `DELETE …/{id}` (`camp_educator:manage`) | Aynı kapsamda (kamp/dönem/etkinlik) aynı eğitmen ikinci kez 409 `EducatorAlreadyAtScope`; rol `lead/assistant/observer/supervisor`. Liste yanıtına `educatorName` eklendi. | `/camps/:id` → Görevli Eğitmenler |

Ek (2026-11-17 doğrulaması D3–D6):
- **Yeniden kayıt:** `POST /api/camps/periods/{periodId}/enrollments` çekilmiş (`withdrawn`) öğrenci için yeni satır açmaz, mevcut satırı geri açar (aynı kayıt kimliği). `enrolled` istenip dönem doluysa kayıt `waitlist`'e düşer. Yeni kayıtta dolu dönem hâlâ 409 `CampPeriodFullUseWaitlist`. Kayıtlı/bekleyen/tamamlanmış öğrenci için 409 `StudentAlreadyInCampPeriod` (önceden genel 409 `Error.DuplicateKey`). Ekran: çekilmiş satırda "Yeniden Kaydet".
- **Tekil tamamlama** (`POST /api/camps/enrollments/{id}/complete`): başlamamış dönemde 409 `CampEnrollmentCompleteNotStarted` (toplu tamamlamadaki kural). "Tamamla" düğmesi başlamamış dönemde gizli.
- **Paket:** kayıt oluştururken (ve uç verilmemiş `UpdateEnrollmentPackage` komutunda) `studentPackageId` kaydın öğrencisine ait olmalı, aksi 422 `CampPackageNotStudents`.
- **Kontenjan yarışı:** kayıt ve bekleme listesinden alma dönem satırını `SELECT … FOR UPDATE` ile kilitler; eşzamanlı kayıtlar kontenjanı aşamaz.

Ek: kayıt listesi yanıtına `studentName` eklendi (ekranda artık kimlik yerine ad). Kamp detayındaki dönem bağlantısı hatalıydı (`/camps/periods/:id` rotası yok); `/camps/periods/:periodId/enrollments`'a düzeltildi.

**Sekizinci tur — B'den A'ya taşınıp ekranı yapılanlar: kampın günlük işleri (2026-11-17):**

Aynı modül anahtarı ve desen (`useModule(Modules.Camps).visible(izin)`, `meta.module`; yardımcılar `utils/campRules.ts`, testi `npm test`). **Kural özeti:** katılım ve yoklama yalnızca o dönemin `enrolled` öğrencileri için girilir; tamamlanmış, iptal (`withdrawn`) ya da bekleme listesindeki kayıt için 409 `EnrollmentNotActive`; kamp sonu raporu yalnızca `completed` kayıt için yazılır, aksi halde 409 `CampReportNeedsCompleted`.

| Akış | Uç(lar) | Kural | Ekran |
|---|---|---|---|
| Etkinlik oluştur / düzenle / sil | `POST /api/camps/periods/{periodId}/activities`, `GET/PUT/DELETE /api/camps/activities/{id}` (`camp_activity:*`) | Bitiş, başlangıçtan sonra; kontenjan pozitif. Düzenleme için etkinliğin `rowVersion`'ı gerekir (ekran önce `GET` ile alır). Dönemde etkinlik varsa dönem silinemez (yedinci tur). | `/camps/periods/:periodId` → Etkinlikler |
| Katılım: tekil | `POST /api/camps/activities/{id}/participations` (`camp_participation:record`) | Kayıt, etkinliğin dönemine ait ve `enrolled` olmalı (409 `EnrollmentActivityPeriodMismatch` / `EnrollmentNotActive`); aynı kayıt ikinci kez 409 `ParticipationAlreadyRecorded`; kontenjan dolu 409 `ActivityFull`. Kontenjan yalnızca `registered`/`attended` kayıtları sayar (`absent`/`excused` dolu etkinliğe de yazılabilir; testle kanıtlı). | etkinlik satırı → Katılım |
| Katılım: toplu | `POST …/participations/bulk` → eklenen sayı | **Eklendi:** eskiden hiçbir kontrol yoktu (başka dönemin, tamamlanmış, bekleyen kaydı yazılabiliyordu, kontenjan atlanıyordu). Şimdi her kayıt için tekil uçtaki kurallar uygulanır, hepsi ya da hiçbiri (409); kontenjanı aşan toplu istek 409 `ActivityFull`; mevcut kayıtlar atlanır; pasif/silinmiş etkinlik 404. | aynı ("Değişiklikleri Kaydet", "Kaydı olmayanları Katıldı yap") |
| Katılım: listele / düzelt | `GET /api/camps/participations`, `PATCH …/{id}` | **Eklendi:** kayıt artık `enrolled` değilse (tamamlanmış/iptal) düzeltme 409 `EnrollmentNotActive`. | aynı (satır başına **Kaydet**) |
| Günlük yoklama: tekil | `POST /api/camps/attendance` (`camp_attendance:record`) | Kayıt `enrolled` olmalı (409); aynı gün ikinci kayıt 409 `AttendanceAlreadyRecorded`. (Mevcut kurallar.) | `/camps/periods/:periodId` → Günlük Yoklama |
| Günlük yoklama: toplu | `POST /api/camps/periods/{periodId}/attendance/bulk` → eklenen sayı | Kayıtlar o döneme ait olmalı (aksi 404 `EnrollmentsNotInPeriod`). **Eklendi:** `enrolled` olmayan kayıt varsa 409 `EnrollmentNotActive` ve hiçbir satır yazılmaz (eskiden tamamlanmış/iptal kayıta da yoklama girilebiliyordu); aynı gün kaydı olanlar atlanır. | aynı ("Yoklamayı Kaydet": yeni satırlar toplu uca, değişenler düzeltmeye) |
| Yoklama listele / düzelt | `GET /api/camps/attendance`, `PATCH …/{id}` | `GET`'e **`campPeriodId`** süzgeci eklendi (gün ekranı için); `PATCH` kayıt `enrolled` değilse 409. | aynı; `Devam Özeti` sekmesi `attendance-summary`'yi gösterir |
| Kamp sonu raporu: yaz / listele | `POST/GET /api/camps/enrollments/{id}/reports` (`camp_report:*`) | **Eklendi:** yalnızca `completed` kayıt için yazılır, aksi 409 `CampReportNeedsCompleted`; kayıt yoksa 404. Rapor değiştirilemez/silinemez (şema). Dosya eki (`fileId`) ekranda yok; yalnızca özet metni. | `/camps/periods/:periodId/enrollments` → tamamlanmış satır → Rapor |
| Kamp performans raporu | `GET /api/camps/performance` (`camp:read`) | Kamp bazında dönem/kayıt/tamamlanan/ayrılan, tamamlama ve devam oranı. `TotalEnrolled` yalnızca `enrolled` + `completed` kayıtları sayar; **bekleme listesi ve iptal (`withdrawn`) sayılmaz**. Tamamlama oranı = tamamlanan / bu kayıt sayısı. | `/camps/performance` (kamp panosundan **Performans Raporu**) |

Not: **V62** menüyü düzeltir: "Kamp Raporları" (`camps.reports`) `/camps/performance`'a bağlandı (`camp:read` ile görünür); "Kamp Devam Takibi" (`camps.attendance`) pasif — yoklama ayrı ekran değil, dönem sayfasındaki sekmedir (kamp sayfasındaki dönem satırından **Etkinlik ve Yoklama**). Yoklamada `absent`/`excused` satırında **devamsızlık nedeni** (`ATTENDANCE_REASON`, normal seans yoklamasıyla aynı liste) seçilebilir; toplu uçta `reasonId` ile gider, düzeltmede (`PATCH`) neden ya da durum değişince güncellenir, durum `present`/`late`'e dönerse neden temizlenir.

**KPI veli memnuniyeti bağı (doğrulandı):** `educator.parent_satisfaction` yalnızca `ops.parent_feedback.rating` değerlerinin eğitmen + dönem ortalamasıdır (`ComputePerformanceSnapshot`). Veli geri bildirimi (seans ya da eğitmen ile) bu ölçüte yansır; birim testi (`SurveyModuleTests`) ve smoke-api (`performance-kpi.compute`) bunu kanıtlar. **Anket yanıtları (`survey_response`) bu ölçüte girmez:** anket yanıtında eğitmen bağı yok; ölçüt şu an yalnızca `parent_feedback` kaynaklıdır. (Anket puanlarını eğitmen KPI'ına katmak ayrı bir karar: seans bağlı yanıt → seansın eğitmeni kuralı gerekir.)

**Seans notu düzenlendi bilgisi (güncellendi, V55):** `scheduling.session_note`'a `edited_at timestamptz` + `edited_by uuid` eklendi; **yalnızca not metni değişince** dolar (salt görünürlük değişikliği düzenleme sayılmaz; o iz `core.audit_log`'ta). Personel ekranı ve **veli portalı** (çocuk detayı → Seanslar, seansın altında) "düzenlendi" gösterir. Portal yalnızca `parent_visible = true` ve silinmemiş notları döner, `CanViewSessions` kapalıysa seans listesiyle birlikte hiç göndermez (`PortalAccessHelper`); görünürlük sonradan kapatılan not portaldan kalkar. **V56 (grup seansı sızıntısı):** not `student_id`'ye bağlanır; portal yalnızca velinin kendi çocuğuna bağlı notları gösterir, `student_id` boş not (grup seansı dahil) hiçbir veliye gösterilmez; grup seansında veliye görünür not için öğrenci seçimi zorunludur (422 `Rule.SessionNoteStudentRequired`). Eski veri: tek katılımcılı seanslardaki görünür notlar bağlandı, grup/katılımcısız seanslardakiler kapatıldı (yerel veritabanında 12 bağlandı, 0 kapatıldı).

**Kamera izleme ↔ rıza ilişkisi (doğrulandı):** veliye izleme yetkisi (`ViewingAuthorization`) verilirken öğrenci + **o veli** adına verilmiş, süresi geçmemiş `camera_viewing` rızası aranır (`ViewingCommands.cs`, kapı 2). İzleme başlatılırken **yetkinin bağlı olduğu velinin kendi** rızası yeniden kontrol edilir; geri alınmışsa/süresi dolmuşsa izleme başlamaz (`Rule.CameraConsentWithdrawn`). Rıza geri alınınca o veliye + öğrenciye ait açık izleme yetkileri iptal edilir (gerekçe "Rıza geri alındı", V52 `revoked_reason`); diğer velinin yetkisi etkilenmez. (Önceki turda öğrenci düzeyinde kontrol ediliyordu ve geri alma yetkiyi iptal etmiyordu; sıkılaştırıldı.)

## 3. (B) Belirli bir modül kullanılırsa gerekli

**Auth** — 1 uç. Davet e-postasındaki bağlantıyı doğrular; e-posta sağlayıcısı bağlanmadan (CLAUDE.md) çalışmaz. Davet akışı açılınca ekran gerekir. (Yönetici hesapları bugün `/users` ile parolalı açılıyor.)

| Metot | Route | İzin |
|---|---|---|
| POST | `/api/auth/verify-email` | ANON |

**Camera** — 3 uç. Kamera atama/yetki ayrıntısı; kamera modülü kullanılırsa gerekir.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/cameras/assignments` | CameraAssignment.Read |
| GET | `/api/cameras/authorizations/{id}` | ViewingAuthorization.Read |
| POST | `/api/cameras/{id}/sessions` | CameraAssignment.Manage |

**Camera** — 2 uç. Canlı izleme akışı (yayın) henüz yok; kamera detayındaki "Canlı İzle" düğmesi bu yüzden gizli (53e2ace). Kamera izleme açılırsa gerekir.

| Metot | Route | İzin |
|---|---|---|
| POST | `/api/cameras/viewing/start` | ViewingSession.Start |
| POST | `/api/cameras/viewing/{logId}/end` | ViewingSession.End |

**Camps** — 1 uç. Kamp modülünün yönetim ve günlük işleri ekranlıdır (yedinci ve sekizinci tur); kalan uç, dönem listesini ayrı çeken `GET /api/camps/{campId}/periods` (kamp detay yanıtı dönemleri zaten içerir).

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/camps/{campId}/periods` | Camps.Read |

**EducationPlans** — 1 uç. Veli görünür plan okuması; portal kendi ucunu kullanıyor.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/education-plans/guardian-visible` | EducationPlans.GuardianView |

**EducationPlans** — 1 uç. Hedef özet raporu; rapor ekranı istenirse eklenir.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/education-plans/reports/student-summary` | GoalReports.Read |

**Goals** — 1 uç. Hedef ilerleme okuması; ilerleme ekleme ekranlı, ayrı okuma uçu gereksiz kalmış.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/goals/student-goals/{goalId}/progress` | GoalProgress.Read |

**Legal** — 2 uç. Şablon sürüm geçmişi listesi; liste ekranı "arşiv sürümleri" süzgeciyle aynı bilgiyi veriyor.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/legal/consent-templates/{code}/versions` | ConsentTemplates.Read |
| GET | `/api/legal/contract-templates/{code}/versions` | ContractTemplates.Read |

**Legal** — 1 uç. Rıza kanıt dosyası ekleme: dosya yükleme akışı (depolama) olmadan anlamsız.

| Metot | Route | İzin |
|---|---|---|
| POST | `/api/legal/consents/{id}/evidence` | StudentConsents.AttachEvidence |

**Meetings** — 1 uç. Toplantı listesi ham ucu (takvim/ayrıntı ekranlı; sonuç güncelleme artık ekranda).

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/meetings` | Meetings.Read |

**NotificationTriggers** — 3 uç. Bildirim tetikleyici yönetimi; e-posta/SMS sağlayıcısı bağlanınca anlamlı (şimdilik yalnızca log).

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/notification-triggers` | NotificationTriggers.Manage |
| PUT | `/api/notification-triggers` | NotificationTriggers.Manage |
| DELETE | `/api/notification-triggers/{id}` | NotificationTriggers.Manage |

**Payments** — 2 uç. Elle kredi tüketme/iade; kredi hareketleri seans akışından ve düzeltme ekranından geliyor.

| Metot | Route | İzin |
|---|---|---|
| POST | `/api/payments/credits/consume` | CreditLedger.Consume |
| POST | `/api/payments/credits/refund` | CreditLedger.Grant |

**Payments** — 3 uç. Finans ayrıntı/rapor okuması; ekranlı akışları engellemiyor.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/payments/packages/{id}/balance` | StudentPackages.Read |
| GET | `/api/payments/refunds/{id}` | Refunds.Read |
| GET | `/api/payments/reports/credit-usage` | FinanceReports.Read |

**Payments** — 1 uç. Promosyon doğrulama: ince ayar (burs ayrıntı/düzenleme artık ekranda).

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/payments/promotions/validate` | Promotions.Read |

**Payments** — 1 uç. Ödeme başarısız işaretleme ödeme sağlayıcısı (gateway) bağlanınca anlamlı; elle tahsilatlarda gerekmiyor.

| Metot | Route | İzin |
|---|---|---|
| POST | `/api/payments/transactions/{id}/fail` | FinancePayments.Capture |

**PerformanceKpi** — 7 uç. KPI okuma/rapor uçları (dashboard ve anlık görüntüler dışında); rapor ekranı istenirse eklenir.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/performance-kpi/dashboards/educator/{educatorId}` | KpiDashboard.Read |
| GET | `/api/performance-kpi/educators/{educatorId}/kpi-values` | KpiSnapshots.Read |
| GET | `/api/performance-kpi/kpi-values` | KpiSnapshots.Read |
| GET | `/api/performance-kpi/parent-feedback` | ParentFeedback.Read |
| GET | `/api/performance-kpi/reports/kpi` | KpiReports.Read |
| GET | `/api/performance-kpi/trends/corporation` | KpiReports.Read |
| GET | `/api/performance-kpi/trends/educator/{educatorId}` | KpiReports.Read |

**PerformanceKpi** — 1 uç. Eğitmen bazlı tekil hesaplama; toplu hesaplama (Dönem Hesapla) artık ekranda.

| Metot | Route | İzin |
|---|---|---|
| POST | `/api/performance-kpi/educators/{educatorId}/compute` | Kpi.Compute |

**PerformanceKpi** — 1 uç. Veli geri bildirimi portaldan/anketten gelir; yönetim ekranı değil.

| Metot | Route | İzin |
|---|---|---|
| POST | `/api/performance-kpi/parent-feedback` | ParentFeedback.Submit |

## 4. (C) Kullanılmayan / ölü / ekranı açılmaması gereken

| Modül | Metot | Route | İzin | Neden |
|---|---|---|---|---|
| Auth | POST | `/api/auth/register` | Users.Create | `/users` oluşturma (ekranlı) aynı işi yapıyor; `register` yinelenen yol. |
| Health | GET | `/Health` | - | Altyapı sağlık ucu (yük dengeleyici/izleme çağırır); ekranı olmaz. |
| Legal | DELETE | `/api/legal/consent-templates/{id}` | ConsentTemplates.Delete | Şablon/rıza silme: imzalı sözleşme ve rıza değişmez/kalıcıdır (CLAUDE.md); yeni sürümle ilerlenir. Ekran açılmamalı. |
| Legal | DELETE | `/api/legal/contract-templates/{id}` | ContractTemplates.Delete | Şablon/rıza silme: imzalı sözleşme ve rıza değişmez/kalıcıdır (CLAUDE.md); yeni sürümle ilerlenir. Ekran açılmamalı. (Taslak *öğrenci sözleşmesi* silme artık A'da: yalnızca `draft`.) |
| Programs | GET | `/api/student-programs/{id}` | Enrollments.Read | Öğrenci-program tekil okuma; liste yanıtı yetiyor. |
| ReferenceData | GET | `/api/reference-data/{typeCode}/default` | RefData.Read | Varsayılan değer ucu; arayüz `isDefault` alanını liste yanıtından alıyor. |
| Students | GET | `/api/students/{id}/campuses` | Students.Read | Alt kayıtlar öğrenci detay yanıtında zaten geliyor (tanı/gelişim profili/kampüs/veli); ayrı çağrı gereksiz. |
| Students | GET | `/api/students/{id}/developmental-profiles` | Students.Read | Alt kayıtlar öğrenci detay yanıtında zaten geliyor (tanı/gelişim profili/kampüs/veli); ayrı çağrı gereksiz. |
| Students | GET | `/api/students/{id}/diagnoses` | Students.Read | Alt kayıtlar öğrenci detay yanıtında zaten geliyor (tanı/gelişim profili/kampüs/veli); ayrı çağrı gereksiz. |

## 5. Modül görünürlüğü (kurum bazında aç/kapat)

B grubundaki modüller (kamp, okul danışmanlığı, kamera, anketler, bakım ekibi, KPI) yarım kaldığı için **yeni kurumda kapalı** başlar. Kontrol sonucu: önceden böyle bir mekanizma **yoktu** (`iam.menu_item.feature_flag` sütunu vardı ama hiçbir kod okumuyordu); V53 ile kuruldu.

| Konu | Karar |
|---|---|
| Durum nerede | `core.corporation.settings` JSONB → `{"modules":{"camps":true,"consultancy":true,"camera":true,"surveys":true,"care_team":true,"kpi":true}}`. Yeni tablo/sütun yok. |
| Anahtar yoksa | **Açık** sayılır (geriye dönük uyum; mevcut kurumlar ve testler kırılmaz). Kapatmak için açıkça `false`. V53 mevcut kurumlara hepsini `true` yazar (demo dahil; yazılı anahtar korunur). |
| Kim değiştirir | Yalnızca platform: `devops/set_modules.py` ve `devops/new_tenant.py`. Kurum yöneticisinin `PUT /api/corporations/{id}/settings` isteğinde `modules` gönderilirse **yok sayılır**, mevcut değer korunur. |
| Uçlar | `[RequireModule(Modules.X)]` (CampsController, ConsultancyController, CameraController, SurveysController, CareTeamController, PerformanceKpiController). Kapalıysa **403 `Error.ModuleDisabled`** (kimlik/izin denetiminden sonra). Durum her istekte okunur: komut sonrası API yeniden başlatmak gerekmez. |
| Menü | V53 `feature_flag`'i modül koduyla doldurur (kök + alt öğeler + yol öneki). `GetUserMenuQuery` kapalı modülün düğümünü (ve alt öğelerini) her istekte eler; menü önbelleği modül durumundan bağımsızdır. |
| Kurum ayarları ekranı | `Kurumlar → kurum → Ayarlar`: "Modüller" listesi **salt okunur** (Açık/Kapalı rozeti); serbest JSON alanında `modules` gösterilmez. |
| Yeni kurum | `new_tenant.py` varsayılan olarak 6 modülü **kapalı** yazar; açmak için `--enable-modules camps,kpi` ya da `all`. |
| Demo | `demo-seed` demo kurumlarını `--enable-modules all` ile açar; mevcut `demo_a/b` açık kalır. |
| Sonradan aç/kapat | `python3 devops/set_modules.py --code ornek --enable camps --disable surveys` · `--list` durum · `--check` deneme · `--dry-run` SQL. |

Anket menüsü V61 ile açıldı (`students-surveys` → `/surveys`, `students-feedback` → `/surveys/feedback`, portalda `portal-surveys`, `portal-feedback`; hepsi `feature_flag = surveys`). `students-care-team` V59 ile yeniden açıldı ("Benim Öğrencilerim"); bakım ekibi kapalıyken menüde ve öğrenci detayındaki sekmede görünmez. Ön yüz açık modülleri menüden değil `GET /api/modules/me`'den okur (uç ekransız listesine girmez: `modules.store.ts` çağırır).

Doğrulama: birim (`ModuleVisibilityTests`: ayrıştırma, `modules` koruma, filtre, menü), entegrasyon (`ModuleVisibilityTests`: yeni kurum kapalı + 403 + menü gizli; aç/kapat anında etkili; kurum yöneticisi açamaz), smoke-api (demo_a: kapat → 403 + menü gizli; aç → geri gelir; kurum ayarıyla açma denemesi korunur).
