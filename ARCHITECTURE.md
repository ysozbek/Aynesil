# ARCHITECTURE.md — AyNesil Platform

Bu dosya, Cursor composer/agent oturumlarında `@ARCHITECTURE.md` ile
referans verilen mimari özet dokümanıdır. Detaylı analiz için
`docs/01` … `docs/08` dosyalarına bakılmalıdır; bu dosya onların
kısa, tek-bakışta-okunur özetidir.

Şema durumu: **Onaylı, PostgreSQL 17 üzerinde green-build doğrulanmış**
(`db/validation/smoke_test.sh`). 136 tablo, 15 schema.

---

## Proje Adlandırma

- Ürün/marka adı (UI, başlık, pazarlama): **AyNesil**
- Teknik/kod tarafı (namespace, solution, veritabanı, repo, docker,
  env): **Aynesil** (PascalCase, iç büyük harf yok)
- İlk/öncü tenant (müşteri): **Akran Hareket** — kodda hardcode
  edilmez, sadece `core.corporation` tablosunda bir kayıt.

---

## Katman Mimarisi

```
Layer 1 (core, iam, ref)   <- domain-agnostic, herhangi bir sektöre satılabilir
Layer 2 (crm, students, assessment, educators, education,
         scheduling, finance, legal, media, ops, camps,
         consultancy)      <- Akran Hareket / özel eğitim domain'i
```

Bağımlılık kuralı tek yönlü: **Layer 2 → Layer 1**, asla tersi değil.

Detay: `docs/02_domain_boundaries.md`, `docs/03_module_boundaries.md`

---

## Multi-Tenancy

- Shared schema + `corporation_id` + **RLS default-deny** (tenant
  context yoksa sıfır satır döner).
- **Campus, bir isolation boundary değil, authorization sub-scope'tur**
  — aynı corporation içinde bir filtre, ayrı bir tenant değil.
- Uygulama veritabanına **non-owner (least-privilege) rol** ile
  bağlanır; owner rolü RLS'i bypass eder, migration/seed için
  kullanılır.

Detay: `docs/01_entity_relationships.md` (§1), Opus mimari analizi (§3)

---

## Configurable Reference Data

4 tablolık motor: `ref_type` / `ref_value` / `ref_value_translation` /
`ref_value_tenant_override`. Üç scope (system / global-configurable /
tenant-specific) aynı yapıda. Yeni bir iş listesi eklemek bir
`INSERT`'tür, asla DDL değişikliği değildir.

**Kural**: bir iş kavramı (tip/durum/kategori/yöntem/aşama) gelecekte
değişebilir/genişleyebilirse → reference data. Enum olarak ASLA
yazılmaz.

Detay: `docs/06_reference_data_strategy.md`

---

## Localization

Per-entity sidecar `*_translation` tabloları (FK + cascade,
`(id, locale)` PK) + statik UI metinleri için `ref.i18n_message`
key/value katalogu. Fallback zinciri: istenen dil → corporation
varsayılanı → `en` → kod.

Detay: `docs/04_localization_strategy.md`, `docs/05_translation_entity_design.md`

---

## Bilinen Gelecek Genişletme (henüz uygulanmadı — bilinçli erteleme)

**OrganizationGroup / Holding tier**: Şu an `core.corporation` tek
başına root tenant entity'sidir. Mimari incelemede (Opus, R1) birden
fazla corporation'ı bir holding/grup çatısı altında toplama ihtiyacı
(cross-corp raporlama, ortak katalog) potansiyel bir gelecek ihtiyacı
olarak tespit edildi, ancak **şu an gerçek bir iş talebi olmadığı için
bilinçli olarak DDL'e eklenmedi**.

- Şu an: `corporation` bağımsız, üstünde herhangi bir parent yok.
- Gerekirse ileride: nullable `corporation.group_id` FK + yeni
  `core.org_group` tablosu — bu **geriye dönük uyumlu bir migration**
  olacak, mevcut hiçbir veriyi/davranışı bozmayacak.
- Şu an OrganizationGroup için herhangi bir tablo, servis veya iş
  kuralı uygulanmayacaktır.
- Ancak yeni geliştirmelerde Corporation'ın sistemin mutlak ve
  değişmez üst seviyesi olduğu varsayımı yapılmamalıdır.
- OrganizationGroup gerçek iş ihtiyacı olarak ortaya çıktığında
  migration ile eklenecektir.

---

## Authorization Strategy

RBAC is the primary authorization model.

Future enhancement: ABAC/Care-Team authorization may be introduced
for clinical data (student records, assessments, case notes,
reports) where access must depend on assignment relationships
rather than only roles.

Current implementation remains RBAC-based. Do not implement ABAC/
care-team logic now — no table, service, or business rule for it
exists in DDL.md yet. New code must not assume role-based checks are
the permanent, only mechanism for clinical-data access.

---

## SaaS Licensing

Tenant subscription, feature licensing and usage metering are not
implemented in the current schema.

The platform architecture must allow future introduction of:

- Tenant subscriptions
- Feature flags
- Plan-based capabilities

without redesigning existing domains.

Do not implement subscription/licensing tables or logic now. New
code must not hardcode the assumption that every tenant has
unlimited access to every module — `menu_item.feature_flag` already
exists as the seam this will eventually plug into.

---

## Diğer Mimari Kararlar (özet)

- **Primary key**: UUID v7 (zaman sıralı, global unique)
- **Audit**: DB-trigger tabanlı (`core.audit_trigger`), sadece
  app-log değil — bypass edilemez.
- **Financial integrity**: bakiyeler asla mutable kolon değil,
  append-only `credit_ledger`'dan türetilir.
- **Scheduling conflict prevention**: `EXCLUDE USING gist`
  (oda çakışması) + trigger (eğitmen double-booking).
- **KVKK/consent**: `student_consent` ledger, kamera izleme dahil
  her hassas erişim consent + audit log ile kapılı.
- **Modüler monolith**: her bounded context kendi PostgreSQL
  schema'sı; mikroservise geçiş outbox event'leri üzerinden,
  şimdilik gerekmiyor.

Detay: `docs/08_risks_recommendations.md`, Opus mimari analizi
(tam rapor — composer geçmişinde / ayrıca dosyalanacaksa
`docs/09_architecture_review.md`)

---

## Bu Dosyanın Güncellenme Kuralı

Bu dosya, yeni bir mimari karar onaylandığında (Komut2 sonrası,
veya yeni bir analiz turu sonrası) güncellenir. Cursor/Sonnet bu
dosyayı **kendi inisiyatifiyle değiştirmez** — sadece okur ve
referans alır. Güncelleme her zaman insan onayından geçer.
