-- =============================================================================
-- AyNesil Platform :: V3 — Hangfire Background Jobs Schema
-- Owner rolü ile çalışır (Flyway). Hangfire.PostgreSql kendi tablolarını
-- bu schema içinde oluşturur — schema yoksa CREATE SCHEMA dener ve
-- aynesil_app rolü database-level CREATE yetkisine sahip olmadığından
-- permission denied alır. Bu migration schema'yı önceden oluşturur.
--
-- Hangfire'ın logic'i:
--   IF NOT EXISTS schema → CREATE SCHEMA  (bu satır permission denied verir)
--   → Biz schema'yı önceden oluşturursak IF NOT EXISTS geçer, CREATE atlanır
--   → Hangfire kendi tablolarını oluşturur (CREATE ON SCHEMA yetkisi ile)
-- =============================================================================

-- Schema'yı owner rolü ile oluştur
CREATE SCHEMA IF NOT EXISTS hangfire;

-- App rolüne schema kullanım izni
GRANT USAGE ON SCHEMA hangfire TO aynesil_app;

-- App rolüne schema içinde nesne oluşturma izni
-- (Hangfire kendi tablolarını runtime'da oluşturur)
GRANT CREATE ON SCHEMA hangfire TO aynesil_app;

-- Hangfire tarafından oluşturulacak tablolara varsayılan izinler
ALTER DEFAULT PRIVILEGES IN SCHEMA hangfire
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO aynesil_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA hangfire
    GRANT USAGE, SELECT ON SEQUENCES TO aynesil_app;
