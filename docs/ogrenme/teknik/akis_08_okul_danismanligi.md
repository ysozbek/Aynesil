# Akış 8 (teknik) — Okul danışmanlığı

**Menü (seed V24/V26):** `consultancy` (`/consultancy`, izin `institution:read`); alt: `consultancy.institutions|plans|visits|observations|reports|agreements` (izinler `institution:read`, `consultancy_plan:read`, `school_visit:read`, `observation:read`, `consultancy_report:read`, `consultancy_agreement:read`) + **`consultancy.analytics`** (`/consultancy/analytics`, `institution:read`, V60). Takipler menü seed'inde yok; router'da var. Menü satırlarının `feature_flag`'i `consultancy` (V53); satır modül kapalıyken düşer.
**Modül bayrağı (ön yüz):** tüm `consultancy/*` rotaları `meta.module = Modules.Consultancy` taşır; `router.beforeEach` `useModulesStore().load()` ile `GET /api/modules/me`'yi bekler, modül kapalıysa `forbidden`'a yönlendirir. Ekranlardaki düğmeler `useModule(Modules.Consultancy).visible(izin)` ile (modül açık **ve** izin var) görünür.
**Router:** `/consultancy` (pano), `institutions[/new|/:id|/:id/edit]`, `plans`, `plans/:id`, `visits`, `visits/:id` (**`VisitDetailView`**), `observations` (ziyaret listesi; satır ziyaret ayrıntısına gider), `reports[?planId|visitId&new=1]`, **`analytics`** (`ConsultancyAnalyticsView`), `agreements[/new|/:id|/:id/edit]`, `follow-ups[/open|/new|/:id|/:id/edit]`.

## Durum sabitleri (statuses.ts)
- `ConsultancyPlanStatus`: draft → active → completed | cancelled
- `SchoolVisitStatus`: planned → completed | cancelled
- `ConsultancyAgreementStatus`: draft → sent → signed → expired | cancelled
- `ConsultancyFollowUpStatus`: pending → in_progress → completed | cancelled

## Ekran / API karşılığı
| İş | Ekran | Uç |
|---|---|---|
| Kurum oluştur/güncelle/sil | `InstitutionFormView`, `InstitutionListView` (satır silme) | `POST/PUT/DELETE /api/consultancy/institutions` |
| Plan oluştur, aktifleştir, tamamla, iptal, sil | `PlanListView/PlanDetailView` (silme yalnızca `draft`) | `plans`, `…/activate|complete|cancel`, `DELETE plans/{id}` |
| Ziyaret oluştur | `VisitListView` (kurum + aktif plan seçici) | `POST visits` |
| Ziyaret ayrıntı, düzenle, tamamla, iptal, sil | `VisitDetailView` (`consultancy.store`: `fetchVisit/updateVisit/completeVisit/cancelVisit/deleteVisit`) | `GET/PUT/DELETE visits/{id}`, `visits/{id}/complete|cancel` |
| Gözlem ekle/oku/düzenle/sil | `VisitDetailView` (ziyaret yanıtındaki `observations`; her değişiklikte ziyaret yeniden okunur); `ObservationListView` ziyaret listesi | `POST visits/{visitId}/observations`, `GET visits/{visitId}/observations`, `GET/PUT/DELETE observations/{id}` |
| Rapor oluştur/ayrıntı/sil | `ReportListView` (satıra tıkla → özet; `schoolVisitId` filtresi) | `POST reports`, `GET reports/{id}`, `DELETE reports/{id}` |
| Sözleşme | oluştur, güncelle, gönder, imzala, süresi doldu, iptal, sil | `agreements/*` |
| Takip | oluştur, güncelle, başlat, tamamla, iptal, **sil** (`FollowUpDetailView`) | `follow-ups/*`, `reporting/follow-ups/open` |
| Raporlama | `ConsultancyAnalyticsView` (4 sekme, her biri kendi iznine bağlı) | `reporting/agreements`, `reporting/institutions`, `reporting/outcomes`, `reporting/visit-history` |

## İş kuralları (Messages.resx)
- **Silme engelleri (iş kuralı hatası = HTTP 409, ham FK hatası değil):** ziyaret → `Rule.OnlyPlannedVisitDelete`, `VisitHasObservations`, `VisitHasReports`, `VisitHasFollowUps`; plan → `OnlyDraftPlanDeleteCancel`, `PlanHasVisits`, `PlanHasAgreements` (`IgnoreQueryFilters`: soft-delete edilmiş sözleşme de FK RESTRICT ile tutar), `PlanHasReports`, `PlanHasFollowUps`; kurum → `InstitutionHasActivePlans`, `InstitutionHasRecords` (plan/ziyaret/sözleşme; kurum soft-delete); takip → `FollowUpDeleteCompleted`. Testler: `tests/Aynesil.Tests/TenantScoping/ConsultancyDeleteRulesTests.cs`.
- **Gözlem kilidi (409):** `ObservationRules.EnsureNotLockedAsync` (rapor varsa) + ziyaret durumu; testler `ObservationLockRulesTests.cs`. Ekran: `VisitDetailView` `canEditObservation` / `canDeleteObservation`.
- Ziyaret güncellemede plan değişirse `ConsultancyPlanNotActive` (aynı kurum + aktif plan; Schedule ile aynı); ziyaret kiracıyla süzülür.
- Plan: "Yalnızca taslak bir plan etkinleştirilebilir. Mevcut durum: '{0}'." / "Yalnızca aktif bir plan tamamlanabilir…" / "'{0}' durumundaki bir plan iptal edilemez." / "'{0}' durumundaki bir danışmanlık planı değiştirilemez."
- Ziyaret: "'{1}' durumundaki bir ziyaret için '{0}' işlemi yapılamaz. Yalnızca 'planlandı' durumundaki ziyaretler değiştirilebilir."
- Sözleşme: "Yalnızca gönderilmiş bir sözleşme imzalanabilir…" / "Yalnızca imzalanmış bir sözleşmenin süresi dolabilir…" / "'{0}' durumundaki bir sözleşme iptal edilemez." / "…Yalnızca taslak sözleşmeler değiştirilebilir."
- Takip: "Yalnızca bekleyen etkinlikler başlatılabilir…" / "'{1}' durumundaki bir etkinlik için '{0}' işlemi yapılamaz."
- Alan kuralları: "{0} zorunludur.", "{0}, {1} alanından önce olamaz.", "{0} alanlarından en az biri verilmelidir." (rapor, takip)

## Koddaki yeri
- Vue: `frontend/aynesil-web/src/views/consultancy/*` · Store: `stores/consultancy.store.ts`, `institution.store.ts`, `followUp.store.ts` · Servis: `services/consultancy.service.ts`
- Controller: `src/Aynesil.Api/Controllers/ConsultancyController.cs` (`api/consultancy`)
- Application: `src/Aynesil.Application/Features/Consultancy/` · Domain: `src/Aynesil.Domain/Modules/Consultancy/Entities/`
- DB: `consultancy.institution`, `consultancy_plan`, `school_visit`, `observation_record`, `consultancy_report`, `consultancy_agreement`, `follow_up_activity` (V24, V26, RLS düzeltmesi V42)

## Demo
`demo_a`: okul → imzalı sözleşme → aktif plan → 2 ziyaret (1 tamamlanmış) → gözlem → ziyaret raporu → başlatılmış takip (demo-seed `consultancy`); smoke-api `consultancyFlows` aynı akışı yazma + silme engelleri + raporlama uçlarıyla doğrular. Hesap: `koordinator@demo-a.test` / `admin@demo-a.test`.
