# Phase 08 — REST / SaaS connectors (`Connectors/`)

## Objective

Document the large **Connectors** tree by **category** (CRM, Marketing, Communication, …) and the standard Web API datasource pattern (commands, models, base class).

## BeepDM `.cursor` sources

- `BeepDM/.cursor/idatasource/SKILL.md`
- Existing repo analysis: `Help/REFACTORING_ANALYSIS.md`, `Help/EXECUTION_SUMMARY.md`

## Repo targets

- `Connectors/<Category>/<Vendor>DataSource/` (or vendor-named folder)
- Reference implementations called out in refactoring docs

## Target HTML

| File | Content |
|------|---------|
| `Help/impl-connectors.html` | Pattern: `WebAPIDataSource`, `CommandAttribute`, `Models.cs`, entity mapping; `#flagship-provider-pages` index table; category hub section notes which hubs repeat the flagship pointer |
| `Help/connectors-*.html` | One hub per top-level category folder (16 hubs) |

### Shipped (first wave)

- `Help/impl-connectors.html`
- `Help/connectors-accounting.html` … `connectors-task-management.html` (see `impl-connectors.html` category table)
- `Help/navigation.js` — overview + all category entries with `activeId` mappings
- Cross-links: `phased-implementations.html` (prerequisite mentions `platform-*.html` + flagship anchor), `roadmap.html` (shipped + phase 08 hub notes), `getting-started.html` (phase 09 pointer), `index.html`, `impl-messaging-vector.html`, `repo-layout.html` (`Help/` row + `Connectors/` row)

## TODO checklist

- [x] Inventory categories from `Connectors/` directory list (excludes `.vs`)
- [x] Each hub lists child connectors with one-line purpose
- [x] Deep-dive only for flagship connectors to avoid churn (Twitter called out on overview + social hub)
- [x] Category hubs that list flagship `conn-*` rows (CRM, Communication, E-commerce, Social media, Customer support, Marketing, Task management, Mail services) include a note after the table linking `impl-connectors.html#flagship-provider-pages`

## Verification

- [x] Category list matches actual `Connectors/` subfolders: Accounting, BusinessIntelligence, Cloud-Storage, Communication, ContentManagement, CRM, CustomerSupport, E-commerce, Forms, IoT, MailServices, Marketing, MeetingTools, SMS, SocialMedia, TaskManagement
- [x] Align with `EXECUTION_SUMMARY` connector counts where helpful (~105+ analyzed)

## Dependency

Phases 01–02 complete; phase 04+ optional for contrast with REST pattern.

## Follow-ups (optional)

- ~~Add `Help/providers/conn-*.html` for flagship vendors~~ — see **phase 09** (`phase-09-connectors-flagship.md`).
- Shorten sidebar: nested “Connectors” submenu if `sphinx-style.css` gains nested rules
