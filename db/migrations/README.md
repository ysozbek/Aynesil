# AyNesil — Incremental Schema Migrations

Bu klasör **baseline'dan sonraki** schema değişikliklerini içerir.

## Strateji

| Aşama | Ne kullanılır | Ne içerir |
|---|---|---|
| **Baseline (ilk kurulum)** | `db/migrate.sh` | `db/00_*`, `db/layer1_core/*`, `db/layer2_sped/*`, `db/99_*`, `db/seed/*` |
| **Sonraki değişiklikler** | Bu klasördeki numbered `.sql` dosyaları | ALTER TABLE, yeni tablolar, yeni seed satırları |

## Dosya Adlandırma Kuralı

```
V{YYYYMMDD}_{HHMM}__{açıklama}.sql

Örnekler:
V20260620_0900__add_student_photo_field.sql
V20260715_1430__add_camp_category_ref_type.sql
V20260801_0800__add_ix_session_educator.sql
```

## Migration Dosyası Yazma Kuralları

1. **Her migration idempotent olmalı** (iki kez çalıştırılabilmeli)
   ```sql
   -- İdempotent ALTER COLUMN
   ALTER TABLE students.student
     ADD COLUMN IF NOT EXISTS photo_url text;
   
   -- İdempotent index
   CREATE INDEX IF NOT EXISTS ix_student_photo ON students.student(photo_url)
     WHERE photo_url IS NOT NULL;
   
   -- İdempotent ref_type INSERT
   INSERT INTO ref.ref_type(code, name, allows_tenant_values)
   VALUES ('photo_type', 'Photo Types', true)
   ON CONFLICT (code) DO NOTHING;
   ```

2. **Hiçbir zaman fiziksel DELETE veya DROP yazmayın** finansal/klinik tablolarda.
   Bunun yerine soft-delete veya `is_active = false` kullanın.

3. **RLS politikaları** yeni tablolarda `99_triggers_rls_policies.sql` mantığını takip edin:
   ```sql
   ALTER TABLE yeni_schema.yeni_tablo ENABLE ROW LEVEL SECURITY;
   CREATE POLICY tenant_isolation ON yeni_schema.yeni_tablo
     USING (corporation_id IS NULL OR corporation_id = core.current_corporation_id())
     WITH CHECK (corporation_id = core.current_corporation_id());
   ```

4. Migration sonunda **app role grant** ekleyin:
   ```sql
   GRANT SELECT, INSERT, UPDATE, DELETE ON yeni_schema.yeni_tablo TO aynesil_app;
   ```

## Migration Uygulama

```bash
# Tek bir migration uygula (owner rolüyle)
PGPASSWORD=changeme_owner psql \
  -h localhost -U aynesil_owner -d aynesil \
  -v ON_ERROR_STOP=1 \
  -f db/migrations/V20260620_0900__add_student_photo_field.sql

# Docker üzerinde
docker exec -i aynesil-db psql \
  -U aynesil_owner -d aynesil \
  -v ON_ERROR_STOP=1 < db/migrations/V20260620_0900__add_student_photo_field.sql
```

## Production'da Flyway / Liquibase

İleride CI/CD pipeline kurulunca bu klasör **Flyway** veya **Liquibase** ile yönetilecek:

```yaml
# Flyway config (flyway.conf)
flyway.url=jdbc:postgresql://db:5432/aynesil
flyway.user=aynesil_owner
flyway.password=${FLYWAY_PASSWORD}
flyway.locations=filesystem:db/migrations
flyway.baselineOnMigrate=true
flyway.baselineVersion=1
```

Baseline = migrate.sh'ın uyguladığı tüm DDL (zaten green-build doğrulanmış).
