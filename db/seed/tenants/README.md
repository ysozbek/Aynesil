# Tenant Seed Data

Bu klasör **tenant-specific** (müşteri-özgü) başlangıç verilerini içerir.  
Flyway migration'larından **ayrı** tutulur — bir Flyway migration herkese zorunlu uygulanır,  
oysa bir tenant'ın şubeleri başka bir müşteri kurulumunda anlamsızdır.

---

## Klasör yapısı

```
db/seed/tenants/
  _template/          ← Yeni tenant için başlangıç şablonu
    01_corporation.sql
    02_campuses.sql
    03_rbac.sql
  akran/              ← Akran Hareket tenant verisi
    01_corporation.sql
    02_campuses.sql
    03_rbac.sql
  <yeni_tenant>/      ← Yeni müşteri için buraya kopyalayın
```

---

## Ne zaman uygulanır?

| Adım | Ne | Nasıl |
|---|---|---|
| 1 | Flyway migrations (`db/migrations/`) | Her ortamda otomatik — Flyway |
| 2 | Tenant seed (bu klasör) | Sadece ilgili ortamda — **manuel** veya CI script |

---

## Yeni tenant kurulumu

```bash
# 1. Şablonu kopyalayın
cp -r db/seed/tenants/_template db/seed/tenants/yeni_musteri

# 2. Dosyaları düzenleyin: <tenant_code>, şube listesi vb.
# 3. Flyway migration'ları uygulanmış bir veritabanına karşı çalıştırın:

psql "$DATABASE_URL" \
  -f db/seed/tenants/yeni_musteri/01_corporation.sql \
  -f db/seed/tenants/yeni_musteri/02_campuses.sql \
  -f db/seed/tenants/yeni_musteri/03_rbac.sql
```

> Tüm seed scriptleri **idempotent**tir (`ON CONFLICT DO NOTHING`).  
> Aynı scripti birden fazla çalıştırmak veri bozukluğuna yol açmaz.

---

## Kural

- `db/migrations/` → **Platform verisi** (schema, system ref data, permission kataloğu)  
- `db/seed/tenants/` → **Tenant verisi** (corporation, kampüsler, rol atamaları)

Yeni bir iş kuralı gerektiren **tablo veya kolon değişikliği** → yeni Flyway migration.  
Yeni bir **müşteri onboarding** → yeni `db/seed/tenants/<kod>/` klasörü.
