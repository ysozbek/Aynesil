#!/usr/bin/env python3
"""
AyNesil — run_local.py
Starts the full local development stack using Docker Compose.
Runs environment_check first, then optionally applies DB schema + seeds.
"""
import subprocess
import sys
import os
from pathlib import Path

ROOT = Path(__file__).parent.parent


def run(cmd: list[str], cwd: Path = ROOT, check: bool = True) -> int:
    print(f"\n▶  {' '.join(cmd)}")
    result = subprocess.run(cmd, cwd=cwd, check=False)
    if check and result.returncode != 0:
        print(f"\n❌  Command failed: {' '.join(cmd)}")
        sys.exit(result.returncode)
    return result.returncode


def env_check() -> None:
    sys.path.insert(0, str(Path(__file__).parent))
    import environment_check
    rc = environment_check.main()
    if rc != 0:
        sys.exit(rc)


def copy_env_if_missing() -> None:
    env_file = ROOT / ".env"
    example = ROOT / ".env.example"
    if not env_file.exists() and example.exists():
        import shutil
        shutil.copy(example, env_file)
        print("⚠️   Created .env from .env.example — please review and update secrets!")


def main() -> None:
    env_check()
    copy_env_if_missing()

    print("\n🐳  Starting AyNesil via Docker Compose...")
    run(["docker", "compose", "up", "--build", "--remove-orphans"])


if __name__ == "__main__":
    main()
