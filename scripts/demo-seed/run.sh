#!/usr/bin/env bash
# AyNesil demo veri seti — tek komut: DB + migration + API → iki demo kurum → veri.
#   scripts/demo-seed/run.sh                  # kur / eksikleri tamamla (idempotent)
#   scripts/demo-seed/run.sh --reset          # demo kurumlarını silip baştan kur
#   scripts/demo-seed/run.sh --only demo_a    # yalnızca bir kurum
#   scripts/demo-seed/run.sh --build          # API imajını yeniden derle (kod değiştiyse)
# Gereksinim: docker, node ≥ 20, python3. Üretim seed'ine (db/, Flyway) dokunmaz; yalnızca demo_a / demo_b kurumlarını yazar.
set -euo pipefail
cd "$(dirname "$0")/../.."
BUILD=""; ARGS=()
for a in "$@"; do if [ "$a" = "--build" ]; then BUILD="--build --force-recreate"; else ARGS+=("$a"); fi; done
API_PORT=$(grep -E '^API_PORT=' .env 2>/dev/null | cut -d= -f2 || true); API_PORT=${API_PORT:-5000}
export DEMO_API="${DEMO_API:-http://localhost:${API_PORT}/api}"
export POSTGRES_USER=$(grep -E '^POSTGRES_USER=' .env 2>/dev/null | cut -d= -f2 || echo aynesil_owner); export POSTGRES_USER=${POSTGRES_USER:-aynesil_owner}
export POSTGRES_DB=$(grep -E '^POSTGRES_DB=' .env 2>/dev/null | cut -d= -f2 || echo aynesil); export POSTGRES_DB=${POSTGRES_DB:-aynesil}

echo "▶ DB + Redis"; docker compose up -d aynesil-db aynesil-redis >/dev/null
echo "▶ Flyway migration"; docker compose run --rm aynesil-flyway >/dev/null
echo "▶ API"; docker compose up -d $BUILD aynesil-api >/dev/null
for i in $(seq 1 40); do curl -sf "${DEMO_API%/api}/health" >/dev/null && break; sleep 3; done
curl -sf "${DEMO_API%/api}/health" >/dev/null || { echo "API ayağa kalkmadı"; docker logs aynesil-api --tail 20; exit 1; }
echo "▶ Demo veri"; node scripts/demo-seed/seed.mjs ${ARGS[@]+"${ARGS[@]}"}
