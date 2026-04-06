# Phase 02 — BeepDM platform integration

## Objective

Document how **DataSources** projects are consumed from **BeepDM** applications: editor, initialization, persisted connections, and the `IDataSource` contract. Content is summarized in HTML; depth stays in BeepDM skills.

## BeepDM `.cursor` sources

- `BeepDM/.cursor/beepdm/SKILL.md` (+ `reference.md`)
- `BeepDM/.cursor/beepservice/SKILL.md` (+ `reference.md`)
- `BeepDM/.cursor/configeditor/SKILL.md` (+ `reference.md`)
- `BeepDM/.cursor/connection/SKILL.md` (+ `reference.md`)
- `BeepDM/.cursor/connectionproperties/SKILL.md` (+ `reference.md`)
- `BeepDM/.cursor/idatasource/SKILL.md` (+ `reference.md`)

## Target HTML files

| File | Topics |
|------|--------|
| `Help/platform-beepdm.html` | `IDMEEditor`, ConfigEditor relationship, when datasources load; **Datasource help in this repo** → phased rollout + flagship <code>conn-*</code> |
| `Help/platform-beepservice.html` | Initializing Beep in desktop/hosted apps; **Plugin documentation in this repo** → phased rollout + flagship <code>conn-*</code> |
| `Help/platform-configeditor.html` | Persisting connections, drivers path, delegated managers; **Documented datasource families** → phased rollout + flagship index |
| `Help/platform-connection.html` | Open/validate/mask connection strings, driver resolution; **Family-specific notes** → phased implementations + flagship <code>conn-*</code> for REST |
| `Help/platform-connection-properties.html` | Building `ConnectionProperties` safely; **REST / SaaS** subsection links `impl-connectors.html` + `#flagship-provider-pages` for `WebAPIConnectionProperties` |
| `Help/platform-idatasource.html` | Contract summary: connection, CRUD, metadata, errors, helpers; **Provider docs** links phased rollout + flagship <code>conn-*</code> pages |

## TODO checklist

- [x] Draft each platform page from skill headings (no duplication of full skill text)
- [x] Cross-link between Connection ↔ ConnectionProperties ↔ IDataSource
- [x] Add note: plugin assemblies loaded via AssemblyHandler / shared context (link BeepDM skills if repo consumers need it)
- [ ] Update `navigation.js` if new platform pages are split further

## Verification

- [ ] A new contributor can trace: **app start → BeepService → ConfigEditor → ConnectionProperties → IDataSource**
- [ ] Links to `.cursor` paths are correct relative to a typical monorepo checkout (BeepDM sibling or submodule — note assumptions in HTML)

## Dependency

Phase 01 complete (shell available).
