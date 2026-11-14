# Ekranı olmayan endpoint'ler

Tarih: 2026-11-14 · Kaynak: `docs/uyusmazlik-raporu.md` §3 (79 endpoint, eski) → güncel envanter.

## 1. Yöntem

İlk liste yalnızca "frontend'in hiç çağırmadığı" uçları sayıyordu. Servis dosyasında metodu tanımlı ama **hiçbir ekranın çağırmadığı** uçlar (ör. telafi atama, iade işleme) orada "kullanılıyor" görünüyordu. Bu turda iki seviyeli bir tarama yapıldı:

1. **Frontend çağrısı yok:** `src/Aynesil.Api/Controllers/*` yönlendirmeleri (HttpGet/Post/…, sınıf `[Route]`'u, `[HasPermission]`) çıkarıldı; `frontend/aynesil-web/src` altında `apiService`/`api` ile çağrılan tüm yollarla eşlendi (şablonlu yollar normalize edildi).
2. **Servis var, ekran yok:** her servis metodunun (`services/*.ts`) adı; `views/`, `components/`, `composables/`, `router/`, `layouts/` içinde, ya da bir store fonksiyonu üzerinden bunlardan birinde kullanılıp kullanılmadığına bakıldı (store → ekran zinciri bir seviye).

Sonuç: **başlangıçta 150**, ilk turda **27 uç kapatıldı** (123 kaldı), ikinci turda **8 uç daha kapatıldı** (§2, "İkinci tur"); kalan **115** (bu belge). Tarama ad tabanlıdır; yanlış pozitif/negatif olabilir (ör. bir çeviri anahtarındaki aynı ad bir ucu "kullanılıyor" gösterebilir; kamera `POST /api/cameras/{id}/sessions` böyle elle düzeltildi). Eski "79" sayısı yalnızca 1. seviyeyi kapsıyordu; fark buradan.

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

**Seans notu düzenlendi bilgisi (güncellendi, V55):** `scheduling.session_note`'a `edited_at timestamptz` + `edited_by uuid` eklendi; **yalnızca not metni değişince** dolar (salt görünürlük değişikliği düzenleme sayılmaz; o iz `core.audit_log`'ta). Personel ekranı ve **veli portalı** (çocuk detayı → Seanslar, seansın altında) "düzenlendi" gösterir. Portal yalnızca `parent_visible = true` ve silinmemiş notları döner, `CanViewSessions` kapalıysa seans listesiyle birlikte hiç göndermez (`PortalAccessHelper`); görünürlük sonradan kapatılan not portaldan kalkar.

**Kamera izleme ↔ rıza ilişkisi (doğrulandı):** veliye izleme yetkisi (`ViewingAuthorization`) verilirken öğrenci + **o veli** adına verilmiş, süresi geçmemiş `camera_viewing` rızası aranır (`ViewingCommands.cs`, kapı 2). İzleme başlatılırken **yetkinin bağlı olduğu velinin kendi** rızası yeniden kontrol edilir; geri alınmışsa/süresi dolmuşsa izleme başlamaz (`Rule.CameraConsentWithdrawn`). Rıza geri alınınca o veliye + öğrenciye ait açık izleme yetkileri iptal edilir (gerekçe "Rıza geri alındı", V52 `revoked_reason`); diğer velinin yetkisi etkilenmez. (Önceki turda öğrenci düzeyinde kontrol ediliyordu ve geri alma yetkiyi iptal etmiyordu; sıkılaştırıldı.)

## 3. (B) Belirli bir modül kullanılırsa gerekli

**AssessmentSessions** — 1 uç. Öneri güncelleme; öneri ekleme ekranlı.

| Metot | Route | İzin |
|---|---|---|
| PUT | `/api/assessment-sessions/{id}/recommendations/{recommendationId}` | ProgramRecommendations.Update |

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

**Camps** — 26 uç. Kamp modülü: ekranlar liste/oluşturma/kayıt düzeyinde; bu ayrıntı işlemleri (dönem/etkinlik/katılım/yoklama/rapor) kamp kullanan kurumda gerekir.

