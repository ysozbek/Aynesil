# Akış 8 (teknik) — Okul danışmanlığı

**Menü (seed V24/V26):** `consultancy` (`/consultancy`, izin `institution:read`); alt: `consultancy.institutions|plans|visits|observations|reports|agreements` (izinler `institution:read`, `consultancy_plan:read`, `school_visit:read`, `observation:read`, `consultancy_report:read`, `consultancy_agreement:read`). Takipler menü seed'inde yok; router'da var.
**Router:** `/consultancy` (pano), `institutions[/new|/:id|/:id/edit]`, `plans`, `plans/:id`, `visits`, `visits/:id` (aynı `VisitListView`), `observations`, `reports`, `agreements[/new|/:id|/:id/edit]`, `follow-ups[/open|/new|/:id|/:id/edit]`.

## Durum sabitleri (statuses.ts)
- `ConsultancyPlanStatus`: draft → active → completed | cancelled
- `SchoolVisitStatus`: planned → completed | cancelled
- `ConsultancyAgreementStatus`: draft → sent → signed → expired | cancelled
- `ConsultancyFollowUpStatus`: pending → in_progress → completed | cancelled

## Ekran / API karşılığı
| İş | Ekran | Uç |
|---|---|---|
| Kurum oluştur/güncelle | var (`InstitutionFormView`) | `POST/PUT /api/consultancy/institutions`; silme ucu var, ekranda yok |
| Plan oluştur, aktifleştir, tamamla, iptal | var (`PlanListView/PlanDetailView`) | `plans`, `…/activate|complete|cancel` |
| Ziyaret oluştur | var (`VisitListView`) | `POST visits` |
| Ziyaret tamamla/iptal | ekranda **durum seçici** (completed/cancelled) var; `…/complete|cancel` uçlarını store çağırmıyor [doğrulanacak: durum seçici `PUT visits/{id}` mi kullanıyor] | `visits/{id}/complete|cancel` |
| Gözlem | `ObservationListView` yalnızca **listeler** (`createObservation` çağrılmıyor) | `POST visits/{visitId}/observations` |
| Rapor oluştur | var (`ReportListView`) | `POST reports` (silme API'de) |
| Sözleşme | var: oluştur, güncelle, gönder, imzala, süresi doldu, iptal, sil | `agreements/*` |
| Takip | var: oluştur, güncelle, başlat, tamamla, iptal | `follow-ups/*`, `reporting/follow-ups/open` |
| Raporlama uçları | `reporting/institutions|outcomes|visit-history|agreements` — ekranları [doğrulanacak] | |

## İş kuralları (Messages.resx)
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
`demo_a`: 1 okul, plan, 2 ziyaret, gözlem, takip görevi, sözleşme (README). Hesap: `koordinator@demo-a.test` / `admin@demo-a.test`.
