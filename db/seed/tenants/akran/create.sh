#!/usr/bin/env bash
# Akran Hareket kurumunu açar (tekrar çalıştırılabilir).
#   db/seed/tenants/akran/create.sh --admin-username mudur@akranhareket.com --admin-name "Ad Soyad" [--check] [--enable-modules kpi,camera]
#
# - Kurum yoksa devops/new_tenant.py ile açar: kurum, 11 kampüs, admin + guardian rolleri ve ilk yönetici.
#   Yöneticinin geçici şifresi ÜRETİLİR, bir kez ekrana yazılır; ilk girişte değiştirmek zorunludur
#   (must_change_password = true).
# - Kurum zaten varsa yeniden açmaz; yalnızca extras.sql'i (kampüs ilçeleri, kuruma özel referans değerleri)
#   idempotent olarak uygular.
# - --check: new_tenant.py'yi deneme kipinde çalıştırır (geri alır), extras.sql uygulanmaz.
# Ön koşul: çalışan yığın (docker compose up -d) ve uygulanmış migration'lar. Herhangi bir dizinden çalıştırılabilir.
# Sahip rolü (POSTGRES_USER) .env'den okunur.
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/../../../.." && pwd)"
cd "$ROOT"
env_get() { grep -E "^$1=" .env 2>/dev/null | tail -1 | cut -d= -f2- || true; }
PGU=$(env_get POSTGRES_USER); PGU=${PGU:-aynesil_owner}
PGD=$(env_get POSTGRES_DB);   PGD=${PGD:-aynesil}
DB=${AYNESIL_DB_CONTAINER:-aynesil-db}
psql_q() { docker exec -i "$DB" psql -X -U "$PGU" -d "$PGD" -v ON_ERROR_STOP=1 "$@"; }

CHECK=""; for a in "$@"; do [ "$a" = "--check" ] && CHECK=1; done

if [ "$(psql_q -Atc "select count(*) from core.corporation where code = 'akran'")" = "0" ]; then
  python3 devops/new_tenant.py --code akran --name "Akran Hareket" --legal-name "Akran Hareket Özel Eğitim" \
    --campus "Etiler Kampüsü:İstanbul"       --campus "Kadıköy Kampüsü:İstanbul"    --campus "Ankara Kampüsü:Ankara" \
    --campus "Bahçelievler Kampüsü:İstanbul" --campus "Eyüpsultan Kampüsü:İstanbul" --campus "Mecidiyeköy Kampüsü:İstanbul" \
    --campus "Bayrampaşa Kampüsü:İstanbul"   --campus "Bahçeşehir Kampüsü:İstanbul" --campus "Yalova Kampüsü:Yalova" \
    --campus "Çekmeköy Kampüsü:İstanbul"     --campus "Ordu Fatsa Kampüsü:Ordu" \
    "$@"
else
  echo "ℹ️   akran kurumu zaten var; yeniden açılmadı (yönetici ve şifresi değişmedi)."
fi

if [ -z "$CHECK" ]; then
  psql_q -q -f - < "$HERE/extras.sql"
  echo "✅  extras.sql uygulandı (kampüs ilçeleri, kuruma özel referans değerleri)."
fi
