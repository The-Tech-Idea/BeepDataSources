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
| Insightly | ✅ Completed | Refactored to WebAPIDataSource pattern with strongly-typed POCO models and CommandAttribute methods. | 2025-10-11 |
| Dynamics365 | ✅ Completed | Refactored to WebAPIDataSource pattern with strongly-typed POCO models and CommandAttribute methods. | 2025-10-11 |
| Pipedrive | ✅ Completed | Refactored to WebAPIDataSource pattern with strongly-typed POCO models and CommandAttribute methods. | 2025-10-11 |
| Nutshell | ✅ Completed | Refactored to WebAPIDataSource pattern with strongly-typed POCO models and CommandAttribute methods. | 2025-10-11 |
| SugarCRM | ✅ Completed | Refactored to WebAPIDataSource pattern with strongly-typed POCO models and CommandAttribute methods. | 2025-10-11 |
| Salesforce | ✅ Completed | Refactored to WebAPIDataSource pattern with strongly-typed POCO models and CommandAttribute methods. | 2025-10-11 |
| Zoho | ✅ Completed | Refactored to WebAPIDataSource pattern with strongly-typed POCO models and CommandAttribute methods. | 2025-01-13 |

## Next Actions

1. All CRM connectors have been successfully refactored to the WebAPIDataSource pattern with strongly-typed POCO models and CommandAttribute methods.
2. All connectors compile successfully and follow the established framework patterns.
