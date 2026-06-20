#!/usr/bin/env bash
# =====================================================================
# Akran Platform :: DDL smoke test (Docker-first, PostgreSQL 17 only)
# Spins up a clean PG17 container, applies all DDL + seed in dependency
# order, then verifies extensions / schemas / 136 tables / RLS /
# exclusion constraints. Exits non-zero with the exact failing file on error.
#
# Usage:   bash db/validation/smoke_test.sh
# Cleanup: docker compose -f db/validation/docker-compose.yml down -v
# =====================================================================
set -euo pipefail

cd "$(dirname "$0")"
COMPOSE=(docker compose -f docker-compose.yml)
PSQL=("${COMPOSE[@]}" exec -T db psql -v ON_ERROR_STOP=1 -U postgres -d akran)
# Same, but quiets idempotent DROP ... IF EXISTS notices during DDL application.
PSQL_QUIET=("${COMPOSE[@]}" exec -e "PGOPTIONS=-c client_min_messages=warning" -T db psql -v ON_ERROR_STOP=1 -U postgres -d akran)

# DDL + seed in strict dependency order (matches numeric file prefixes).
FILES=(
  db/00_extensions_conventions.sql
  db/layer1_core/01_localization.sql
  db/layer1_core/02_tenancy.sql
  db/layer1_core/03_reference_data.sql
  db/layer1_core/04_identity_access.sql
  db/layer1_core/05_platform_services.sql
  db/layer2_sped/01_students.sql
  db/layer2_sped/02_educators.sql
  db/layer2_sped/03_crm.sql
  db/layer2_sped/04_assessment.sql
  db/layer2_sped/05_education.sql
  db/layer2_sped/06_scheduling.sql
  db/layer2_sped/07_finance.sql
  db/layer2_sped/08_legal.sql
  db/layer2_sped/09_media.sql
  db/layer2_sped/10_ops.sql
  db/layer2_sped/11_camps.sql
  db/layer2_sped/12_consultancy.sql
  db/layer2_sped/13_parent_portal.sql
  db/99_triggers_rls_policies.sql
  db/seed/01_reference_data_seed.sql
  db/seed/02_akran_bootstrap.sql
)

echo "==> [1/4] Fresh PostgreSQL 17 container"
"${COMPOSE[@]}" down -v --remove-orphans >/dev/null 2>&1 || true
"${COMPOSE[@]}" up -d

echo "==> [2/4] Waiting for readiness"
for _ in $(seq 1 60); do
  if "${COMPOSE[@]}" exec -T db pg_isready -U postgres -d akran >/dev/null 2>&1; then break; fi
  sleep 1
done
echo -n "    server: "
"${COMPOSE[@]}" exec -T db psql -tA -U postgres -d akran -c "select version();"

echo "==> [3/4] Applying DDL + seed in dependency order"
for f in "${FILES[@]}"; do
  printf '    -- %s\n' "$f"
  if ! "${PSQL_QUIET[@]}" -f "/repo/$f"; then
    echo ""
    echo "❌ BUILD FAILED while applying: $f"
    echo "   (see the psql error above for the exact line and message)"
    exit 1
  fi
done

echo "==> [4/4] Verifying objects"
if ! "${PSQL[@]}" -f /repo/db/validation/verify.sql; then
  echo ""
  echo "❌ VERIFICATION FAILED (see assertion error above)"
  exit 1
fi

echo ""
echo "============================================================"
echo "✅ GREEN BUILD — all DDL applied and all checks passed"
echo "============================================================"
echo "Container 'akran_pg17' left running for inspection."
echo "Tear down with: docker compose -f db/validation/docker-compose.yml down -v"
