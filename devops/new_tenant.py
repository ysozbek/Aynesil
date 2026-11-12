#!/usr/bin/env python3
"""
AyNesil — new_tenant.py
Yeni bir müşteri (kurum / tenant) açar. Tek komut, tek transaction.

Oluşturulanlar:
  1. core.corporation    — kurum kaydı
  2. core.campus         — bir veya daha fazla kampüs (şube)
  3. iam.role 'admin'    — tüm izinlere sahip kurum yöneticisi rolü
  4. iam.user_account    — ilk yönetici kullanıcı (+ iam.user_role ataması)

Referans veriler (seans türleri, ödeme yöntemleri vb.), izin kataloğu ve menüler
platform genelidir (corporation_id NULL), Flyway ile zaten yüklüdür; kurum
bunları ilk girişten itibaren görür.

Ön koşul: Flyway migration'ları uygulanmış bir veritabanı (docker compose up).

Kullanım:
  python3 devops/new_tenant.py \\
      --code ornek --name "Örnek Özel Eğitim" \\
      --campus "Merkez Kampüs:İstanbul" --campus "Kadıköy Kampüs:İstanbul" \\
      --admin-username mudur@ornek.com --admin-name "Ayşe Yılmaz"

  --check     Her şeyi çalıştırır ama sonunda ROLLBACK yapar (deneme; veri yazılmaz)
  --dry-run   Üretilen SQL'i ekrana basar, veritabanına bağlanmaz
  --local     Docker yerine yerel psql kullanır (PGHOST/PGUSER/PGDATABASE/PGPASSWORD)

Şifre verilmezse güçlü bir geçici şifre üretilir ve yalnızca bir kez ekrana yazılır.

Not: Giriş ekranı yalnızca kullanıcı adıyla çalışır; bu yüzden kullanıcı adı tüm
sistemde benzersizdir (V36, uq_user_account_username). Script bunu önceden kontrol
edip anlaşılır bir hata verir. Yönetici için e-posta adresi önerilir.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import secrets
import string
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).parent.parent
CONTAINER = "aynesil-db"


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


def lit(value: str | None) -> str:
    """SQL string literal (standard_conforming_strings=on)."""
    if value is None:
        return "NULL"
    if "\x00" in value:
        raise ValueError("NUL karakteri kullanılamaz")
    return "'" + value.replace("'", "''") + "'"


def generate_password(length: int = 14) -> str:
    alphabet = string.ascii_letters + string.digits
    while True:
        pw = "".join(secrets.choice(alphabet) for _ in range(length - 2)) + secrets.choice("!@#$%*") + secrets.choice(string.digits)
        if any(c.islower() for c in pw) and any(c.isupper() for c in pw):
            return pw


def campus_code(name: str, used: set[str]) -> str:
    """'Kadıköy Kampüs' -> 'KADIKOY' (unique within this run)."""
    tr = str.maketrans("çğıöşüÇĞİÖŞÜ", "cgiosuCGIOSU")
    base = re.sub(r"[^A-Za-z0-9]", "", name.translate(tr).split(" ")[0]).upper()[:10] or "KAMPUS"
    code, n = base, 2
    while code in used:
        code, n = f"{base[:8]}{n}", n + 1
    used.add(code)
    return code


def parse_campuses(values: list[str] | None) -> list[dict[str, str | None]]:
    items = values or ["Merkez Kampüs"]
    used: set[str] = set()
    result = []
    for raw in items:
        name, _, city = raw.partition(":")
        name = name.strip()
        if not name:
            sys.exit(f"❌  Kampüs adı boş olamaz: '{raw}'")
        result.append({"code": campus_code(name, used), "name": name, "city": city.strip() or None})
    return result


# ── SQL ───────────────────────────────────────────────────────────────────────

def build_sql(args: argparse.Namespace, campuses: list[dict], password: str, commit: bool) -> str:
    settings = {
        "nt.code": args.code,
        "nt.legal_name": args.legal_name or args.name,
        "nt.display_name": args.name,
        "nt.locale": args.locale,
        "nt.currency": args.currency,
        "nt.timezone": args.timezone,
        "nt.campuses": json.dumps(campuses, ensure_ascii=False),
        "nt.username": args.admin_username,
        "nt.email": args.admin_email or (args.admin_username if "@" in args.admin_username else ""),
        "nt.full_name": args.admin_name,
        "nt.password": password,
    }
    set_lines = ",\n  ".join(f"set_config({lit(k)}, {lit(v)}, true)" for k, v in settings.items())

    return f"""\\set ON_ERROR_STOP on
