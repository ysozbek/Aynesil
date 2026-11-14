#!/usr/bin/env python3
"""
AyNesil — set_modules.py
Bir kurumun (tenant) isteğe bağlı modüllerini açar / kapatır. Platform işidir:
`core.corporation.settings.modules` yalnızca bu script ve new_tenant.py tarafından yazılır;
kurum yöneticisinin ayar güncellemesi bu anahtarı değiştiremez.

Modüller (Aynesil.Shared/Constants/Modules.cs ile aynı kodlar):
  camps         Kamp
  consultancy   Okul danışmanlığı
  camera        Kamera
  surveys       Anketler
  care_team     Bakım ekibi
  kpi           Eğitmen KPI

Kapalı modülün menüsü görünmez (iam.menu_item.feature_flag) ve uçları 403 `Error.ModuleDisabled` döner.
Anahtar hiç yazılmamışsa modül AÇIK sayılır (geriye dönük uyum); new_tenant.py yeni kurumda hepsini
açıkça false yazar.

Kullanım:
  python3 devops/set_modules.py --code ornek --list
  python3 devops/set_modules.py --code ornek --enable camps --disable surveys
  python3 devops/set_modules.py --code ornek --enable camps,kamera_yok --check     # hata: bilinmeyen modül
  python3 devops/set_modules.py --code demo_a --enable all                         # hepsini aç
  python3 devops/set_modules.py --code ornek --disable all --dry-run               # SQL'i yazdır

  --list      Mevcut durumu yazar, değişiklik yapmaz
  --check     Çalıştırır ama sonunda ROLLBACK yapar (deneme)
  --dry-run   Üretilen SQL'i yazdırır, veritabanına bağlanmaz
  --local     Docker yerine yerel psql kullanır (PGHOST/PGUSER/PGDATABASE/PGPASSWORD)

Bağlantı new_tenant.py ile aynıdır (docker exec → aynesil-db, .env'deki POSTGRES_USER/POSTGRES_DB; owner rolü).
Not: Modül durumu her istekte okunur; API'yi yeniden başlatmak gerekmez.
"""
from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).parent.parent
CONTAINER = os.environ.get("AYNESIL_DB_CONTAINER", "aynesil-db")

MODULES: dict[str, str] = {
    "camps": "Kamp",
    "consultancy": "Okul danışmanlığı",
    "camera": "Kamera",
    "surveys": "Anketler",
    "care_team": "Bakım ekibi",
    "kpi": "Eğitmen KPI",
}


# ── Helpers ───────────────────────────────────────────────────────────────────

def read_env() -> dict[str, str]:
    """Reads KEY=VALUE pairs from the repo's .env (no shell expansion)."""
    env: dict[str, str] = {}
    path = ROOT / ".env"
    if not path.exists():
        return env
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        env[key.strip()] = value.strip().strip('"').strip("'")
    return env


def lit(value: str) -> str:
    """SQL string literal (standard_conforming_strings=on)."""
    if "\x00" in value:
        raise ValueError("NUL karakteri kullanılamaz")
    return "'" + value.replace("'", "''") + "'"


def parse_modules(values: list[str] | None) -> list[str]:
    """`--enable a,b --enable c` / `all` → doğrulanmış, tekrarsız modül listesi."""
    out: list[str] = []
    for raw in values or []:
        for item in raw.split(","):
            item = item.strip()
            if not item:
                continue
            names = list(MODULES) if item == "all" else [item]
            for name in names:
                if name not in MODULES:
                    sys.exit(f"❌  Bilinmeyen modül: '{name}'. Geçerli: {', '.join(MODULES)} (veya all)")
                if name not in out:
                    out.append(name)
    return out


def modules_json(enabled: bool, names: list[str]) -> str:
    return "{" + ",".join(f'"{n}": {"true" if enabled else "false"}' for n in names) + "}"


def psql_command(local: bool) -> list[str]:
    if local:
        return ["psql", "-X", "-v", "ON_ERROR_STOP=1", "-f", "-"]
    env = read_env()
    user = env.get("POSTGRES_USER", "aynesil_owner")
    db = env.get("POSTGRES_DB", "aynesil")
    check = subprocess.run(["docker", "inspect", "--format={{.State.Running}}", CONTAINER],
                           capture_output=True, text=True)
    if check.returncode != 0 or check.stdout.strip() != "true":
        sys.exit(f"❌  '{CONTAINER}' container'ı çalışmıyor. Önce: docker compose up -d aynesil-db")
    return ["docker", "exec", "-i", CONTAINER, "psql", "-X", "-U", user, "-d", db,
            "-v", "ON_ERROR_STOP=1", "-f", "-"]


