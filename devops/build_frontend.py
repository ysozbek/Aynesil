#!/usr/bin/env python3
"""
AyNesil — build_frontend.py
Builds the Vue 3 frontend for production.
"""
import subprocess
import sys
import os
from pathlib import Path

ROOT = Path(__file__).parent.parent
FRONTEND = ROOT / "frontend" / "aynesil-web"


def run(cmd: list[str], cwd: Path, env: dict | None = None) -> None:
    print(f"\n▶  {' '.join(cmd)}")
    full_env = {**os.environ, **(env or {})}
    result = subprocess.run(cmd, cwd=cwd, env=full_env)
    if result.returncode != 0:
        sys.exit(result.returncode)


def main() -> None:
    print("\n🔨  Building Vue 3 frontend...")
    run(["npm", "ci"], cwd=FRONTEND)
    run(["npm", "run", "type-check"], cwd=FRONTEND)
    run(["npm", "run", "build"], cwd=FRONTEND,
        env={"VITE_API_BASE_URL": "/api"})
    print(f"\n✅  Frontend build complete → {FRONTEND / 'dist'}")


if __name__ == "__main__":
    main()
