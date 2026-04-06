# Phase 09 — Flagship connector provider pages

## Objective

Add focused **`Help/providers/conn-*.html`** pages for high-traffic REST connectors: project path, `DataSourceType`, `WebAPIDataSource` usage, entities/commands at a glance, and links back to category hubs.

## Target HTML

### Wave A (shipped)

| File | Connector | Repo path |
|------|-----------|-----------|
| `Help/providers/conn-salesforce.html` | Salesforce | `Connectors/CRM/SalesforceDataSource/` |
| `Help/providers/conn-slack.html` | Slack | `Connectors/Communication/SlackDataSource/` |
| `Help/providers/conn-shopify.html` | Shopify | `Connectors/E-commerce/ShopifyDataSource/` |
| `Help/providers/conn-twitter.html` | Twitter / X | `Connectors/SocialMedia/Twitter/` |

### Wave B (shipped)

| File | Connector | Repo path |
|------|-----------|-----------|
| `Help/providers/conn-hubspot.html` | HubSpot | `Connectors/CRM/HubSpotDataSource/` |
| `Help/providers/conn-zendesk.html` | Zendesk | `Connectors/CustomerSupport/Zendesk/` |
| `Help/providers/conn-microsoft-teams.html` | Microsoft Teams | `Connectors/Communication/MicrosoftTeamsDataSource/` |

### Wave C (shipped)

| File | Connector | Repo path |
|------|-----------|-----------|
| `Help/providers/conn-dynamics365.html` | Dynamics 365 | `Connectors/CRM/Dynamics365DataSource/` |
| `Help/providers/conn-mailchimp.html` | Mailchimp | `Connectors/Marketing/MailchimpDataSource/` |
| `Help/providers/conn-google-chat.html` | Google Chat | `Connectors/Communication/GoogleChatDataSource/` |

### Wave D (shipped)

| File | Connector | Repo path |
|------|-----------|-----------|
| `Help/providers/conn-asana.html` | Asana | `Connectors/TaskManagement/AsanaDataSource/` |
| `Help/providers/conn-bigcommerce.html` | BigCommerce | `Connectors/E-commerce/BigCommerceDataSource/` |
| `Help/providers/conn-gmail.html` | Gmail | `Connectors/MailServices/Gmail/` |

**Ring order (prev / next footer links):** Salesforce → Slack → Shopify → Twitter / X → HubSpot → Zendesk → Teams → Dynamics 365 → Mailchimp → Google Chat → Asana → BigCommerce → Gmail → (back to) Salesforce. Category **Conn:** hubs follow in `navigation.js`.

## TODO checklist

- [x] Pages use same shell as other `providers/*.html` (`../` assets, `../navigation.js`)
- [x] `navigation.js` maps each filename to a unique `activeId`
- [x] `impl-connectors.html` lists flagship pages in a **single table** (`#flagship-provider-pages`): help link, category hub, repo folder, `DataSourceType`; home `index.html` links to that anchor; category hubs link from table rows where applicable
- [x] Cross-discovery: `Help/EXECUTION_SUMMARY.md` subsection for static HTML flagship docs; `Help/repo-layout.html` Connectors row links overview + `#flagship-provider-pages`; `navigation.js` sidebar **Flagship conn-* index (table)** and **active** state when URL hash is `#flagship-provider-pages` on `impl-connectors.html`; phase 08 plan notes the anchor on `impl-connectors.html`; each flagship `conn-*.html` **Also read** includes the flagship table link; `impl-messaging-vector.html` **Contrast: product REST** + next-line phases 08–09; all six **platform-*.html** pages link phased rollout and/or flagship index where relevant (`platform-beepdm`, `platform-beepservice`, `platform-configeditor`, `platform-connection-properties`, `platform-connection`, `platform-idatasource`); eight category hubs that ship `conn-*` links add a post-table note to the flagship anchor; `impl-connectors.html` BeepDM skills paragraph links the in-page table

## Verification

- [x] `DataSourceType` and `DatasourceCategory.Connector` match source `AddinAttribute`
- [x] No broken relative links from `Help/providers/`

## Dependency

Phase 08 (`impl-connectors.html` + category hubs) complete.

## Follow-ups

- More `conn-*.html` (e.g. Stripe, Trello, Monday) incrementally — **Stripe** is not in repo yet; **Gmail** shipped in wave D.
- Optional OAuth / rate-limit appendix per vendor

## API documentation pattern (all flagship `conn-*.html`)

Each provider page should include, derived from the matching `*DataSource.cs` and `Models`:

1. **Source & `IDataSource`** — repo path, base class, and how reads/writes are routed.
2. **Command methods** — table: `CommandAttribute.Name`, C# signature, return type, `ObjectType` (note when `Name` is omitted and the method name is the command key).
3. **Model types** — namespace and main DTO / helper classes.
4. **Connection** — how `WebAPIConnectionProperties` (or noted auth) applies; place **after** model types when the page also has intro sections (e.g. API surface, entities). Pages with vendor-specific preamble (e.g. registration, entities-only) may keep connection earlier if documented that way.
