#!/usr/bin/env python3
"""
AyNesil — package_release.py
Creates a versioned release archive: backend binary + frontend dist + DB scripts.
Usage: python3 devops/package_release.py --version 1.0.0
"""
import subprocess
import sys
import argparse
import shutil
import tarfile
from pathlib import Path
from datetime import datetime

ROOT = Path(__file__).parent.parent
RELEASES = ROOT / "releases"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--version", default=datetime.now().strftime("%Y.%m.%d"))
    parser.add_argument("--runtime", default="linux-x64")
    args = parser.parse_args()

    release_dir = RELEASES / f"aynesil-{args.version}-{args.runtime}"
    release_dir.mkdir(parents=True, exist_ok=True)

    # API binary
    api_src = ROOT / "publish" / args.runtime
    if api_src.exists():
        shutil.copytree(api_src, release_dir / "api", dirs_exist_ok=True)

    # Frontend dist
    frontend_src = ROOT / "frontend" / "aynesil-web" / "dist"
    if frontend_src.exists():
        shutil.copytree(frontend_src, release_dir / "web", dirs_exist_ok=True)

    # DB scripts
    shutil.copytree(ROOT / "db", release_dir / "db", dirs_exist_ok=True)

    # Docker assets
    shutil.copy(ROOT / "docker-compose.yml", release_dir)
    shutil.copy(ROOT / ".env.example", release_dir)

    # Archive
    archive = RELEASES / f"aynesil-{args.version}-{args.runtime}.tar.gz"
    with tarfile.open(archive, "w:gz") as tar:
        tar.add(release_dir, arcname=release_dir.name)

    shutil.rmtree(release_dir)

    print(f"\n✅  Release packaged → {archive}")
    print(f"    Size: {archive.stat().st_size / 1_048_576:.1f} MB")


if __name__ == "__main__":
    main()
