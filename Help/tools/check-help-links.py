"""Verify relative href and src targets under Help/ resolve to existing files (fragments ignored)."""
from __future__ import annotations

import re
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
HREF_RE = re.compile(r"""href\s*=\s*["']([^"']+)["']""", re.I)
SRC_RE = re.compile(r"""src\s*=\s*["']([^"']+)["']""", re.I)


def _skip_url(raw: str) -> bool:
    r = raw.strip()
    if not r or r.startswith("#"):
        return True
    low = r.lower()
    if low.startswith(
        ("http://", "https://", "mailto:", "javascript:", "tel:", "data:", "blob:")
    ):
        return True
    return False


def _check_attribute(
    html_path: pathlib.Path,
    raw: str,
    attr_name: str,
    broken: list[tuple[pathlib.Path, str, str, pathlib.Path]],
) -> bool:
    """Returns True if this attribute was a local path check under Help/."""
    if _skip_url(raw):
        return False
    path_part, _sep, _frag = raw.partition("#")
    path_part = path_part.strip()
    if not path_part:
        return False
    target = (html_path.parent / path_part).resolve()
    try:
        target.relative_to(ROOT.resolve())
    except ValueError:
        broken.append((html_path, attr_name, raw, target))
        return True
    if not target.is_file():
        broken.append((html_path, attr_name, raw, target))
    return True


def main() -> int:
    broken: list[tuple[pathlib.Path, str, str, pathlib.Path]] = []
    checked_href = 0
    checked_src = 0

    for html_path in sorted(ROOT.rglob("*.html")):
        text = html_path.read_text(encoding="utf-8", errors="replace")
        for raw in HREF_RE.findall(text):
            if _check_attribute(html_path, raw, "href", broken):
                checked_href += 1
        for raw in SRC_RE.findall(text):
            if _check_attribute(html_path, raw, "src", broken):
                checked_src += 1

    print(
        f"Scanned Help/**/*.html; checked {checked_href} relative file hrefs, "
        f"{checked_src} relative file src=."
    )
    if not broken:
        print("OK: no missing relative href/src targets under Help/.")
        return 0

    print("Broken relative href/src:")
    for src, attr, val, resolved in broken:
        rel_src = src.relative_to(ROOT)
        print(f"  {rel_src}: {attr}={val!r} -> {resolved}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
