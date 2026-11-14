# Akış 4 — Program kaydı → tekrarlayan program / seans planlama → yoklama → seans notu → telafi

**Ne işe yarar:** Öğrenciyi aldığı programa yazmak, haftalık seansları üretmek, seansı yapıp yoklama almak, not ve hedef ilerlemesi girmek, gelmeyen öğrenci için telafi hakkı yönetmek. Seans tamamlanınca **paket kredisi otomatik düşer** (Akış 5'e köprü).

**Kim yapar:** Sekreter/planlamacı/koordinatör (program, kayıt, plan, seans, telafi onayı); eğitmen (yoklama, not, tamamla, telafi talebi). Demo'da eğitmen: `session:read`, `session_note:*`, `attendance:*`, `makeup_request:read|request`, `program:read`, `enrollment:read` — seans **oluşturamaz**, tamamlama düğmesi görünmez (`session:complete` demo eğitmen rolünde yok) [doğrulanacak: demo rol regex'i `session:read` ile sınırlı; tamamlamayı koordinatör yapar].

**Menüler:** `Programlar` (`programs`; alt: liste, kayıtlar `/programs/enrollments`, yeni program) · `Seans Planlama` (`scheduling`; alt: seanslar, tekrar eden planlar `/scheduling/recurring-schedules`, odalar, yoklama, telafi `/scheduling/makeup-requests`, takvim).

## Durumlar

| Varlık | Geçişler |
|---|---|
| Kayıt (`education.enrollment`) | aktif → **Durum Değiştir** → … → **Kaydı Sonlandır** (sonlandırma nedeni, bitiş tarihi) |
| Öğrenci programı (`StudentProgramStatus`) | `active` ↔ `paused`, → `completed` / `cancelled` |
| Seans (`SessionStatus`) | `scheduled` → `completed` (**Tamamla**) · `cancelled` (**İptal Et**, gerekçe) · `no_show` · `rescheduled` (**Yeniden Planla**; yeni saat) |
| Yoklama (`AttendanceStatus`) | `present`, `absent`, `late`, `excused`, `left_early`. Seans ekranındaki düğmeler yalnızca *Katıldı / Gelmedi / Geç* gösteriyor; mazeret (`ATTENDANCE_REASON` ref veri) seçilebilir. `excused`/`left_early` API'den [doğrulanacak: UI'da seçenek var mı] |
| Telafi talebi (`MakeupRequestStatus`) | `requested` → **Onayla** → `approved` → *seans ata* → `scheduled` → *tamamla* → `completed` ; **Reddet** → `rejected` ; süre dolunca `expired` |

Seans bir kez `completed`/`cancelled`/`no_show` olduktan sonra durumu değişmez ("Tamamlanmış seansların durumu değiştirilemez." vb.).

## Adım adım

### A. Program ve kayıt
1. **Program** (bir kere): `Programlar → Yeni Program`; programa **hizmet** (`program_service`) eklenir, çeviri girilir.
2. **Kayıt oluştur:** `Programlar → Kayıtlar → Kayıt Oluştur` (öğrenci + program). Öğrencinin aktif kaydı varsa yeni kayıt açılmaz; ekran şunu söyler: "Bu öğrencinin {date} tarihli aktif kaydı var. Yeni kayıt açılmayacak; seçtiğiniz program bu kayda eklenecek." Kayıt açılır ama program ataması patlarsa: "Kayıt oluşturuldu ancak program ataması başarısız oldu: {error}".
3. Kayıt detayı: **Program Ata**, **Programı Düzenle**, **Programı Sonlandır**, **Program Atamasını Kaldır**, **Durum Değiştir**, **Kaydı Sonlandır**.
4. Değerlendirme önerisi (Akış 3) bu karara girdi sağlar ama otomatik kayıt yoktur.

