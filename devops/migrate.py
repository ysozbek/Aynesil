#!/usr/bin/env python3
"""
AyNesil — migrate.py
Cross-platform migration runner (Windows + Linux + macOS).
Runs db/migrate.sh inside the running aynesil-db Docker container
OR directly against a local PostgreSQL instance.

Kullanım:
  python3 devops/migrate.py                  # Docker'daki DB'ye (varsayılan)
  python3 devops/migrate.py --local          # Local psql ile
  python3 devops/migrate.py --seed-only      # Sadece seed (DDL atla)
  python3 devops/migrate.py --skip-seed      # DDL uygula, seed atla
  python3 devops/migrate.py --incremental    # db/migrations/ klasöründeki yenileri uygula
"""
import subprocess
import sys
import os
import argparse
import glob
from pathlib import Path

ROOT = Path(__file__).parent.parent


def run(cmd: list[str], **kwargs) -> int:
    print(f"\n▶  {' '.join(cmd)}")
    result = subprocess.run(cmd, **kwargs)
    return result.returncode


def migrate_via_docker(args: argparse.Namespace) -> int:
    """Runs migrate.sh inside the aynesil-db container via docker exec."""
    print("\n🐳  Running migration inside Docker container (aynesil-db)...")

    # Check container is running
    check = subprocess.run(
        ["docker", "inspect", "--format={{.State.Running}}", "aynesil-db"],
        capture_output=True, text=True
    )
    if check.returncode != 0 or check.stdout.strip() != "true":
        print("❌  Container 'aynesil-db' is not running.")
        print("    Start with: docker compose up -d aynesil-db")
        return 1

    # Copy migrate.sh into container and run it
    cmd = ["docker", "exec", "-i", "aynesil-db", "bash", "-c", """
set -e
apk add --no-cache bash curl >/dev/null 2>&1 || true
export PGHOST=localhost
export PGUSER=${POSTGRES_USER:-aynesil_owner}
export PGPASSWORD=${POSTGRES_PASSWORD:-changeme_owner}
export PGDATABASE=${POSTGRES_DB:-aynesil}
"""]

    # Use docker exec to run migrate.sh from the mounted repo volume
    extra_flags = []
    if args.seed_only:
        extra_flags.append("--seed-only")
    if args.skip_seed:
        extra_flags.append("--skip-seed")

    cmd_str = f"bash /repo/db/migrate.sh {' '.join(extra_flags)}" if extra_flags else "bash /repo/db/migrate.sh"

    rc = run([
        "docker", "compose", "run", "--rm", "--no-deps",
        "-e", f"PGPASSWORD={os.environ.get('POSTGRES_PASSWORD', 'changeme_owner')}",
        "aynesil-migrator"
    ], cwd=ROOT)

    return rc


def migrate_local(args: argparse.Namespace) -> int:
    """Runs migrate.sh directly with local psql."""
    print("\n💻  Running migration locally (local psql)...")

    env = {
        **os.environ,
        "PGHOST": os.environ.get("PGHOST", "localhost"),
        "PGPORT": os.environ.get("PGPORT", "5432"),
        "PGDATABASE": os.environ.get("PGDATABASE", "aynesil"),
        "PGUSER": os.environ.get("PGUSER", "aynesil_owner"),
        "PGPASSWORD": os.environ.get("PGPASSWORD", ""),
        "AYNESIL_DB_APP_USER": os.environ.get("AYNESIL_DB_APP_USER", "aynesil_app"),
        "AYNESIL_DB_APP_PASSWORD": os.environ.get("AYNESIL_DB_APP_PASSWORD", "changeme_app"),
    }

    flags = []
    if args.seed_only:
        flags.append("--seed-only")
    if args.skip_seed:
        flags.append("--skip-seed")

    script = ROOT / "db" / "migrate.sh"
    rc = run(["bash", str(script)] + flags, env=env, cwd=ROOT)
    return rc


def apply_incremental(args: argparse.Namespace) -> int:
    """Applies db/migrations/V*.sql files in order, tracking applied ones."""
    migrations_dir = ROOT / "db" / "migrations"
    files = sorted(glob.glob(str(migrations_dir / "V*.sql")))

    if not files:
        print("✅  No incremental migrations found in db/migrations/")
        return 0

    print(f"\n📋  Found {len(files)} incremental migration(s):")
    for f in files:
        print(f"    {Path(f).name}")

    print("\n⚠️  Incremental migration requires manual tracking.")
    print("    Consider adopting Flyway for production:")
    print("    https://flywaydb.org/documentation/database/postgresql\n")

    env = {
        **os.environ,
        "PGPASSWORD": os.environ.get("PGPASSWORD", ""),
    }

    for f in files:
        name = Path(f).name
        print(f"\n▶  Applying {name}...")
        result = subprocess.run([
            "psql",
            "-h", os.environ.get("PGHOST", "localhost"),
            "-U", os.environ.get("PGUSER", "aynesil_owner"),
            "-d", os.environ.get("PGDATABASE", "aynesil"),
            "-v", "ON_ERROR_STOP=1",
            "-f", f
        ], env=env)

        if result.returncode != 0:
            print(f"❌  Failed on {name}")
            return result.returncode
        print(f"    ✓ {name}")

    print("\n✅  All incremental migrations applied.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="AyNesil database migration runner")
    parser.add_argument("--local", action="store_true",
                        help="Run against local psql (skip Docker)")
    parser.add_argument("--seed-only", action="store_true",
                        help="Skip DDL, apply seed data only")
    parser.add_argument("--skip-seed", action="store_true",
                        help="Apply DDL only, skip seed data")
    parser.add_argument("--incremental", action="store_true",
                        help="Apply db/migrations/V*.sql files in order")
    args = parser.parse_args()

    if args.incremental:
        return apply_incremental(args)
    elif args.local:
        return migrate_local(args)
    else:
        return migrate_via_docker(args)


if __name__ == "__main__":
    sys.exit(main())
