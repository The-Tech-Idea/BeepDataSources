# CRM Connectors Refactoring Progress

This document tracks the migration of CRM connectors to the shared WebAPIDataSource pattern ("Twitter pattern") with strongly typed models.

## Status Legend

- ✅ Completed – connector fully aligned with the new pattern and documented.
- 🚧 In progress – currently being refactored.
- ⏳ Pending – queued for refactor.

## Connector Overview

| Connector | Status | Notes | Last Updated |
|-----------|--------|-------|--------------|
| HubSpot | ✅ Completed | Refactored to WebAPIDataSource map pattern with typed models. | 2025-10-03 |
| Copper | ✅ Completed | Entity mapping + pagination implemented using shared helpers. | 2025-10-03 |
| Freshsales | ✅ Completed | Twitter-pattern refactor with typed models/helpers and pagination. | 2025-10-04 |
| Insightly | ⏳ Pending | Uses legacy IDataSource implementation. | 2025-10-03 |
| Dynamics365 | ⏳ Pending | Legacy Graph-based IDataSource implementation. | 2025-10-03 |
| Pipedrive | ⏳ Pending | Legacy connector awaiting migration. | 2025-10-03 |
| Nutshell | ⏳ Pending | Legacy connector awaiting migration. | 2025-10-03 |
| SugarCRM | ⏳ Pending | Legacy connector awaiting migration. | 2025-10-03 |
| Salesforce | ⏳ Pending | Needs review for alignment with shared pattern. | 2025-10-03 |
| Zoho | ⏳ Pending | Newer implementation but still requires consolidation. | 2025-10-03 |

## Next Actions

1. Kick off Insightly refactor, mirroring the Freshsales pattern (entity map + typed models).
2. Continue down the priority list (Insightly → Dynamics365 → Pipedrive/Nutshell → others).
3. Update this tracker after each connector reaches the ✅ state.
