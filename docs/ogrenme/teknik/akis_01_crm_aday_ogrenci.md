# Akış 1 — Aday (CRM) → görüşme → öğrenciye dönüşüm

**Ne işe yarar:** Kuruma başvuran çocuğun/ailenin "aday" olarak kaydedilmesi, takip edilmesi (arama/e-posta/görüşme) ve karar verilince öğrenci kaydına bağlanması.

**Kim yapar:** `lead:*` ve `interview:*` izinlerine sahip roller (demo'da kurum yöneticisi ve koordinatör; eğitmen, muhasebe ve veli bu menüyü görmez).

**Menü:** `CRM` (seed: `crm`, izin `student:read`; alt öğeler `crm-leads`, `crm-pipeline`, `crm-activities`, `crm-reports`). Ayrıca router'da `/crm` (CRM Kontrol Paneli) var.

> Not: Menü seed'inde `crm-interviews` (`/crm/interviews`) satırı var ama V31 ile pasifleştirildi; görüşmeler aday detayındaki sekmeden yönetilir.

## Durumlar

Aday durumu ve pipeline aşaması `statuses.ts`'te **yok**; çünkü referans veri (`ref.ref_value`) olarak tutulur:

| Tür | Varsayılan değerler (seed) |
|---|---|
| `lead_status` | new → contacted → qualified → interview → converted → lost |
| `pipeline_stage` | prospect → contacted → assessment → recommendation → enrollment |
| `lead_source` | website, phone, social_media, referral, walk_in |

Durum geçişlerinde **sıra zorunluluğu yok**: `Lead.ChangeStatus` herhangi bir duruma geçişe izin verir ve her değişikliği `crm.lead_status_history`'ye yazar. (Kurum bu değerleri Referans Veriler ekranından değiştirebilir.)

Görüşme durumu (`LeadInterviewStatus`): `scheduled` → `completed` | `no_show` | `cancelled`; `no_show`/`scheduled`'a yeniden planlanabilir.

## Adım adım

1. **Aday ekle** — `CRM → Adaylar` → **Aday Ekle** (`/crm/leads/new`, izin `lead:create`). Alanlar: iletişim kişisi, telefon, e-posta, çocuk adı/doğum tarihi, kaynak, kampüs, başvuru nedeni, yönlendirme detayı, puan. Kayıt `POST /api/leads`.
2. **Atama** — adayı bir personele atamak `lead:assign` izni ister (`POST /api/leads/{id}/assign`). Arayüzde ayrı düğme yerine aday düzenleme formundaki "Atanan" alanı var [doğrulanacak: Assign ucu ekrandan çağrılıyor mu].
3. **Aktivite kaydet** — aday detayı → **Aktiviteler** sekmesi → **Aktivite Kaydet** (tür, gelen/giden yön, konu, not, takip tarihi). `lead_activity:create`. Takip tarihi verilen aktiviteler `CRM → Aktiviteler` ve Kontrol Paneli'ndeki "Yaklaşan Takipler"de listelenir (süresi geçmiş olanlar "Süresi Geçmiş").
4. **Durum değiştir** — aday detayı → **Durum Değiştir** (durum + isteğe bağlı pipeline aşaması). `POST /api/leads/{id}/status`, `lead:update`. İstemci satırın `rowVersion` değerini gönderir; başkası arada değiştirmişse eşzamanlılık hatası alırsınız. **Pipeline** ekranı adayları aşamalara göre gruplar.
5. **Görüşme planla** — aday detayı → **Görüşmeler** sekmesi → **Görüşme Planla** (kampüs, planlanan tarih). `interview:create`. Geçmiş tarih API'de reddedilir (doğrulayıcı `ScheduledAt` kuralı).
6. **Görüşmeyi tamamla** — **Görüşmeyi Tamamla**: sonuç + öneri yazılır (`interview:manage`). Aday detayı → Görüşmeler sekmesinde planlı görüşme için **Tamamla**, **Ertele** (`/api/interviews/{id}/reschedule`, yeni zaman), **Gelmedi** (`…/no-show`, onaylı) ve **İptal** (`…/cancel`) düğmeleri vardır (`interview:manage`).
7. **Öğrenciye dönüştür** — aday detayı → **Öğrenciye Dönüştür** (`/crm/leads/:id/convert`, izin `lead:convert`). İki mod:
   - **Yeni öğrenci oluştur:** form adaydan dolar (kampüs, başvuru nedeni aktarılır). İsteğe bağlı "İletişim kişisini veli olarak ekle" → veli oluşturulur ve öğrenciye **birincil veli + mali sorumlu** olarak bağlanır. Düğme: **Öğrenciyi Oluştur ve Dönüştür**. Sırayla 3 API çağrısı yapılır: öğrenci oluştur → (veli oluştur + bağla) → `POST /api/leads/{id}/convert`.
   - **Var olan öğrenciye bağla:** öğrenci adıyla aranıp seçilir → **Dönüştür**.
8. Dönüşünce adayın `ConvertedStudentId`/`ConvertedAt` alanı dolar, "Dönüştürüldü" rozeti görünür; öğrencide kaynak aday izi tutulur.

## Dikkat edilecek kurallar ve hata mesajları

| Durum | Mesaj (Messages.resx) |
|---|---|
| Aday zaten dönüştürülmüş | "Aday zaten öğrenciye dönüştürülmüş." (`Rule.LeadAlreadyConverted`); ekranda "Bu aday zaten öğrenciye dönüştürülmüş." |
| Öğrencinin zaten bir kaynak adayı var | "{0} öğrencisinin zaten bir kaynak adayı var (aday kimliği: {1}). Yeniden bağlanamaz." (`Student.SetLeadOrigin`) |
| Tamamlanmış görüşmeyi iptal | "Tamamlanmış bir görüşme iptal edilemez." |
| İptal edilmiş görüşmeyi tamamla | "İptal edilmiş bir görüşme tamamlanamaz." |
| Tamamlanan/iptal görüşmeyi yeniden planla | "'{0}' durumundaki bir görüşme yeniden planlanamaz." |
| Tamamlanan/iptal görüşme "gelmedi" | "Görüşme durumu '{0}' iken gelmedi olarak işaretlenemez." |
| Eşzamanlı düzenleme | `Error.Concurrency` (rowVersion uyuşmazlığı) |

- **Kısmi başarısızlık:** yeni öğrenci oluşturulup dönüşüm adımı patlarsa ekran öğrenciyi seçili bırakır ve şu notu gösterir: "Öğrenci kaydı oluşturuldu ama işlem tamamlanamadı… tekrar 'Dönüştür' diyerek adayı bu öğrenciye bağlayın." Yeniden "Yeni öğrenci" demeyin, çift kayıt olur.
- Dönüşümde **portal erişimi açılmaz**; sonra veli kartından açılır (bkz. Akış 2).
- Rapor: `CRM → Raporlar` tarih aralığına göre toplam/dönüşen/dönüşüm oranı ve kaynağa göre kırılım (`GET /api/leads/reports/conversion`).

## Koddaki yeri

- Vue: `frontend/aynesil-web/src/views/crm/` (`LeadsView`, `LeadFormView`, `LeadDetailView`, `LeadConvertView`, `PipelineView`, `ActivitiesView`, `ReportsView`, `CrmDashboardView`)
- Store/servis: `src/stores/lead.store.ts`, `src/stores/leadActivity.store.ts`, `src/services/lead.service.ts`, (dönüşümde) `src/services/student.service.ts`
- Controller: `src/Aynesil.Api/Controllers/LeadsController.cs`, `InterviewsController.cs`
- Command/Query: `src/Aynesil.Application/Features/Leads/` (`CreateLeadCommand`, `ChangeLeadStatusCommand`, `ConvertLeadToStudentCommand`, `LogLeadActivityCommand`, `ScheduleInterviewCommand`, `ManageInterviewCommands`, …)
- Domain: `src/Aynesil.Domain/Modules/Crm/Entities/Lead.cs`, `Interview.cs`
- DB: `crm.lead`, `crm.lead_activity`, `crm.interview`, `crm.lead_status_history`; referans: `ref.ref_type/ref_value` (`lead_status`, `pipeline_stage`, `lead_source`, `activity_type`)

## Kendin dene (demo)

Giriş: `koordinator@demo-a.test` veya `admin@demo-a.test`, şifre `DemoSifre!2026`. `demo_a`'da aşamalara dağılmış 20 aday, ~42 aktivite, 10 görüşme ve 4 dönüşmüş aday var.
1. `CRM → Adaylar`: dönüşmemiş bir aday seçin → **Aktivite Kaydet** (takip tarihi yarın).
2. **Görüşme Planla** (ileri tarih) → **Görüşmeyi Tamamla**.
3. **Öğrenciye Dönüştür** → "Yeni öğrenci" + "veliyi ekle" → sonra `Öğrenciler`'de yeni öğrenciyi ve velisini görün.
4. Dönüşmüş bir adayda tekrar dönüştürmeyi deneyip "zaten dönüştürülmüş" mesajını görün.
