#!/usr/bin/env bash
# =====================================================================
# AyNesil Platform :: migrate.sh
#
# ⚠️  FLYWAY MIGRATE ARTIK PRIMARY YOL:
#     docker compose up   → aynesil-flyway servisi V1+V2+afterMigrate çalıştırır
#     docker compose run aynesil-flyway migrate  → elle çalıştır
#
# Bu script artık YEDEK / LOCAL DEV içindir.
# Üretimde Flyway kullanın (db/migrations/ klasörü + docker/flyway.conf).
#
# Flyway olmadan local psql ile hızlı test:
#   ./db/migrate.sh --host localhost --db aynesil --user aynesil_owner
#
# ── Flyway ile çalıştır (docker varsa önerilen yol): ──────────────────
#   docker compose run --rm aynesil-flyway migrate
#   docker compose run --rm aynesil-flyway info     # migration durumu
#   docker compose run --rm aynesil-flyway validate # checksum doğrula
#   docker compose run --rm aynesil-flyway repair   # başarısız migration onar
# =====================================================================
set -euo pipefail

# ── Defaults ──────────────────────────────────────────────────────────
PG_HOST="${PGHOST:-localhost}"
PG_PORT="${PGPORT:-5432}"
PG_DB="${PGDATABASE:-aynesil}"
PG_USER="${PGUSER:-aynesil_owner}"
PG_PASS="${PGPASSWORD:-}"
APP_USER="${AYNESIL_DB_APP_USER:-aynesil_app}"
APP_PASS="${AYNESIL_DB_APP_PASSWORD:-changeme_app}"
SEED_ONLY=false
SKIP_SEED=false

# ── Parse args ────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --host)      PG_HOST="$2"; shift 2 ;;
    --port)      PG_PORT="$2"; shift 2 ;;
    --db)        PG_DB="$2"; shift 2 ;;
    --user)      PG_USER="$2"; shift 2 ;;
    --seed-only) SEED_ONLY=true; shift ;;
    --skip-seed) SKIP_SEED=true; shift ;;
    *) echo "Unknown arg: $1"; exit 1 ;;
  esac
done

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# ── psql helper ───────────────────────────────────────────────────────
run_sql() {
  local file="$1"
  local label="${2:-$file}"
  printf "  %-60s" "$label"
  PGPASSWORD="$PG_PASS" psql \
    -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$PG_DB" \
    -v ON_ERROR_STOP=1 \
    -q -f "$REPO_ROOT/$file" 2>&1 | grep -v "^$" | grep -v "^NOTICE" | head -5 || true
  echo " ✓"
}

run_sql_inline() {
  local sql="$1"
  local label="${2:-inline}"
  printf "  %-60s" "$label"
  PGPASSWORD="$PG_PASS" psql \
    -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$PG_DB" \
    -v ON_ERROR_STOP=1 -q -c "$sql" 2>&1 | grep -v "^$" | head -3 || true
  echo " ✓"
}

# ── Wait for PG ready ─────────────────────────────────────────────────
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  AyNesil — Database Migration"
echo "  Host: $PG_HOST:$PG_PORT  DB: $PG_DB  User: $PG_USER"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "⏳  Waiting for PostgreSQL..."
for i in $(seq 1 30); do
  if PGPASSWORD="$PG_PASS" pg_isready -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$PG_DB" -q 2>/dev/null; then
    echo "    Connected ✓"
    break
  fi
  if [[ $i -eq 30 ]]; then
    echo "❌  PostgreSQL not ready after 30s. Aborting."
    exit 1
  fi
  sleep 1
done

# ── Create app role (idempotent) ─────────────────────────────────────
echo ""
echo "👤  Ensuring application role exists..."
run_sql_inline "
DO \$\$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${APP_USER}') THEN
    CREATE ROLE \"${APP_USER}\" WITH LOGIN PASSWORD '${APP_PASS}'
      NOCREATEDB NOCREATEROLE NOSUPERUSER;
  ELSE
    ALTER ROLE \"${APP_USER}\" WITH PASSWORD '${APP_PASS}';
  END IF;
