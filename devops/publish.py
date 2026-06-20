#!/usr/bin/env python3
"""
AyNesil — publish.py
Publishes the .NET API as a self-contained binary for the target platform.
Usage: python3 devops/publish.py [--runtime linux-x64]
"""
import subprocess
import sys
import argparse
from pathlib import Path

ROOT = Path(__file__).parent.parent
API_PROJ = ROOT / "src" / "Aynesil.Api" / "Aynesil.Api.csproj"
PUBLISH_DIR = ROOT / "publish"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--runtime", default="linux-x64",
                        help="Target runtime (linux-x64, win-x64, osx-arm64)")
    args = parser.parse_args()

    PUBLISH_DIR.mkdir(exist_ok=True)

    print(f"\n📦  Publishing API for {args.runtime}...")
    result = subprocess.run([
        "dotnet", "publish", str(API_PROJ),
        "-c", "Release",
        "-r", args.runtime,
        "--self-contained", "true",
        "-o", str(PUBLISH_DIR / args.runtime),
        "-p:PublishSingleFile=true",
        "-p:EnableCompressionInSingleFile=true",
    ], cwd=ROOT)

    if result.returncode != 0:
        sys.exit(result.returncode)

    print(f"\n✅  Published → {PUBLISH_DIR / args.runtime}")


if __name__ == "__main__":
    main()
