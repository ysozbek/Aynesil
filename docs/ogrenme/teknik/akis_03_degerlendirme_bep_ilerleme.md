# Akış 3 — Değerlendirme → BEP (plan, hedefler, onay) → ilerleme kaydı

**Ne işe yarar:** Çocuğu ölçmek (değerlendirme), buna göre Bireysel Eğitim Planı (BEP/IEP) hazırlayıp onaylatmak, planın hedeflerini seanslarda takip edip ilerlemeyi kaydetmek, veliye göstermek.

**Kim yapar:** Değerlendirme ekibi (şablon, oturum, rapor), BEP sorumlusu/koordinatör (plan, onay), eğitmen (ilerleme, inceleme). Demo'da eğitmen: `student_goal:*`, `goal_progress:*`, `education_plan:read` ve `add_review`; plan oluşturma/onay yetkisi yok. Koordinatör ve yönetici hepsini yapar.

**Menüler:** `Değerlendirme` (`assessment`; alt: `assessment-dashboard`, `assessment-sessions`, `assessment-templates`) · `BEP` (`bep`/`education-plans`; V31 sonrası rota `/bep`) · `Hedefler` (`goals`; alt: kütüphane `/goals/libraries`, şablonlar, öğrenci hedefleri).

## Durumlar

| Varlık | Geçişler |
|---|---|
| Değerlendirme oturumu (`AssessmentSessionStatus`) | `planned` → `in_progress` (**Başlat**) → `completed` (**Tamamla**) ; `planned/in_progress` → `cancelled` (**İptal Et**) |
| Değerlendirme raporu | taslak → sonuçlandırılmış (**Raporu Sonuçlandır**; sonra değiştirilemez) |
| BEP (`EducationPlanStatus`) | `draft` → **İncelemeye Gönder** → `in_review` → **Onayla** → `approved` → **Aktifleştir** → `active` ; `in_review` → **Reddet** → `draft` ; `approved/active` → **Revize Et** → `revised` (sürüm +1) ; **Kapat** → `closed` (draft/revised/closed kapatılamaz) |
| Öğrenci hedefi (`StudentGoalStatus`) | `active` ↔ `on_hold`, → `achieved` / `discontinued` (**Durumu Değiştir**, `student_goal:change_status`) |
| Onay kararı (`EducationPlanDecision`) | approved / rejected / changes_requested (onay kayıtları `education_plan_approval`'da) |
| İlerleme eğilimi | `improving`, `stable`, `declining` (İyileşiyor / Sabit / Gerileme) |

## Adım adım

### A. Değerlendirme
1. **Şablon (bir kez):** `Değerlendirme → Şablonlar` → oluştur: bölümler (section) ve maddeler (item, yanıt türü). Şablon yeni sürümlenebilir (`/version`), `Publish` izniyle aktif/pasif yapılır (`PATCH …/active`).
2. **Oturum oluştur:** `Değerlendirme → Değerlendirmeler → Oturum Oluştur`: şablon + **aday veya öğrenci** (en az biri zorunlu: "… alanlarından en az biri verilmelidir.") + tarih. Yeni gelen çocuk henüz öğrenci değilse aday üzerinden planlanır; aday öğrenciye dönünce oturum öğrenciye bağlanabilir (`LinkToStudent`) [doğrulanacak: bağlama tetikleyicisi].
3. Oturum detayı sekmeleri: **Genel Bakış**, **Puanlama**, **Rapor**, **Öneriler**.
4. **Başlat** (`assessment_session:start`) → **Puanlama** sekmesinde maddeler doldurulup **Yanıtları Kaydet** (`submit_responses`) → **Tamamla** (toplam puan hesaplanır).
5. **Rapor:** oturum tamamlanmadıkça "Rapor oluşturmak için oturumun tamamlanması gerekir." → **Rapor Oluştur** (özet, bulgular) → **Raporu Sonuçlandır**.
6. **Öneriler:** **Öneri Ekle** — önerilen program, yoğunluk, gerekçe (`program_recommendation`). Bu bilgi, Akış 4'te programa kayıt kararını besler (otomatik kayıt yok).
7. Silme yalnızca `planlandı` durumundaki oturum için ("Yalnızca Planlandı durumundaki oturumlar silinebilir").

### B. BEP
1. **Akademik dönem** tanımlı olmalı (`/api/education-plans/academic-periods`; bitiş başlangıçtan sonra olmalı; "güncel dönem" işaretlenir). Ekran: `BEP → Akademik Dönemler` (`/bep/academic-periods`, `AcademicPeriodListView.vue`; menü `plans-academic` V31'de pasifti, **V54** ile açıldı; eski `/education-plans/academic-periods` yönlendirilir). Uçlar: `GET` liste/tekil, `POST`, `PUT {id}` (`rowVersion`), `POST {id}/set-current`, `DELETE {id}` (`academic_period:read|manage`). Silme: plan bağlıysa `Rule.AcademicPeriodInUse`; güncel dönem → `Rule.AcademicPeriodCurrentDelete` (yeni). Dönem türü `ACADEMIC_TERM` referans verisinden seçilir.
2. **Hedef kaynağı:** `Hedefler → Hedef Kütüphanesi` (kütüphane) ve **Hedef Şablonları** (çok dilli şablon + çeviri). Bunlardan öğrenciye **Öğrenci Hedefi** açılır (`Hedefler → Öğrenci Hedefleri → yeni`): başlangıç değeri, hedef değer, ustalık kriteri, hedef tarihi, sorumlu.
3. `BEP → Plan Oluştur` (`/bep/new`, `education_plan:create`): öğrenci, akademik dönem, başlık, başlangıç/bitiş tarihi. Plan `draft` doğar.
4. Plan detayı → **Hedefler** sekmesi → **Hedef Ekle** (öğrencinin bu plana eklenmemiş hedefleri listelenir; "Hedef yoksa önce oluşturun"). Uzun/kısa vadeli hedefler ayrı gruplanır; **Hedefi Kaldır**, sıralama (`reorder`). (`education_plan:manage_goals`)
5. **İncelemeye Gönder** (`:submit`) → onaylayıcı **Onayla** / **Reddet** (`:approve`; Reddet planı tekrar `draft`'a döndürür) → onaylı planı **Aktifleştir** (`:approve` izni gerekir).
6. **Veli görünürlüğü:** plan detayında "Veli Görüntülüyor" anahtarı (`PATCH …/guardian-visibility`, `:approve` izni). Yalnızca `approved` veya `active` plan için açılır; yoksa "Veli görünürlüğü yalnızca onaylı veya aktif planlar için açılabilir." Veli portalda yalnızca `GuardianVisible = true` planı görür.
7. **İnceleme Ekle** (`:add_review`; tarih, özet, sonuç) — eğitmen de yapabilir. **Revize Et**: değişiklik özeti yazılır, sürüm artar, snapshot `education_plan_revision`'a gider; **Revizyonlar** sekmesinde görülür. **Kapat**: plan sonlanır.

### C. İlerleme kaydı
1. İlerlemenin ana yolu seans içindedir (Akış 4: seans notu + hedef ilerlemesi → `scheduling.session_goal`).
2. Doğrudan kayıt: `Hedefler → Öğrenci Hedefleri → (hedef detayı)` → **İlerleme Kaydet** (`goal_progress:record`): ölçüm tarihi, ölçüm değeri, tamamlanma %, eğilim, not. Kayıtlar `goal_progress`'e eklenir; trend grafiği `GET /api/goals/student-goals/{id}/trend`.
3. **Hedef Paneli** (`/goals`) toplam/aktif/ulaşılan hedef, başarı oranı, gelişim alanına göre kırılım ve trend raporunu gösterir (`GoalReports.Read`).

## Dikkat edilecek kurallar ve hata mesajları

| Durum | Mesaj |
|---|---|
| Taslak olmayanı incelemeye gönder | "Yalnızca taslak bir plan incelemeye gönderilebilir. Mevcut durum: '{0}'." |
| İncelemede olmayanı onayla / reddet | "Yalnızca incelemedeki bir plan onaylanabilir / reddedilebilir. Mevcut durum: '{0}'." |
| Onaysız planı aktifleştir | "Yalnızca onaylı bir plan etkinleştirilebilir. Mevcut durum: '{0}'." |
| Kapatılamayan plan | "'{0}' durumundaki bir plan kapatılamaz." |
| Revize edilemeyen plan (taslak/kapalı) | "'{0}' durumundaki bir plan revize edilemez." |
| Veli görünürlüğü (onaysız plan) | "Veli görünürlüğü yalnızca onaylı veya aktif planlar için açılabilir." |
| Oturum başlat (planlandı değil) | "'{0}' durumundaki bir seans başlatılamaz. Yalnızca 'planlandı' durumundaki seanslar başlatılabilir." |
| Oturum tamamla (devam etmiyor) | "'{0}' durumundaki bir seans tamamlanamaz. Yalnızca 'devam ediyor' durumundaki seanslar tamamlanabilir." |
| Kapalı oturumu yeniden planla | "Tamamlanmış veya iptal edilmiş bir seans yeniden planlanamaz." |
| Sonuçlanmış raporu düzenle / yeniden sonuçlandır | "Kesinleştirilmiş raporlar değiştirilemez ve düzenlenemez." / "Rapor zaten kesinleştirilmiş." |
| Geçersiz eğilim / karar değeri | "{0} şu değerlerden biri olmalıdır: {1}." (örn. improving, stable, declining) |
| Dönem tarihleri | "{0}, {1} alanından sonra olmalıdır." |

- BEP için `ABAC/bakım ekibi` kısıtı: öğrenciye erişim bakım ekibi süzgecinden de geçer (Akış 12) [doğrulanacak: BEP uçlarının tam kapsamı].

## Koddaki yeri

- Vue: `views/assessment/*`, `views/bep/*` (`EducationPlanListView/FormView/DetailView`), `views/goals/*`
- Store/servis: `stores/assessment.store.ts`, `assessmentTemplate.store.ts`, `bep.store.ts`, `goal.store.ts`; `services/assessment.service.ts`, `bep.service.ts`, `goal.service.ts`
- Controller: `AssessmentTemplatesController.cs`, `AssessmentSessionsController.cs`, `EducationPlansController.cs`, `GoalsController.cs`
- Command/Query: `Application/Features/Assessment/`, `Features/Plans/`, `Features/Goals/`
- Domain: `Domain/Modules/Assessment/Entities/`, `Domain/Modules/Education/Entities/` (`EducationPlan`, `StudentGoal`, `GoalProgress`, …)
- DB: `assessment.assessment_template(+_translation)`, `assessment_section`, `assessment_item`, `assessment_session`, `assessment_response`, `assessment_report`, `program_recommendation`; `education.academic_period`, `education_plan`, `education_plan_goal`, `education_plan_approval`, `education_plan_review`, `education_plan_revision`, `goal_library`, `goal_template(+_translation)`, `student_goal`, `goal_progress`

## Kendin dene (demo)

- `koordinator@demo-a.test`: `BEP`'te 8 plan var (taslak/incelemede/onaylı/aktif). Taslağı **İncelemeye Gönder** → **Onayla** → **Aktifleştir**; sonra aktif planda **Revize Et** ve **Veli Görüntülüyor**'u aç. Hatalı geçiş için aktif planda tekrar **Onayla** görünmez; API'den denerseniz mesajı görürsünüz.
- `egitmen@demo-a.test`: `Hedefler → Öğrenci Hedefleri`: 24 hedef/96 ilerleme kaydı var. Bir hedefe **İlerleme Kaydet**. BEP'te sadece görüntüler ve **İnceleme Ekle** yapar.
- `Değerlendirme`: 1 şablon, 4 oturum — birini **Başlat → Yanıtları Kaydet → Tamamla → Rapor Oluştur → Raporu Sonuçlandır** sırasıyla geçin.
- Veli etkisi: planı veli-görünür yapıp `veli1@demo-a.test` ile `Portal → Çocuklarım → Hedefler`'e bakın.