### B. Oda ve planlama
1. `Seans Planlama → Odalar`: oda oluştur (fiziksel/sanal). Sanal oda kampüse bağlanamaz: "Sanal odalar bir kampüse atanamaz." (`ops` takvimi: `calendar_entry` ile kurum tatili/kapanış girilir, `sessions:manage_calendar`.)
2. **Tek seans:** `Seanslar → Seans Oluştur`: başlık, tür, kampüs, oda, saat, öğrenci(ler), eğitimci(ler). Oda dolu ya da eğitimci meşgulse DB/komut reddeder: "{0} odası istenen zaman diliminde zaten dolu." / "{0} eğitimcisi istenen zaman diliminde zaten meşgul."
3. **Tekrarlayan plan:** `Tekrar Eden Planlar → Plan Oluştur`: öğrenci, program, hizmet, eğitimci, kampüs, oda, günler, saat, aralık. Listede **Seansları Oluştur** (`sessions:bulk_generate`) → sonuç: "{count} seans oluşturuldu." + atlananlar: "{count} tarih, salon dolu olduğu için atlandı." / "…eğitimcinin o saatte başka seansı olduğu için atlandı." Ayrıca plan pasifleştirme, istisna ekleme, toplu iptal ve toplu oda değiştirme uçları API'de (`bulk-cancel`, `bulk-reassign-room`) — ekranda yalnızca **Seansları Oluştur** görüldü [doğrulanacak].
4. Seans detayı sekmeleri: **Genel Bakış, Katılımcılar, Yoklama, Notlar, Hedefler**. Katılımcı eklemek: "Öğrenci zaten bu seansın katılımcısı." hatası verebilir. Eğitimci eklemek: "Eğitimci zaten bu seansa atanmış" / çakışma: "{0} eğitimcisi çakışan bir seansta zaten meşgul."
5. **Hedefler** sekmesi → **✕** seans hedefini çıkarır (`DELETE /api/scheduling/sessions/{sessionId}/goals/{studentGoalId}`, `session:manage_goals`; seansta olmayan hedef → 404 `Rule.GoalNotInSession`; `session_goal` satırı silinir, `student_goal` kalır). **Hedef Ekle**: öğrencinin aktif BEP hedeflerinden seçilir (önce seansa öğrenci eklenmeli; yoksa "Hedef eklemek için önce seansa öğrenci ekleyin."). Hedef yoksa "Öğrenciye hedef tanımla" bağlantısı Akış 3'e götürür.

### C. Seans günü: yoklama → not → tamamla
1. **Yoklama** sekmesi: her öğrenci için *Katıldı / Gelmedi / Geç* ve gerekirse mazeret (`attendance:record`; toplu: `…/attendance/bulk`). Kaydedilmeyen "Kaydedilmedi" görünür.
2. **Not Ekle** (`session_note:write`): yazan, oturumdaki kullanıcıya bağlı **eğitmen kaydıdır** (kullanıcı eğitmen kaydına bağlı değilse not yazılamaz [doğrulanacak]; demo seed bu yüzden notları `egitmen@…` ile yazar).
3. **Tamamla** (`session:complete`): onay metni "Yoklamada 'geldi' olan her öğrencinin, seansın programına bağlı paketinden 1 seans hakkı düşülür." Önce yoklama, sonra Tamamla.
4. **Kredi düşümü kuralları** (`SessionCreditConsumer`): yalnızca `present`/`late`/`left_early` olanlar için; program öğrencinin `student_program`'ından, yoksa seansın `program_service` programından bulunur; o programa bağlı, süresi geçmemiş, ≥1 hakkı olan **aktif** paket seçilir (önce en erken bitecek); 1 hak düşer, hak bitince paket `exhausted` olur; aynı seans için ikinci kez düşmez. Uygun paket yoksa **hiçbir şey düşmez** ve seans detayı "Seans hakkı düşülmedi" + ipucu gösterir ("…Gerekirse Kredi Defteri ekranından elle düşün."). Düşünce "{n} seans hakkı düşüldü" yazar.
5. **İptal Et**: gerekçe istenir; kredi düşmez. **Yeniden Planla**: bitiş başlangıçtan sonra olmalı. Gelmedi işareti (`no-show`) `Sessions.Cancel` iznini kullanır.

### D. Telafi
1. Gelmeyen öğrenci için `Telafi Talepleri → Talep Oluştur` (`makeup_request:request`): kaçırılan seans, not. Talepte son tarih (`expiresOn`) görünür.
2. Koordinatör **Onayla** (onay mesajı: "{student} için telafi talebini onaylamak istediğinize emin misiniz?") veya **Reddet** (gerekçe) — `makeup_request:manage`. Ana ekranda "Bekleyen Telafi Talepleri" sayacı.
3. Onaylı talepte **Seans ata** (`POST …/assign-session`; `makeup_request:manage`): seans öğrencinin takviminden (`GET /scheduling/calendar/student/{id}`, 90 gün) seçilir; backend seansın katılımcıları arasında talep sahibi öğrenci yoksa `Rule.MakeupSessionStudentMismatch` ile reddeder. Planlı talepte **Tamamla** (`…/complete`; seansın tamamlanmış olması zorunlu tutulmaz). Süre dolumu (`expired`) için bir uç yok; domain metodu var, tetikleyen mekanizma [doğrulanacak].

## Dikkat edilecek kurallar ve hata mesajları

