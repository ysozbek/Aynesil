#!/usr/bin/env python3
"""Permissions.cs -> frontend/aynesil-web/src/constants/permissions.ts

Backend'deki `Aynesil.Shared.Constants.Permissions` sınıfı tek kaynaktır; frontend sabitleri
buradan üretilir. Yeni izin eklendiğinde yeniden çalıştırın:

    python3 scripts/gen-permissions.py            # dosyayı yeniden yazar
    python3 scripts/gen-permissions.py --check    # güncel değilse 1 ile çıkar (CI/pre-commit)

Üretilen dosyayı elle düzenlemeyin.
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "src/Aynesil.Shared/Constants/Permissions.cs"
OUT = ROOT / "frontend/aynesil-web/src/constants/permissions.ts"

CLASS_RE = re.compile(r"^\s*public static class (\w+)\s*$")
CONST_RE = re.compile(r'^\s*public const string (\w+)\s*=\s*"([^"]+)"\s*;')
COMMENT_RE = re.compile(r"^\s*//\s*──\s*(.+?)\s*─+\s*$")


def parse():
    groups, order, current, comment, depth_root = {}, [], None, None, False
    for line in SRC.read_text(encoding="utf-8").splitlines():
        c = COMMENT_RE.match(line)
        if c:
            comment = c.group(1)
            continue
        m = CLASS_RE.match(line)
        if m:
            name = m.group(1)
            if name == "Permissions":
                continue
            if name in groups:
                sys.exit(f"Yinelenen izin grubu: {name}")
            groups[name] = {"comment": comment, "items": {}}
            order.append(name)
            current, comment = name, None
            continue
        k = CONST_RE.match(line)
        if k and current:
            if k.group(1) in groups[current]["items"]:
                sys.exit(f"Yinelenen izin: {current}.{k.group(1)}")
            groups[current]["items"][k.group(1)] = k.group(2)
    return groups, order


def render(groups, order):
    out = [
        "/**",
        " * İzin kodları — ÜRETİLMİŞ DOSYA, elle düzenlemeyin.",
        " * Kaynak: src/Aynesil.Shared/Constants/Permissions.cs",
        " * Yeniden üretmek için: python3 scripts/gen-permissions.py",
        " */",
        "export const Permissions = {",
    ]
    for g in order:
        info = groups[g]
        if info["comment"]:
            out.append(f"  // {info['comment']}")
        out.append(f"  {g}: {{")
        for k, v in info["items"].items():
            out.append(f"    {k}: '{v}',")
        out.append("  },")
    out += [
        "} as const",
        "",
        "type Values<T> = T[keyof T]",
        "export type PermissionCode = Values<{ [G in keyof typeof Permissions]: Values<(typeof Permissions)[G]> }>",
        "",
    ]
    return "\n".join(out)


def main():
    groups, order = parse()
    text = render(groups, order)
    if "--check" in sys.argv:
        if not OUT.exists() or OUT.read_text(encoding="utf-8") != text:
            print("permissions.ts güncel değil: python3 scripts/gen-permissions.py")
            sys.exit(1)
        return
    OUT.write_text(text, encoding="utf-8")
    n = sum(len(g["items"]) for g in groups.values())
    print(f"{OUT.relative_to(ROOT)}: {len(groups)} grup, {n} izin")


if __name__ == "__main__":
    main()
