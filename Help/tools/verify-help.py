"""Run all Help static checks (nav mapping + href/src). Exit non-zero if any fail."""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parent


def main() -> int:
    scripts = [
        TOOLS / "check-nav-mapping.py",
        TOOLS / "check-help-links.py",
    ]
    code = 0
    for path in scripts:
        print(f"--- {path.name} ---", flush=True)
        r = subprocess.run([sys.executable, str(path)], cwd=str(TOOLS.parent))
        if r.returncode != 0:
            code = 1
        print()
    if code == 0:
        print("verify-help: all checks passed.")
    else:
        print("verify-help: one or more checks failed.", file=sys.stderr)
    return code


if __name__ == "__main__":
    sys.exit(main())