def build_sql(code: str, enable: list[str], disable: list[str], *, commit: bool, read_only: bool) -> str:
    """Tek transaction; sonunda güncel durumu NOTICE olarak basar (anahtar yoksa 'açık')."""
    patch = {}
    patch.update({n: True for n in enable})
    patch.update({n: False for n in disable})
    patch_json = "{" + ",".join(f'"{n}": {"true" if v else "false"}' for n, v in patch.items()) + "}"
    keys = ", ".join(lit(n) for n in MODULES)
    update = "" if read_only else f"""
  update core.corporation
     set settings = jsonb_set(coalesce(settings, '{{}}'::jsonb), '{{modules}}',
                              coalesce(settings -> 'modules', '{{}}'::jsonb) || {lit(patch_json)}::jsonb, true),
         updated_at = now()
   where id = v_id;
"""
    end = "rollback;  -- --list/--check: nothing is written" if (read_only or not commit) else "commit;"
    return f"""
begin;
do $mod$
declare
  v_id uuid;
  v_mods jsonb;
  m text;
begin
  select id into v_id from core.corporation where code = {lit(code)} and deleted_at is null;
  if v_id is null then
    raise exception 'Kurum bulunamadı: %', {lit(code)};
  end if;
{update}
  select coalesce(settings -> 'modules', '{{}}'::jsonb) into v_mods from core.corporation where id = v_id;
  foreach m in array array[{keys}] loop
    raise notice 'MODULE % = %', rpad(m, 12),
      case when coalesce((v_mods ->> m)::boolean, true) then 'açık' else 'kapalı' end
      || case when v_mods ? m then '' else ' (anahtar yok → varsayılan açık)' end;
  end loop;
end
$mod$;
{end}
"""


def main() -> int:
    parser = argparse.ArgumentParser(description="AyNesil — kurum modüllerini aç/kapat",
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--code", required=True, help="Kurum kodu (ör: ornek)")
    parser.add_argument("--enable", action="append", metavar="MODÜL[,MODÜL]", help="Açılacak modül(ler) veya all")
    parser.add_argument("--disable", action="append", metavar="MODÜL[,MODÜL]", help="Kapatılacak modül(ler) veya all")
    parser.add_argument("--list", action="store_true", help="Mevcut durumu yaz, değiştirme")
    parser.add_argument("--local", action="store_true", help="Docker yerine yerel psql kullan")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="Çalıştır ama ROLLBACK yap (deneme)")
    mode.add_argument("--dry-run", action="store_true", help="SQL'i yazdır, bağlanma")
    args = parser.parse_args()

    enable = parse_modules(args.enable)
    disable = parse_modules(args.disable)
    both = sorted(set(enable) & set(disable))
    if both:
        sys.exit(f"❌  Aynı modül hem açılıp hem kapatılamaz: {', '.join(both)}")
    if not args.list and not enable and not disable:
        sys.exit("❌  --enable, --disable veya --list verin.")

    sql = build_sql(args.code, enable, disable, commit=not args.check, read_only=args.list)
    if args.dry_run:
        print(sql)
        return 0

    result = subprocess.run(psql_command(args.local), input=sql, text=True, capture_output=True)
    output = (result.stdout + result.stderr).strip()
    for line in output.splitlines():
        if any(tag in line for tag in ("NOTICE", "ERROR", "HATA", "FATAL", "DETAIL")):
            line = re.sub(r"^(psql:<stdin>:)?\d+:\s*", "", line.strip())
            line = line.replace("NOTICE:  MODULE ", "    ")
            print(line if line.startswith("    ") else "    " + line)

    if result.returncode != 0:
        print("\n❌  Modüller güncellenemedi. Hiçbir veri yazılmadı (işlem geri alındı).")
        return result.returncode

    if args.list:
        return 0
    if args.check:
        print("\n✅  Deneme başarılı (veri yazılmadı). Uygulamak için --check olmadan çalıştırın.")
    else:
        print(f"\n✅  '{args.code}' modülleri güncellendi. API yeniden başlatmak gerekmez.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
