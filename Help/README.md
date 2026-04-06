# Beep DataSources — static HTML help

Open **`index.html`** in a browser or serve this folder with any static host.

- **Navigation / theme:** `navigation.js` (localStorage key `beep-datasources-docs-theme`)
- **Styles:** `sphinx-style.css` (adapted from Beep Streaming / DME docs)
- **Phased authoring:** `../.plans/` (start at `README.md` and `00-MASTER-PLAN.md`)

Platform pages summarize **BeepDM** behavior; authoritative text lives in **`BeepDM/.cursor/<skill>/SKILL.md`**.

After adding or renaming pages, run **`python Help/tools/check-nav-mapping.py`** from the repo root so `navigation.js` stays aligned with every HTML file. Run **`python Help/tools/check-help-links.py`** to verify relative `href` and local `src=` targets under `Help/`. **`python Help/tools/verify-help.py`** runs that plus the nav-mapping script.