| Durum | Mesaj |
|---|---|
| Oda/eğitimci çakışması | "{0} odası … zaten dolu." / "{0} eğitimcisi … zaten meşgul." (DB'de `EXCLUDE USING gist` + tetikleyici de korur) |
| Sonlanmış seansı değiştir | "İptal edilmiş bir seansın durumu değiştirilemez." / "Gelmedi olarak işaretlenmiş…" / "Tamamlanmış seansların durumu değiştirilemez." / "Seans '{0}' son durumunda olduğu için değiştirilemez." |
| Tarih hatası | "{0}, {1} alanından sonra olmalıdır." |
| Telafi yanlış durum | "Telafi talebi bu işlem için '{0}' durumunda olmalıdır. Mevcut durum: '{1}'." |
| Telafi reddi/süre | "'{0}' durumundaki bir telafi talebi reddedilemez." / "…süresi doldu olarak işaretlenemez." |
| Plan | "{0} pozitif olmalıdır." / "Eğitimci zaten bu tekrarlayan programa atanmış." |
| Eşzamanlılık | `Error.Concurrency` (satırın `rowVersion`'ı eski) |

- Seans kredisi, yalnızca **paketin programı = seansın programı** ise düşer. Paket tanımında program bağlantısı yoksa seans hakkı **düşmez** (Kullanım Kılavuzu 3.7).
- Sanal oda: `campusId = null`.

## Koddaki yeri

- Vue: `views/programs/*`, `views/scheduling/*` (`SessionDetailView`, `SessionFormView`, `SessionListView`, `RecurringSchedule*`, `RoomListView`, `AttendanceDashboardView`, `MakeupRequestListView`, `CalendarView`, `SchedulingDashboardView`)
- Store/servis: `stores/program.store.ts`, `session.store.ts`, `scheduling.store.ts`, `attendance.store.ts`, `makeupSession.store.ts`, `calendar.store.ts`; `services/program.service.ts`, `scheduling.service.ts`
- Controller: `ProgramsController.cs` (programlar + kayıtlar), `SchedulingController.cs`
- Command/Query: `Application/Features/Programs/`, `Features/Scheduling/Commands/` (`SessionCommands`, `AttendanceCommands`, `MakeupCommands`, `RecurringScheduleCommands`, `SessionNoteCommands`, `RoomCommands`, `CalendarEntryCommands`), `Features/Scheduling/SessionCreditConsumer.cs`
- Domain: `Domain/Modules/Scheduling/Entities/` (`Session`, `MakeupRequest`, `RecurringSchedule`, …), `Domain/Modules/Education/Entities/` (`Enrollment`, `Program`, `StudentProgram`)
- DB: `education.program(+_translation)`, `program_service`, `enrollment`, `student_program`; `scheduling.room`, `session`, `session_participant`, `session_educator`, `session_goal`, `session_note`, `attendance`, `recurring_schedule(+_educator)`, `recurrence_exception`, `makeup_request`, `calendar_entry`; `finance.credit_ledger`

## Kendin dene (demo)

`demo_a`'da ~225 seans (son 8 hafta + 2 hafta ileri; karışık yoklama, iptal, gelmedi, 2 ertelenen), tekrarlayan program, telafi talepleri ve 8 seans notu var.
1. `koordinator@demo-a.test` → `Seanslar`: ileride bir `scheduled` seansı aç → **Katılımcılar**'da öğrenci, **Yoklama**'da *Katıldı* → **Tamamla** → "{n} seans hakkı düşüldü" yazısını ve `Finans → Kredi Defteri`'ndeki yeni `consume` satırını görün. Yoklamasız bir seansı tamamlayıp "düşülmedi" ipucunu görün.
2. `Tekrar Eden Planlar`: bir plan seçip **Seansları Oluştur** (2 haftalık aralık) → atlanan tarih mesajlarını izleyin.
3. `Telafi Talepleri`: bekleyen talepte **Onayla**/**Reddet**.
4. `egitmen@demo-a.test`: kendi seansında yoklama + **Not Ekle**; "Seans Oluştur" düğmesi yok (yetkisiz).
5. `veli1@demo-a.test`: `Portal → Çocuklarım → (çocuk) → Seanslar`.

### Seans notu düzenleme (2. tur kapanışı)
`PUT /api/scheduling/sessions/{sessionId}/notes/{noteId}` (`session_note:write`, gövde: `body`, `parentVisible`, `rowVersion`). `EditSessionNoteCommandHandler`: oturumdaki kullanıcıya bağlı eğitmen (`ActingEducator.FindIdAsync`) = `authored_by` ise düzenler; değilse `session_note:delete` izni (yönetici) gerekir, yoksa 403 `Rule.SessionNoteAuthorOnly`. `rowVersion` uyuşmazsa 409 `Error.Concurrency`. Yazar (`authored_by`) değişmez. Şemada `edited_at/edited_by` yok: ekran "Düzenlendi" rozetini `updated_at − created_at > 2 sn` ile gösterir; ayrıntılı iz `core.audit_log`'ta. Öneri (uygulanmadı): portal notları gösterecekse `edited_at/edited_by` eklemek. Testler: `SessionNoteAuthorTests` (yazar/başkası/yönetici/eski rowVersion), smoke-api `scheduling.editNote*`.