| Metot | Route | İzin |
|---|---|---|
| DELETE | `/api/camps/activities/{activityId}` | CampActivities.Delete |
| GET | `/api/camps/activities/{activityId}` | CampActivities.Read |
| PUT | `/api/camps/activities/{activityId}` | CampActivities.Update |
| POST | `/api/camps/activities/{activityId}/participations` | CampParticipation.Record |
| POST | `/api/camps/activities/{activityId}/participations/bulk` | CampParticipation.Record |
| GET | `/api/camps/attendance` | CampAttendance.Read |
| PATCH | `/api/camps/attendance/{attendanceId}` | CampAttendance.Record |
| DELETE | `/api/camps/educators/{assignmentId}` | CampEducators.Manage |
| PATCH | `/api/camps/educators/{assignmentId}/role` | CampEducators.Manage |
| POST | `/api/camps/enrollments/{enrollmentId}/complete` | CampEnrollments.Complete |
| GET | `/api/camps/enrollments/{enrollmentId}/reports` | CampReports.Read |
| POST | `/api/camps/enrollments/{enrollmentId}/reports` | CampReports.Create |
| POST | `/api/camps/enrollments/{enrollmentId}/waitlist` | CampEnrollments.Manage |
| GET | `/api/camps/participations` | CampParticipation.Read |
| PATCH | `/api/camps/participations/{participationId}` | CampParticipation.Record |
| GET | `/api/camps/performance` | Camps.Read |
| DELETE | `/api/camps/periods/{periodId}` | Camps.ManagePeriods |
| PUT | `/api/camps/periods/{periodId}` | Camps.ManagePeriods |
| POST | `/api/camps/periods/{periodId}/activities` | CampActivities.Create |
| POST | `/api/camps/periods/{periodId}/attendance/bulk` | CampAttendance.Record |
| POST | `/api/camps/periods/{periodId}/bulk-complete` | CampEnrollments.Complete |
| POST | `/api/camps/{campId}/educators` | CampEducators.Manage |
| GET | `/api/camps/{campId}/periods` | Camps.Read |
| DELETE | `/api/camps/{id}` | Camps.Delete |
| POST | `/api/camps/{id}/activate` | Camps.Activate |
| POST | `/api/camps/{id}/deactivate` | Camps.Activate |

**CareTeam** — 6 uç. Bakım ekibi (ABAC) yönetimi ekranı yok; CLAUDE.md: kısmen devreye alınmış, genişletilmeyecek. Bakım ekibi kullanılacaksa gerekir.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/care-team/my-students` | CareTeam.Read |
| GET | `/api/students/{studentId}/care-team` | CareTeam.Read |
| POST | `/api/students/{studentId}/care-team` | CareTeam.Assign |
| DELETE | `/api/students/{studentId}/care-team/{assignmentId}` | CareTeam.Assign |
| GET | `/api/students/{studentId}/care-team/{assignmentId}` | CareTeam.Read |
| PUT | `/api/students/{studentId}/care-team/{assignmentId}` | CareTeam.Assign |

**Consultancy** — 18 uç. Okul danışmanlığı modülü: kullanan kurumda ziyaret/gözlem/rapor ayrıntı ekranları gerekir.

| Metot | Route | İzin |
|---|---|---|
| DELETE | `/api/consultancy/follow-ups/{id}` | FollowUps.Delete |
| DELETE | `/api/consultancy/institutions/{id}` | Institutions.Delete |
| DELETE | `/api/consultancy/observations/{id}` | Observations.Delete |
| GET | `/api/consultancy/observations/{id}` | Observations.Read |
| PUT | `/api/consultancy/observations/{id}` | Observations.Update |
| DELETE | `/api/consultancy/plans/{id}` | ConsultancyPlans.Delete |
| GET | `/api/consultancy/reporting/agreements` | ConsultancyAgreements.Read |
| GET | `/api/consultancy/reporting/institutions` | Institutions.Read |
| GET | `/api/consultancy/reporting/outcomes` | ConsultancyPlans.Read |
| GET | `/api/consultancy/reporting/visit-history` | SchoolVisits.Read |
| DELETE | `/api/consultancy/reports/{id}` | ConsultancyReports.Delete |
| DELETE | `/api/consultancy/visits/{id}` | SchoolVisits.Delete |
| GET | `/api/consultancy/visits/{id}` | SchoolVisits.Read |
| PUT | `/api/consultancy/visits/{id}` | SchoolVisits.Update |
| POST | `/api/consultancy/visits/{id}/cancel` | SchoolVisits.Cancel |
| POST | `/api/consultancy/visits/{id}/complete` | SchoolVisits.Complete |
| GET | `/api/consultancy/visits/{visitId}/observations` | Observations.Read |
| POST | `/api/consultancy/visits/{visitId}/observations` | Observations.Create |

**EducationPlans** — 1 uç. Veli görünür plan okuması; portal kendi ucunu kullanıyor.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/education-plans/guardian-visible` | EducationPlans.GuardianView |