\\set QUIET on
begin;

-- Parameters as transaction-local settings (psql variables are not visible inside DO blocks)
select
  {set_lines};

do $tenant$
declare
  v_code      text  := current_setting('nt.code');
  v_username  text  := current_setting('nt.username');
  v_email     text  := nullif(current_setting('nt.email'), '');
  v_campuses  jsonb := current_setting('nt.campuses')::jsonb;
  v_corp      uuid;
  v_campus    uuid;
  v_first     uuid;
  v_role      uuid;
  v_user      uuid;
  v_perms     int;
  c           record;
begin
  -- ── Validation ──────────────────────────────────────────────────────────
  if v_code !~ '^[a-z][a-z0-9_]{{1,30}}$' then
    raise exception 'Kurum kodu geçersiz: "%" (küçük harf, rakam, alt çizgi; harfle başlamalı)', v_code;
  end if;
  if exists (select 1 from core.corporation where code = v_code) then
    raise exception 'Bu kurum kodu zaten kullanılıyor: %', v_code;
  end if;
  -- Usernames are globally unique (V36): login resolves by username alone.
  if exists (select 1 from iam.user_account where username = v_username::citext) then
    raise exception 'Bu kullanıcı adı zaten kullanılıyor: % (e-posta adresi kullanın)', v_username;
  end if;
  select count(*) into v_perms from iam.permission;
  if v_perms = 0 then
    raise exception 'İzin kataloğu boş. Önce Flyway migration''larını uygulayın (docker compose up).';
  end if;

  -- ── Corporation ─────────────────────────────────────────────────────────
  insert into core.corporation (code, legal_name, display_name, default_locale, default_currency, timezone)
  values (v_code, current_setting('nt.legal_name'), current_setting('nt.display_name'),
          current_setting('nt.locale'), current_setting('nt.currency'), current_setting('nt.timezone'))
  returning id into v_corp;

  -- ── Campuses ────────────────────────────────────────────────────────────
  for c in select * from jsonb_to_recordset(v_campuses) as x(code text, name text, city text) loop
    insert into core.campus (corporation_id, code, name, city, is_active)
    values (v_corp, c.code, c.name, c.city, true)
    returning id into v_campus;
    v_first := coalesce(v_first, v_campus);
  end loop;

  -- ── Admin role: every permission in the platform catalog ────────────────
  insert into iam.role (corporation_id, code, name, is_system)
  values (v_corp, 'admin', 'Administrator', true)
  returning id into v_role;

  insert into iam.role_permission (role_id, permission_id)
  select v_role, p.id from iam.permission p;

  -- ── First admin user ────────────────────────────────────────────────────
  insert into iam.user_account (corporation_id, username, email, full_name, password_hash,
                                status, preferred_locale, primary_campus_id)
  values (v_corp, v_username, v_email, current_setting('nt.full_name'),
          crypt(current_setting('nt.password'), gen_salt('bf', 12)),
          'active', current_setting('nt.locale'), v_first)
  returning id into v_user;

  insert into iam.user_role (corporation_id, user_id, role_id, campus_id)
  values (v_corp, v_user, v_role, null);

  raise notice 'Kurum: % (%), kampüs: %, yönetici: %, izin: %',
    current_setting('nt.display_name'), v_code, jsonb_array_length(v_campuses), v_username, v_perms;
