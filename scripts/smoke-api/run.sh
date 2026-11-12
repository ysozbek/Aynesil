#!/usr/bin/env bash
# AyNesil API duman testi — tek komutla: DB + migration + API → test kurumu → endpoint çıkarımı → test.
#   scripts/smoke-api/run.sh            # tam akış
#   scripts/smoke-api/run.sh --only camp   # yalnızca 'camp' ile başlayan GET'ler (ek argümanlar run.mjs'e gider)
# Gereksinim: docker, node ≥ 20, python3, frontend/aynesil-web/node_modules (typescript).
# API, üretim imajıyla (docker/api.Dockerfile) çalıştırılır; ICU artık imajın içinde.
set -euo pipefail
cd "$(dirname "$0")/../.."
FX=scripts/smoke-api/fixtures.json
jq() { python3 -c "import json,sys;d=json.load(open('$FX'));print(eval(sys.argv[1]))" "$1"; }
API_PORT=$(grep -E '^API_PORT=' .env 2>/dev/null | cut -d= -f2 || true); API_PORT=${API_PORT:-5000}
export SMOKE_API="${SMOKE_API:-http://localhost:${API_PORT}/api}"

echo "▶ DB + Redis"; docker compose up -d aynesil-db aynesil-redis >/dev/null
echo "▶ Flyway migration"; docker compose run --rm aynesil-flyway >/dev/null
echo "▶ API imajı (üretim Dockerfile'ı ile derle + başlat)"
docker compose up -d --build --force-recreate aynesil-api >/dev/null
for i in $(seq 1 40); do curl -sf "${SMOKE_API%/api}/health" >/dev/null && break; sleep 3; done
curl -sf "${SMOKE_API%/api}/health" >/dev/null || { echo "API ayağa kalkmadı"; docker logs aynesil-api --tail 20; exit 1; }

CODE=$(jq "d['tenant']['code']")
EXISTS=$(docker exec aynesil-db psql -U aynesil_owner -d aynesil -Atc "select count(*) from core.corporation where code='$CODE'")
if [ "$EXISTS" = "0" ]; then
  echo "▶ Test kurumu oluşturuluyor ($CODE)"
  python3 devops/new_tenant.py --code "$CODE" --name "$(jq "d['tenant']['name']")" --campus "$(jq "d['tenant']['campus']")" \
    --admin-username "$(jq "d['admin']['username']")" --admin-name "$(jq "d['admin']['fullName']")" \
    --admin-password "$(jq "d['admin']['password']")" >/dev/null
else echo "▶ Test kurumu zaten var ($CODE)"; fi

echo "▶ Frontend servislerinden uç noktalar çıkarılıyor"; node scripts/smoke-api/extract-endpoints.mjs
echo "▶ Duman testi"; node scripts/smoke-api/run.mjs "$@"