END
\$\$;" "create/update app role '${APP_USER}'"

if [[ "$SEED_ONLY" == "true" ]]; then
  echo ""
  echo "⏭️   --seed-only: skipping DDL, applying seed only"
else
  # ── DDL in strict dependency order ─────────────────────────────────
  echo ""
  echo "🏗️   Applying DDL (owner role — bypasses RLS)..."
  echo ""

  DDL_FILES=(
    "db/00_extensions_conventions.sql"
    "db/layer1_core/01_localization.sql"
    "db/layer1_core/02_tenancy.sql"
    "db/layer1_core/03_reference_data.sql"
    "db/layer1_core/04_identity_access.sql"
    "db/layer1_core/05_platform_services.sql"
    "db/layer2_sped/01_students.sql"
    "db/layer2_sped/02_educators.sql"
    "db/layer2_sped/03_crm.sql"
    "db/layer2_sped/04_assessment.sql"
    "db/layer2_sped/05_education.sql"
    "db/layer2_sped/06_scheduling.sql"
    "db/layer2_sped/07_finance.sql"
    "db/layer2_sped/08_legal.sql"
    "db/layer2_sped/09_media.sql"
    "db/layer2_sped/10_ops.sql"
    "db/layer2_sped/11_camps.sql"
    "db/layer2_sped/12_consultancy.sql"
    "db/layer2_sped/13_parent_portal.sql"
    "db/99_triggers_rls_policies.sql"
  )

  for f in "${DDL_FILES[@]}"; do
    if [[ -f "$REPO_ROOT/$f" ]]; then
      run_sql "$f"
    else
      echo "  ⚠️  SKIP (not found): $f"
    fi
  done
fi

# ── Grant app role permissions (post-DDL) ───────────────────────────
echo ""
echo "🔑  Granting permissions to app role..."
SCHEMAS=(core iam ref crm students assessment educators education scheduling finance legal media ops camps consultancy)
for schema in "${SCHEMAS[@]}"; do
  run_sql_inline "
    DO \$\$ BEGIN
      IF EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = '${schema}') THEN
        EXECUTE 'GRANT USAGE ON SCHEMA ${schema} TO \"${APP_USER}\"';
        EXECUTE 'GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA ${schema} TO \"${APP_USER}\"';
        EXECUTE 'GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA ${schema} TO \"${APP_USER}\"';
        EXECUTE 'ALTER DEFAULT PRIVILEGES IN SCHEMA ${schema} GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO \"${APP_USER}\"';
      END IF;
    END \$\$;" "grant ${schema} → ${APP_USER}"
done

if [[ "$SKIP_SEED" == "false" ]]; then
  # ── Seed ──────────────────────────────────────────────────────────
  echo ""
  echo "🌱  Applying seed data..."
  echo ""

  SEED_FILES=(
    "db/seed/01_reference_data_seed.sql"
    "db/seed/02_akran_bootstrap.sql"
  )

  for f in "${SEED_FILES[@]}"; do
    if [[ -f "$REPO_ROOT/$f" ]]; then
      run_sql "$f"
    else
      echo "  ⚠️  SKIP (not found): $f"
    fi
  done
fi

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  ✅  Migration complete"
echo "═══════════════════════════════════════════════════════════════"
echo ""

# ── Quick sanity check ────────────────────────────────────────────────
TABLE_COUNT=$(PGPASSWORD="$PG_PASS" psql -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$PG_DB" -tA -c "
  SELECT COUNT(*) FROM information_schema.tables
  WHERE table_schema IN ('core','iam','ref','crm','students','assessment',
                          'educators','education','scheduling','finance',
                          'legal','media','ops','camps','consultancy')
    AND table_type = 'BASE TABLE';")
echo "  📊  Tables created: $TABLE_COUNT (expected: ~136)"
echo ""