end
$tenant$;

{"commit;" if commit else "rollback;  -- --check: nothing is written"}
"""


# ── Runner ────────────────────────────────────────────────────────────────────

def psql_command(args: argparse.Namespace) -> list[str]:
    if args.local:
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


def main() -> int:
    parser = argparse.ArgumentParser(description="AyNesil — yeni kurum (tenant) aç",
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--code", required=True, help="Kurum kodu: küçük harf, benzersiz (ör: ornek)")
    parser.add_argument("--name", required=True, help="Uygulamada görünen kurum adı")
    parser.add_argument("--legal-name", help="Resmi unvan (varsayılan: --name)")
    parser.add_argument("--campus", action="append", metavar="AD[:ŞEHİR]",
                        help="Kampüs; birden fazla verilebilir (varsayılan: 'Merkez Kampüs')")
    parser.add_argument("--admin-username", required=True,
                        help="Yönetici kullanıcı adı; tüm sistemde benzersiz olmalı (e-posta önerilir)")
    parser.add_argument("--admin-name", required=True, help="Yöneticinin adı soyadı")
    parser.add_argument("--admin-email", help="Yönetici e-postası (kullanıcı adı e-postaysa otomatik)")
    parser.add_argument("--admin-password", help="Verilmezse geçici şifre üretilir")
    parser.add_argument("--locale", default="tr")
    parser.add_argument("--currency", default="TRY")
    parser.add_argument("--timezone", default="Europe/Istanbul")
    parser.add_argument("--local", action="store_true", help="Docker yerine yerel psql kullan")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="Çalıştır ama ROLLBACK yap (deneme)")
    mode.add_argument("--dry-run", action="store_true", help="SQL'i yazdır, bağlanma")
    args = parser.parse_args()

    if args.admin_password is not None and len(args.admin_password) < 8:
        sys.exit("❌  Şifre en az 8 karakter olmalı.")
    password = args.admin_password or generate_password()
    campuses = parse_campuses(args.campus)
    sql = build_sql(args, campuses, password, commit=not args.check)

    if args.dry_run:
        print(sql.replace(lit(password), "'********'"))
        return 0

    print(f"\n🏢  Kurum açılıyor: {args.name} ({args.code})"
          + ("  [--check: deneme, veri yazılmayacak]" if args.check else ""))
    for c in campuses:
        print(f"    • Kampüs {c['code']}: {c['name']}" + (f" ({c['city']})" if c["city"] else ""))

    result = subprocess.run(psql_command(args), input=sql, text=True, capture_output=True)
    output = (result.stdout + result.stderr).strip()
    # Show NOTICE / ERROR lines only (hide psql's command tags)
    for line in output.splitlines():
        if any(tag in line for tag in ("NOTICE", "ERROR", "HATA", "FATAL", "DETAIL")):
            print("    " + re.sub(r"^(psql:<stdin>:)?\d+:\s*", "", line.strip()))

    if result.returncode != 0:
        print("\n❌  Kurum açılamadı. Hiçbir veri yazılmadı (işlem geri alındı).")
        return result.returncode

    if args.check:
        print("\n✅  Deneme başarılı. Gerçekten açmak için aynı komutu --check olmadan çalıştırın.")
        return 0

    print("\n✅  Kurum açıldı. Müşteriye iletilecek giriş bilgisi:")
    print(f"    Kullanıcı adı : {args.admin_username}")
    if args.admin_password:
        print("    Şifre         : (komutta verdiğiniz şifre)")
    else:
        print(f"    Geçici şifre  : {password}")
        print("    (Bu şifre tekrar gösterilmez. İlk girişten sonra değiştirilmesini isteyin.)")
    print("\nSonraki adım: docs/musteri/KURULUM_YOL_HARITASI.md → 1. hafta")
    return 0


if __name__ == "__main__":
    sys.exit(main())
