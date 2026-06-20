#!/usr/bin/env python3
"""
AyNesil — build.py
Builds both backend and frontend.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))

import environment_check
import build_backend
import build_frontend


def main() -> None:
    rc = environment_check.main()
    if rc != 0:
        sys.exit(rc)
    build_backend.main()
    build_frontend.main()
    print("\n🎉  Full build complete.")


if __name__ == "__main__":
    main()
