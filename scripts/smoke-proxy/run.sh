#!/usr/bin/env bash
# Gerçek istemci IP'si duman testi — çalışan compose yığınında, isteği ön yüz nginx'inden (aynesil-web) geçirerek.
#   scripts/smoke-proxy/run.sh
# Ön koşul: yığın ayakta (docker compose up -d) ve demo verisi kurulu (scripts/demo-seed/run.sh).
#
# Sınar:
#  1) Güvenilen vekil arkasındaki iki istemci (X-Forwarded-For A / B) ayrı hız sınırı sayaçlarına düşer.
#     Vekil, compose ağına bağlanan bir curl konteyneridir: Linux sunucuda ana makinedeki ters vekil de
#     Docker ağ geçidi (compose ağının .1 adresi) üzerinden bu ağdan gelir. Docker Desktop'ta (macOS/Windows)
#     ana makine bağlantıları VM adresinden (ör. 192.168.65.1) gelir ve güvenilmez; bu yüzden ana makine
#     yerine konteyner kullanılır. Ana makinenin görünen adresi 4. adımda bilgi olarak yazılır.
#  2) Compose ağı dışındaki bir kaynağın uydurduğu X-Forwarded-For yok sayılır: her istekte farklı sahte IP
#     gönderse de 11. denemede 429 alır.
#  3) Başarılı girişte iam.auth_session.ip_address ve API log'u gerçek istemci IP'sini gösterir.
# Adresler TEST-NET (203.0.113.0/24) aralığındandır; kullanıcı adları her çalıştırmada rastgeledir.
set -euo pipefail
cd "$(dirname "$0")/../.."
env_get() { grep -E "^$1=" .env 2>/dev/null | tail -1 | cut -d= -f2- || true; }
WEB="http://127.0.0.1:$(env_get FRONTEND_PORT | grep . || echo 5173)"
NET_IN=$(docker inspect aynesil-web -f '{{range $k, $v := .NetworkSettings.Networks}}{{$k}} {{end}}' | awk '{print $1}')
PGU=$(env_get POSTGRES_USER | grep . || echo aynesil_owner); PGD=$(env_get POSTGRES_DB | grep . || echo aynesil)
RUN=$(od -An -N4 -tx1 /dev/urandom | tr -d ' \n')
O3=$(( (16#${RUN:0:2} % 200) + 20 ))   # her çalıştırmada farklı TEST-NET adresleri (sayaçlar 1 dk yaşar)
IP_A="203.0.113.$O3"; IP_B="203.0.113.$((O3 + 1))"; IP_OK="203.0.113.$((O3 + 2))"
CURL_IMAGE=curlimages/curl:8.10.1
fail=0
ok()  { echo "  ✓ $*"; }
bad() { echo "  ✗ $*"; fail=1; }
# Compose ağındaki "ters vekil" üzerinden giriş denemeleri. Argümanlar: "XFF kullanıcı şifre" üçlüleri.
# Çıktı: her deneme için HTTP durum kodu (boşlukla ayrılmış).
proxy_logins() {
  docker run --rm --network "$NET_IN" --entrypoint sh "$CURL_IMAGE" -c '
    while [ $# -gt 0 ]; do
      curl -s -o /dev/null -w "%{http_code} " -X POST http://aynesil-web/api/auth/login -H "Content-Type: application/json" \
        -H "X-Forwarded-For: $1" --data "{\"username\":\"$2\",\"password\":\"$3\"}"
      shift 3
    done' sh "$@"
}

echo "▶ 1) Aynı güvenilen vekil arkasında iki istemci ($IP_A, $IP_B)"
args=(); for i in $(seq 1 11); do args+=("$IP_A" "smoke-$RUN-a$i" x); done; args+=("$IP_B" "smoke-$RUN-b" x)
read -r -a codes <<<"$(proxy_logins "${args[@]}")"
first10="${codes[*]:0:10}"
[ "$first10" = "401 401 401 401 401 401 401 401 401 401" ] && ok "A ilk 10 deneme 401" || bad "A ilk 10 deneme: $first10"
[ "${codes[10]}" = 429 ] && ok "A 11. denemede 429" || bad "A 11. deneme: ${codes[10]} (429 beklenirdi)"
[ "${codes[11]}" = 401 ] && ok "B etkilenmedi (401)" || bad "B: ${codes[11]} (401 beklenirdi; sayaç paylaşılıyor)"

echo "▶ 2) Compose ağı dışından sahte X-Forwarded-For"
NET="aynesil-smoke-untrusted-$RUN"
docker network create "$NET" >/dev/null
docker network connect "$NET" aynesil-web
trap 'docker network disconnect "$NET" aynesil-web >/dev/null 2>&1 || true; docker network rm "$NET" >/dev/null 2>&1 || true' EXIT
codes=$(docker run --rm --network "$NET" --entrypoint sh "$CURL_IMAGE" -c '
  for i in $(seq 1 11); do
    curl -s -o /dev/null -w "%{http_code} " -X POST http://aynesil-web/api/auth/login -H "Content-Type: application/json" \
      -H "X-Forwarded-For: 198.18.0.$i" --data "{\"username\":\"smoke-'"$RUN"'-f$i\",\"password\":\"x\"}"
  done')
last=$(echo "$codes" | awk '{print $NF}')
[ "$last" = 429 ] && ok "her istekte farklı sahte IP'ye rağmen 11. denemede 429 ($codes)" || bad "sahte başlık sınırı aştı: $codes"

echo "▶ 3) Başarılı girişte gerçek IP ($IP_OK)"
s=$(proxy_logins "$IP_OK" admin@demo-a.test 'DemoSifre!2026' | tr -d ' ')
[ "$s" = 200 ] && ok "giriş 200" || bad "giriş: $s (demo verisi kurulu mu?)"
ip=$(docker exec aynesil-db psql -U "$PGU" -d "$PGD" -Atc \
  "select s.ip_address from iam.auth_session s join iam.user_account u on u.id=s.user_id where u.username='admin@demo-a.test' order by s.issued_at desc limit 1")
[ "$ip" = "$IP_OK" ] && ok "iam.auth_session.ip_address = $ip" || bad "iam.auth_session.ip_address = $ip ($IP_OK beklenirdi)"
sleep 1
if docker logs --since 2m aynesil-api 2>&1 | grep "Login succeeded" | grep -q "$IP_OK"; then ok "API log'unda 'Login succeeded ... $IP_OK'"
else bad "API log'unda $IP_OK ile 'Login succeeded' satırı yok"; fi

echo "▶ 4) Bilgi: ana makineden gelen bağlantının nginx'te görünen adresi"
curl -s -o /dev/null -X POST "$WEB/api/auth/login" -H 'Content-Type: application/json' --data "{\"username\":\"smoke-$RUN-host\",\"password\":\"x\"}" || true
sleep 1
host_ip=$(docker logs --since 1m aynesil-api 2>&1 | grep 'POST /api/auth/login' | tail -1 | sed -n 's/.*"ClientIp": "\([^"]*\)".*/\1/p')
subnet=$(docker network inspect "$NET_IN" -f '{{(index .IPAM.Config 0).Subnet}}')
echo "  ana makine → $host_ip (compose ağı: $subnet). Linux sunucuda bu adres compose ağının ağ geçidi olmalı;"
echo "  öyleyse sunucudaki ters vekilin X-Forwarded-For'u kabul edilir. Docker Desktop'ta VM adresi görünür (beklenen)."

[ $fail = 0 ] && echo "✅ gerçek istemci IP'si zinciri doğru" || { echo "❌ hata var"; exit 1; }