**EducationPlans** — 1 uç. Hedef özet raporu; rapor ekranı istenirse eklenir.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/education-plans/reports/student-summary` | GoalReports.Read |

**Educators** — 1 uç. Hiyerarşi bitiş tarihi; hiyerarşi ekle/listele ekranlı, bitirme eksik (organizasyon şeması kullanılırsa).

| Metot | Route | İzin |
|---|---|---|
| PATCH | `/api/educators/{id}/hierarchy/{edgeId}/end` | Educators.ManageHierarchy |

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

**Meetings** — 2 uç. Toplantı listesi ham ucu (takvim/ayrıntı ekranlı) ve çıktı güncelleme.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/meetings` | Meetings.Read |
| PUT | `/api/meetings/outcomes/{outcomeId}` | Meetings.RecordOutcome |

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

**Payments** — 1 uç. Taslak faturaya indirim: muhasebe isteğine bağlı; oluşturma akışı fatura detayında yok.

| Metot | Route | İzin |
|---|---|---|
| POST | `/api/payments/discounts` | Discounts.Apply |

**Payments** — 3 uç. Finans ayrıntı/rapor okuması; ekranlı akışları engellemiyor.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/payments/packages/{id}/balance` | StudentPackages.Read |
| GET | `/api/payments/refunds/{id}` | Refunds.Read |
| GET | `/api/payments/reports/credit-usage` | FinanceReports.Read |

**Payments** — 3 uç. Burs/promosyon ayrıntı-güncelleme-doğrulama; liste ve oluşturma ekranlı, bu uçlar ince ayar.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/payments/promotions/validate` | Promotions.Read |
| GET | `/api/payments/scholarships/{id}` | Scholarships.Read |
| PUT | `/api/payments/scholarships/{id}` | Scholarships.Update |

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

**Portal** — 1 uç. Portalda BEP özeti sayfası yok (hedef/rapor/seans sayfaları var).

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/portal/students/{studentId}/bep` | Portal.Access |

**Scheduling** — 3 uç. Takvim kaydı (tatil/etkinlik) oluşturma-listeleme-silme: yalnızca takvim görünümünü etkiler, seans planlamayı engellemez; API ile yapılabiliyor.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/scheduling/calendar-entries` | Sessions.Read |
| POST | `/api/scheduling/calendar-entries` | Sessions.ManageCalendar |
| DELETE | `/api/scheduling/calendar-entries/{id}` | Sessions.ManageCalendar |

**Scheduling** — 1 uç. Oda takvimi görünümü; okul/kampüs/eğitmen/öğrenci takvimleri ekranlı.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/scheduling/calendar/room/{roomId}` | Rooms.Read |

**Scheduling** — 4 uç. Toplu işlem (iptal/oda/yoklama) ve tekrar istisnası; tekil işlemler ekranda var.

| Metot | Route | İzin |
|---|---|---|
| POST | `/api/scheduling/recurring-schedules/{id}/bulk-cancel` | Sessions.BulkCancel |
| POST | `/api/scheduling/recurring-schedules/{id}/bulk-reassign-room` | Sessions.BulkReassign |
| POST | `/api/scheduling/recurring-schedules/{id}/exceptions` | Sessions.Update |
| POST | `/api/scheduling/sessions/{sessionId}/attendance/bulk` | Attendance.Record |

**Surveys** — 9 uç. Anket modülünün frontend servisi/ekranı hiç yok (yalnızca API). Anket kullanılacaksa tümü birlikte yapılmalı.

| Metot | Route | İzin |
|---|---|---|
| GET | `/api/surveys` | Surveys.Read |
| POST | `/api/surveys` | Surveys.Create |
| GET | `/api/surveys/parent-feedback` | ParentFeedback.Read |
| POST | `/api/surveys/parent-feedback` | ParentFeedback.Create |
| GET | `/api/surveys/responses` | Surveys.Read |
| POST | `/api/surveys/responses` | Surveys.Respond |
| DELETE | `/api/surveys/{id}` | Surveys.Delete |
| GET | `/api/surveys/{id}` | Surveys.Read |
| PUT | `/api/surveys/{id}` | Surveys.Update |


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

Menüsü olmayan modüller: `students-surveys` ve `students-care-team` menü satırları pasif (ekran yok); bayrak yine de konmuştur, ekran gelince modül kapalıyken görünmez.

Doğrulama: birim (`ModuleVisibilityTests`: ayrıştırma, `modules` koruma, filtre, menü), entegrasyon (`ModuleVisibilityTests`: yeni kurum kapalı + 403 + menü gizli; aç/kapat anında etkili; kurum yöneticisi açamaz), smoke-api (demo_a: kapat → 403 + menü gizli; aç → geri gelir; kurum ayarıyla açma denemesi korunur).
