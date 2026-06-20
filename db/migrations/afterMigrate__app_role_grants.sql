-- =============================================================================
-- AyNesil Platform :: Flyway afterMigrate Callback
-- Bu dosya her başarılı "flyway migrate" komutundan SONRA otomatik çalışır.
-- Amaç: aynesil_app rolüne tüm schema'larda gerekli izinleri ver.
--
-- Neden callback? Yeni bir V*.sql migration eklendiğinde (yeni tablo/schema)
-- bu callback otomatik çalışır ve grant'lar güncel kalır.
-- Flyway Community Edition destekler: afterMigrate.sql
-- =============================================================================

DO $$
DECLARE
  v_app_role text := current_setting('flyway.appRole', true);
  v_schemas  text[] := ARRAY[
    'core','iam','ref',
    'crm','students','assessment','educators','education',
    'scheduling','finance','legal','media','ops','camps','consultancy'
  ];
  v_schema   text;
BEGIN
  -- Flyway config'den app role oku; yoksa varsayılan kullan
  IF v_app_role IS NULL OR v_app_role = '' THEN
    v_app_role := 'aynesil_app';
  END IF;

  -- Rol var mı kontrol et
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = v_app_role) THEN
    RAISE WARNING 'App role "%" not found — skipping grants.', v_app_role;
    RETURN;
  END IF;

  FOREACH v_schema IN ARRAY v_schemas LOOP
    -- Schema var mı kontrol et (bazı layer2 schema'ları ileride eklenebilir)
    IF EXISTS (
      SELECT 1 FROM information_schema.schemata
      WHERE schema_name = v_schema
    ) THEN
      -- Schema kullanım izni
      EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I', v_schema, v_app_role);

      -- Mevcut tablolara DML izni
      EXECUTE format(
        'GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO %I',
        v_schema, v_app_role
      );

      -- Mevcut sequence'lara izin (SERIAL / IDENTITY kolonlar için)
      EXECUTE format(
        'GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %I TO %I',
        v_schema, v_app_role
      );

      -- Gelecekte eklenen tablolar için varsayılan izinler
      EXECUTE format(
        'ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I',
        v_schema, v_app_role
      );

      EXECUTE format(
        'ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT ON SEQUENCES TO %I',
        v_schema, v_app_role
      );

      RAISE NOTICE 'Grants applied: schema=% role=%', v_schema, v_app_role;
    ELSE
      RAISE NOTICE 'Schema "%" not found — skipping.', v_schema;
    END IF;
  END LOOP;

  -- public schema: flyway_schema_history tablosuna okuma izni (opsiyonel)
  IF EXISTS (SELECT FROM pg_roles WHERE rolname = v_app_role) THEN
    EXECUTE format('GRANT SELECT ON public.flyway_schema_history TO %I', v_app_role);
  END IF;

END $$;
