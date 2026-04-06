# Phase 01 — HTML framework & conventions

## Objective

Establish the static Help site: shared styles, sidebar navigation, search box, dark/light theme, and entry pages so later phases only add content files.

## Target files

| File | Purpose |
|------|---------|
| `Help/sphinx-style.css` | Shared typography, layout, tables, admonitions |
| `Help/navigation.js` | `NavigationManager`, theme key `beep-datasources-docs-theme` |
| `Help/index.html` | Home, feature grid, links to `.plans` |
| `Help/getting-started.html` | How to read docs, clone layout, link to roadmap + phased rollout; phase 09 flagship pointer and link to `phased-implementations.html` |
| `Help/roadmap.html` | Human-readable phase summary (mirrors `.plans`) |

## TODO checklist

- [x] Copy/adapt `sphinx-style.css` with DataSources branding
- [x] Implement `navigation.js` with submenu sections: Getting started, BeepDM platform, Implementation (phased), Reference
- [x] Add `index.html` and `getting-started.html`
- [x] Add `roadmap.html`
- [x] Add `Help/README.md` pointer

## Conventions for later phases

- New pages: duplicate `<head>` / sidebar / `navigation.js` pattern from `getting-started.html`.
- Register every page in `createNavigationMapping()` and `getNavigationHTML()`.
- Prefer **one major topic per HTML file**; split long provider lists into category pages in phase 08.
- Pages under `Help/providers/` use `../sphinx-style.css`, `../navigation.js`, and `../`-relative links; `navigation.js` sets `baseHref` to `../` when the path contains `/providers/` so sidebar links resolve.

## Verification

- [ ] Sidebar search filters links without errors
- [ ] Active page highlights correctly on each shipped page
- [ ] Theme persists across reload
- [x] `Help/tools/check-nav-mapping.py` — confirms each `Help/**/*.html` basename has a `navigation.js` mapping entry (run from repo root: `python Help/tools/check-nav-mapping.py`)
- [x] `Help/tools/check-help-links.py` — confirms relative `href` and local `src=` targets exist under `Help/`
- [x] `Help/tools/verify-help.py` — runs nav mapping + link checks (`python Help/tools/verify-help.py`)

## References

- Template style: `Beep.StreamingEvents/Help/`
- Phase 02 for filling platform nav links with real content
