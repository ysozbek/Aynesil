# Akış 7 — Toplantı → sonuç → takip görevleri; izin talebi → onay

İki ilişkili ama bağımsız iş akışı: **Toplantı yönetimi** ve **Eğitimci izin yönetimi**.

## Bölüm A — Toplantı → sonuç → takip görevleri

**Ne işe yarar:** İç/veli/aday/dış toplantıları planlamak, katılımı işaretlemek, kararları yazmak, karardan çıkan işleri atanıp izlenebilir görev yapmak.

**Kim yapar:** `meeting:*` izinli roller (demo: yönetici ve koordinatör tam; eğitmen yalnızca `meeting:read`).

**Menü:** `Toplantılar` (`meetings`; alt: `meetings-list` `/meetings`, `meetings-calendar`, `meetings-follow-ups` — izin `meeting:manage_follow_ups`). Router: `/meetings` (pano), `/meetings/list`, `/meetings/new`, `/meetings/calendar`, `/meetings/follow-ups`, `/meetings/:id`, `/meetings/:id/edit`.

### Durumlar
| Varlık | Geçişler |
|---|---|
| Toplantı (`MeetingStatus`) | `scheduled` → **Tamamla** → `completed` ; `scheduled` → **İptal Et** → `cancelled` |
| Katılımcı (`MeetingAttendance`) | `invited` → `attended` / `absent` / `tentative` (Katıldı / Katılmadı) |
| Takip görevi (`MeetingFollowUpStatus`) | `open` → `in_progress` → `done` ; `cancelled`. **Done ve cancelled geri açılamaz.** |
| Toplantı türü (ref `meeting_type`) | internal, parent, prospect, external |
| Katılımcı türü (DDL) | `user`, `guardian`, `lead`, `external` |

### Adım adım
1. `Toplantılar → Yeni Toplantı` (`meeting:create`): başlık, tür, başlangıç, bitiş (bitiş başlangıçtan sonra olmalı), konum/link.
2. Detay → **Katılımcı Ekle** (`meeting:manage_participants`): kullanıcı/veli/aday ya da dış katılımcı (ad soyad). Toplantı sonrası **Katıldı / Katılmadı** (`meeting:record_attendance`); kaldırma mümkün.
3. **Tamamla** (`meeting:complete`) / **İptal Et** (`meeting:cancel`).
4. **Sonuç Ekle** (`meeting:record_outcome`): özet + kararlar.
5. **Görev Ekle** (`meeting:manage_follow_ups`): eylem, atanan, son tarih. Durum **Tamamla** düğmesi ve durum değiştirme (liste ve detayda). `Toplantılar → Takip Görevleri`: bekleyen ve **gecikmiş** görevler tek listede. Pano "Bekleyen Takip" sayısını gösterir.
6. **Takvim:** `Toplantılar → Takvim` (ay/hafta/gün).
7. Güncelleme/silme: `meeting:update`, `meeting:delete`.

### Kurallar ve mesajlar
| Durum | Mesaj |
|---|---|
| İptal edilmiş toplantıyı güncelle / tamamla | "İptal edilmiş bir toplantı güncellenemez." / "İptal edilmiş bir toplantı tamamlanamaz." |
| Tekrar tamamla / iptal | "Toplantı zaten tamamlanmış." / "Toplantı zaten iptal edilmiş." |
| Tamamlanmış toplantıyı iptal | "Tamamlanmış bir toplantı iptal edilemez." |
| Kapalı takibi düzenle | "Kapatılmış bir takip güncellenemez." |
| Tamamlanmış / iptal takibi yeniden aç | "Tamamlanmış bir takip yeniden açılamaz." / "İptal edilmiş bir takip yeniden açılamaz." |
| Geçersiz durum/tür | "{0} şu değerlerden biri olmalıdır: {1}." |
| Tarih | "{0}, {1} alanından sonra olmalıdır." |

### Koddaki yeri
- Vue: `views/meetings/*` (`MeetingDashboardView`, `MeetingListView`, `MeetingFormView`, `MeetingDetailView`, `MeetingCalendarView`, `FollowUpListView`)
- Store/servis: `stores/meeting.store.ts`, `meetingCalendar.store.ts`; `services/meeting.service.ts`
- Controller: `MeetingsController.cs` · Command/Query: `Application/Features/Meetings/`
- Domain: `Domain/Modules/Ops/Entities/Meeting*.cs` · DB: `ops.meeting`, `meeting_participant`, `meeting_outcome`, `meeting_follow_up`

### Kendin dene
`koordinator@demo-a.test` → `Toplantılar`: 4 toplantı; takip görevleri tamamlanmış/gecikmiş/açık. Gecikmiş görevi `Takip Görevleri`'nde bulup **Tamamla**; tamamlanmış göreve geri açmayı API'den deneyip mesajı görün. Yeni toplantı → katılımcı → **Tamamla** → **Sonuç Ekle** → **Görev Ekle**. `egitmen@demo-a.test` yalnızca okur.

---

## Bölüm B — İzin talebi → onay

**Ne işe yarar:** **Eğitimcilerin** (personel) yıllık/hastalık/ücretsiz/saatlik izinlerini talep, onay ve bakiye takibi.

