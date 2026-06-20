#!/usr/bin/env python3
"""
AyNesil — environment_check.py
Verifies that all required tools and services are available before build/run.
Cross-platform: Windows, Linux, macOS.
"""
import subprocess
import sys
import shutil
from dataclasses import dataclass
from typing import Optional

@dataclass
class Check:
    name: str
    command: str
    min_version: Optional[str] = None
    required: bool = True


CHECKS = [
    Check("Docker",          "docker --version",            required=True),
    Check("Docker Compose",  "docker compose version",      required=True),
    Check(".NET SDK",        "dotnet --version",            min_version="9", required=True),
    Check("Node.js",         "node --version",              min_version="22", required=True),
    Check("npm",             "npm --version",               required=True),
    Check("Python 3",        "python3 --version",           min_version="3.10", required=False),
    Check("Git",             "git --version",               required=False),
    Check("psql",            "psql --version",              required=False),
    Check("redis-cli",       "redis-cli --version",         required=False),
]


def run(cmd: str) -> tuple[bool, str]:
    try:
        result = subprocess.run(
            cmd.split(), capture_output=True, text=True, timeout=5
        )
        return result.returncode == 0, (result.stdout + result.stderr).strip()
    except (FileNotFoundError, subprocess.TimeoutExpired):
        return False, "not found"


def check_version(output: str, min_version: str) -> bool:
    import re
    numbers = re.findall(r"\d+", output)
    if not numbers:
        return False
    major = int(numbers[0])
    min_major = int(min_version.split(".")[0])
    return major >= min_major


def main() -> int:
    print("\n🔍  AyNesil — Environment Check\n" + "─" * 50)
    failures = 0

    for check in CHECKS:
        ok, output = run(check.command)
        version_ok = True
        if ok and check.min_version:
            version_ok = check_version(output, check.min_version)
            if not version_ok:
                ok = False
                output = f"version too old (need >= {check.min_version}): {output}"

        status = "✅" if ok else ("❌" if check.required else "⚠️ ")
        print(f"  {status}  {check.name:<20} {output[:60]}")
        if not ok and check.required:
            failures += 1

    print("─" * 50)
    if failures:
        print(f"\n❌  {failures} required tool(s) missing. Please install them before proceeding.\n")
        return 1
    else:
        print("\n✅  All required tools are available. Ready to build!\n")
        return 0


if __name__ == "__main__":
    sys.exit(main())
