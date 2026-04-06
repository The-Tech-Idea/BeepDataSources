"""Compare Help/**/*.html basenames to createNavigationMapping keys in navigation.js."""
from __future__ import annotations

import re
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent


def main() -> int:
    js = (ROOT / "navigation.js").read_text(encoding="utf-8")
    map_keys = set(re.findall(r"'([a-z0-9-]+\.html)':", js))
    html_names = {p.name for p in ROOT.rglob("*.html")}
    missing = sorted(html_names - map_keys)
    extra = sorted(map_keys - html_names)
    print(f"HTML files: {len(html_names)}, mapping keys: {len(map_keys)}")
    if missing:
        print("In filesystem but NOT in navigation.js mapping:")
        for x in missing:
            print(f"  {x}")
    if extra:
        print("In mapping but file missing:")
        for x in extra:
            print(f"  {x}")
    if not missing and not extra:
        print("OK: every HTML basename has a mapping entry and vice versa.")
        return 0
    return 1


if __name__ == "__main__":
    sys.exit(main())