**Kim yapar:** Talep: `leave_request:submit` (demo'da eğitmen: `submit`, `read`, `cancel`, ayrıca `leave_balance:read`). Onay/ret: `leave_request:approve|reject` (koordinatör/yönetici). Bakiye yönetimi: `leave_balance:manage` (İzin → Bakiyeler → **Bakiye Tanımla** = `POST /api/leave/balances`, eğitmen/tür/yıl/hak/birim; **Hak Güncelle** = `PATCH …/{id}/entitlement`).

**Menü:** `İzin` (`leave`/`leave-management`, rota `/leave`; alt: talepler, takvim, bakiyeler, raporlar). Router: `/leave`, `/leave/requests`, `/leave/requests/new|:id|:id/edit`, `/leave/calendar`, `/leave/balances`, `/leave/reports`.

### Durumlar
`LeaveStatus`: `pending` → **Onayla** → `approved` ; `pending` → **Reddet** → `rejected` ; **İptal Et** → `cancelled`. Birim: `day` (Gün) / `hour` (Saat). Tür (ref `leave_type`): annual, sick, unpaid, hourly.

### Adım adım
1. `İzin → Talepler → yeni` (`/leave/requests/new`): **eğitimci seçimi zorunlu** (talep `educators.educator` satırına bağlıdır; oturum kullanıcısı otomatik alınmaz — formdaki yorum: eski form kullanıcı id'sini gönderip reddediliyordu). Tür, birim, başlangıç/bitiş, miktar (pozitif), neden.
2. Çakışma: aynı eğitimci için aynı dönemde bekleyen/onaylı talep varsa: "Aynı eğitimci için bu dönemde bekleyen veya onaylı bir izin talebi zaten var."
3. Bekleyen talep düzenlenebilir (**yalnızca `pending`**): "Yalnızca bekleyen izin talepleri güncellenebilir."
4. **Onay:** detayda onaylayıcı önce **seans etkisini** görür (`GET /api/leave/requests/{id}/session-impact`: izin aralığındaki seanslar), sonra **Onayla** veya **Reddet** (red nedeni alanı: "Lütfen red nedenini belirtiniz..." [doğrulanacak: boş bırakılabiliyor mu]). Onay anında `leave_approval` kaydı yazılır; eğitimcinin **bakiye kaydı varsa** miktar bakiyeden düşer ve yetmezse reddedilir: "Yetersiz izin bakiyesi. Kullanılabilir: {0} {1}, talep edilen: {2} {3}." Bakiye kaydı hiç yoksa kontrol yapılmaz [doğrulanacak: "if a balance record exists" yorumundan].
5. **İptal Et** (`leave_request:cancel`): onaylı iptalde bakiyenin geri yüklenmesi [doğrulanacak: `Restore` metodu var, çağrıldığı yer doğrulanmadı].
6. **Bakiyeler** (`leave_balance:manage`): bakiye başlat, hak ediş ayarla (**Hak**), **Devir İşlemi** (bir yıldan diğerine). **Takvim**: izinlerin takvim görünümü. **Raporlar**: kullanım ve trend (`leave_report:read`).
7. Onaylı izin planlamayı etkiler: veritabanı seviyesinde izin aralığı seans/eğitimci çakışmasında kullanılır (`EXCLUDE USING gist` — CLAUDE.md); seansların otomatik iptal/yeniden atanması **yok**, etkiyi onaylayıcı görür [doğrulanacak].

### Kurallar ve mesajlar
| Durum | Mesaj |
|---|---|
| Onaylanamayan durum | "'{0}' durumundaki bir izin talebi onaylanamaz." / "…reddedilemez." / "…iptal edilemez." |
| Bakiye | "Yetersiz izin bakiyesi. Kullanılabilir: {0} {1}, talep edilen: {2} {3}." / "Yetersiz izin bakiyesi. Hak edilen: {0} {1}, kullanılan: {2} {1}, talep edilen: {3} {1}." |
| Çakışma | "Aynı eğitimci için bu dönemde bekleyen veya onaylı bir izin talebi zaten var." |
| Sayı/tarih | "{0} pozitif olmalıdır." / "{0}, {1} alanından sonra olmalıdır." / "{0} negatif olamaz." |

### Koddaki yeri
- Vue: `views/leave/*` (`LeaveDashboardView`, `LeaveListView`, `LeaveFormView`, `LeaveDetailView`, `LeaveCalendarView`, `LeaveBalanceView`, `LeaveReportsView`)
- Store/servis: `stores/leave.store.ts`, `leaveApproval.store.ts`; `services/leave.service.ts`
- Controller: `LeaveController.cs` · Command/Query: `Application/Features/Leaves/`
- Domain: `Domain/Modules/Ops/Entities/Leave*.cs` · DB: `ops.leave_request`, `leave_approval`, `leave_balance`

### Kendin dene
`demo_a`: 8 bakiye, 4 talep (bekleyen/onaylı/reddedilmiş/iptal). `egitmen@demo-a.test` → yeni talep (kendi eğitimci kaydını seçin; eğitmen izni `submit` dar olabilir [doğrulanacak]). `koordinator@demo-a.test` → bekleyen talep → seans etkisi → **Onayla**; bakiyeyi `İzin → Bakiyeler`'de izleyin; bakiyeden fazla miktarla ikinci talebi onaylayıp hata mesajını görün.
