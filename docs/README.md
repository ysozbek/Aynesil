# AyNesil Dokümanları

Dokümanlar okuyucuya göre gruplanmıştır.

## Müşteriye dönük — `musteri/`

| Doküman | Kimin için | Ne anlatır |
|---|---|---|
| [KULLANIM_KILAVUZU.md](musteri/KULLANIM_KILAVUZU.md) | Kurum personeli, veli | Ekranların rol bazında kullanımı. Sonunda kurulum/destek ekibi için **Teknik Ek** var. |
| [DEMO_SENARYOSU.md](musteri/DEMO_SENARYOSU.md) | Demoyu yapan kişi | Demo verisi hazırlığı ve üç hikâyelik sunum akışı, söylenecek cümleler |
| [KURULUM_KILAVUZU.md](musteri/KURULUM_KILAVUZU.md) | Sistem yöneticisi (A), kurulumu yapan kişi (B) | A: sunucu kurulumu, ortam değişkenleri, HTTPS/ters vekil, yedekleme, güncelleme, canlıya çıkış listesi. B: yeni kurumu sıfırdan canlıya alma, roller, hesap bağlama, müşteriden istenecek bilgiler |
| [KURULUM_YOL_HARITASI.md](musteri/KURULUM_YOL_HARITASI.md) | — | Taşındı; KURULUM_KILAVUZU.md'ye yönlendirir |

## Mimari — bu klasörün kökü (yetkili kaynak)

Bu dosyalar migration'lardan ve SQL dosyalarından isimleriyle referans verildiği için yerinde durur; taşınmamalıdır. Özet: kök dizindeki `ARCHITECTURE.md`.

| Doküman | Konu |
|---|---|
| [01_entity_relationships.md](01_entity_relationships.md) | Varlık ilişkileri |
| [02_domain_boundaries.md](02_domain_boundaries.md) | Alan sınırları |
| [03_module_boundaries.md](03_module_boundaries.md) | Modül sınırları |
| [04_localization_strategy.md](04_localization_strategy.md) | Yerelleştirme stratejisi |
| [05_translation_entity_design.md](05_translation_entity_design.md) | Çeviri tabloları tasarımı |
| [06_reference_data_strategy.md](06_reference_data_strategy.md) | Referans veri stratejisi |
| [07_implementation_order.md](07_implementation_order.md) | Uygulama sırası |
| [08_risks_recommendations.md](08_risks_recommendations.md) | Riskler ve öneriler |
| [ABAC_CARE_TEAM_DESIGN.md](ABAC_CARE_TEAM_DESIGN.md) | Bakım ekibi (care-team) erişim tasarımı |

## Çalışma dokümanları — `calisma/`

Ekip içi, güncel iş takibi ve karar kayıtları.

| Doküman | Ne anlatır |
|---|---|
| [EKRAN_IYILESTIRME_LISTESI.md](calisma/EKRAN_IYILESTIRME_LISTESI.md) | Ekran sorunları listesi ve durumları; bekleyen terim kararları (Bölüm 3) |
| [MIGRATION_ONERISI_V35_tekrar_eden_plan.md](calisma/MIGRATION_ONERISI_V35_tekrar_eden_plan.md) | V35 migration'ının gerekçesi (uygulandı; karar kaydı) |
| [FLYWAY_NOTLARI.md](calisma/FLYWAY_NOTLARI.md) | Flyway komutları için kısa notlar |
| [../ekransiz-endpointler.md](ekransiz-endpointler.md) | Ekranı olmayan endpoint'ler: A (canlıdan önce) / B (modüle bağlı) / C (ölü), gerekçeli; hangileri kapatıldı |

## Raporlar — `raporlar/`

Geçmiş denetim ve doğrulama raporları. Tarihlidir; güncel durumu yansıtmayabilir.

| Doküman | Ne anlatır |
|---|---|
| [cross_layer_alignment_report.md](raporlar/cross_layer_alignment_report.md) | Katmanlar arası iş akışı uyum denetimi (2026-10-20) |
| `frontend_5A` … `frontend_5F_*_validation_report.md` | Frontend modül doğrulama raporları |

## Başka yerlerdeki dokümanlar

- `ARCHITECTURE.md` (kök): mimari özet, yetkili kaynak
- `CLAUDE.md` (kök): kod asistanı için proje rehberi
- `db/README.md`, `db/migrations/README.md`, `db/seed/tenants/README.md`: veritabanı, migration ve kurum seed notları (kodun yanında durur)
