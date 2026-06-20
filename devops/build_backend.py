#!/usr/bin/env python3
"""
AyNesil — build_backend.py
Builds and tests the .NET backend.
"""
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).parent.parent
SLN = ROOT / "Aynesil.sln"


def run(cmd: list[str], **kwargs) -> None:
    print(f"\n▶  {' '.join(cmd)}")
    result = subprocess.run(cmd, cwd=ROOT, **kwargs)
    if result.returncode != 0:
        sys.exit(result.returncode)


def main() -> None:
    print("\n🔨  Building .NET backend...")
    run(["dotnet", "restore", str(SLN)])
    run(["dotnet", "build", str(SLN), "-c", "Release", "--no-restore"])
    run(["dotnet", "test", str(SLN), "-c", "Release", "--no-build",
         "--logger", "console;verbosity=normal"])
    print("\n✅  Backend build complete.")


if __name__ == "__main__":
    main()
