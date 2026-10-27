# Bounded relational pages

RDBDataSource 2.1.2 implements BeepDM's optional `IBoundedPagedDataSource` on the existing shared `RDBSource`. PostgreDataSourceCore 1.0.5 uses the same local BeepDM model/engine projects as this owner; mixing its older packaged contracts with the local source cannot compile the new interface.

Use `UnitofWork<T>.PreparePageReadAsync` for application reads. It copies predicates, adds the authoritative tenant predicate independently, appends the complete declared primary key to ordering, validates the page envelope and schema, and publishes through the existing stage admission callback. Do not replace it with `GetEntity` plus `Skip/Take`.

The original five-argument `BoundedPageRequest` constructor remains available. Its six-argument overload accepts one any-of filter group. All required filters stay AND predicates outside that parenthesized OR group; `WithValues` preserves the alternatives while the UoW adds its tenant filter. It is query data, not an authorization credential. The caller still owns business admission and publication context checks.

## Producer behavior

- Requires cached entity fields, a declared complete unique primary key, declared filter/order fields resolved to their exact physical metadata spelling and an order containing every key. Unsupported operators and empty predicates refuse; they are never silently dropped.
- Supports the strict query path for SQLite, PostgreSQL and SQL Server/Azure SQL. Other relational dialects refuse explicitly. A source implementing the optional interface does not prove that its actual provider/configuration is qualified.
- Creates an independent ADO.NET connection from the configured provider factory and original connection settings. It does not enlist in, commit, close or reuse the caller's shared source connection or active writer transaction.
- Runs filtered count and page queries in one transaction: SQLite/SQL Server Serializable; PostgreSQL Repeatable Read. The SQL Server count uses COUNT_BIG; paging offsets are checked Int64 values.
- SQL projection emits a leading oversize marker and replaces a rejected cell with NULL. The marker produces a failed task; the caller never receives a truncated successful row. SQLite checks every field because its dynamic storage can hold large text in a declared numeric column. PostgreSQL/SQL Server also check every cell.
- Only admitted scalar cells are extracted. Encoded JSON uses a fixed-capacity stream, with a maximum row count, field byte guards, strict snapshot/count consistency and an immutable response. Binary values are base64. No background work survives cancellation: the whole producer lifetime is awaited, including drivers whose ADO.NET async implementation is synchronous.
- Required literal contains search escapes wildcard characters and uses LOWER. The strict bounded filter path also supports `equalsignorecase` and `notequalsignorecase`: it uses the existing parameter builder and LOWER equality/inequality, so `%`, `_`, brackets and dollar characters remain literal. Beep.DoitAll product creation consumes this for its one-row SKU preflight. Unicode case folding, collation and sort semantics depend on the database; they are not claimed equivalent to .NET's OrdinalIgnoreCase.
- Anonymous SQLite `:memory:` connections refuse this independent-connection path. File databases and named shared-memory databases are the supported arrangements.

Failures report through the existing HandleDatabaseError owner and rethrow. The task's response/failure is the read outcome; a mutable shared ErrorObject is not a concurrent operation receipt. Existing legacy reads/writes retain their own behavior.

## Focused evidence and limits

`tests/RDBDataSource.Tests/BoundedPageTests.cs` executes this producer over Microsoft.Data.Sqlite: two-company 10k/100k fixtures, count/page bounds, tie ordering, literal OR search, independent connection/transaction observation, large offsets, physical cancellation acknowledgement, text/encoded/binary limits and SQLite dynamic storage. Fixture SQL is setup, not application query code.

Beep.DoitAll's ProductCatalogReadTests also use the actual SDK SQLite driver, the staged UoW and InventoryService. Their app evidence and remaining acceptance are recorded in `Beep.DoitAll/docs/plan/odoo-saas-review/11-product-catalog-paging.md`. Selected SQLite checks do not qualify live PostgreSQL/SQL Server deployments, count/offset latency at every data shape, financial writes, all inherited providers or every app list.

Microsoft.Data.Sqlite 10.0.8's streaming GetChars path crashed on CASE expression columns lacking an origin table. The producer uses SQL-admitted scalar extraction instead; oversized values are excluded before extraction. A test-host crash is not a handled application outcome.

Reviewed 2026-10-11: [SQLite isolation](https://www.sqlite.org/isolation.html), [PostgreSQL transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html), [SQL Server isolation](https://learn.microsoft.com/en-us/sql/t-sql/statements/set-transaction-isolation-level-transact-sql?view=sql-server-ver17). SQL Server Serializable may block writers; live index/latency/concurrency acceptance remains required.


Product-create follow-up, 2026-10-11: actual SDK SQLite/UoW product creation exercises literal case-insensitive SKU equality and refusal of failed preflight; the eleven bounded producer guards were rerun after the shared filter change. App receipts are in `Beep.DoitAll/docs/plan/odoo-saas-review/12-product-create-workflow.md`. This query is not a unique constraint or independent-writer serialization, and no live PostgreSQL/SQL Server collation qualification is claimed.


Sales list follow-up, 2026-10-11: the actual SDK/UoW SalesOrders consumer uses working/status/search/sort/count pages. Physical NULL values are written as JSON null before scalar serialization, because provider metadata can declare an underlying non-nullable type for a nullable column. DateTime/decimal/bool NULLs and populated scalar values have an actual SQLite guard; the app's required DateTime still refuses JSON null. This does not add date parsing/defaults or qualify P13/UTC/cross-provider behavior. The twelve final bounded producer checks and app evidence are recorded in `Beep.DoitAll/docs/plan/odoo-saas-review/13-sales-order-list-workspace.md`.


## Declared related numeric comparisons — 2026-10-13

RDBSource 2.1.6 implements optional IBoundedComparedRelatedPagedDataSource through the existing bounded related-query owner. PageRelatedComparison admits only declared parent/related fields with matching integer/decimal CLR types and a fixed =, <>, <, <=, >, >= symbol whitelist. Correlations and required/alternative child filters remain applied before both count and page; no application SQL expression is accepted. UnitOfWork refuses the capability when the producer does not declare it. Beep.DoitAll stock-balance inquiry consumes QuantityOnHand <= Product.ReorderLevel with positive threshold and current company/product correlation. This is not warehouse-rule, aggregate demand or replenishment qualification. SQLite/live-provider numeric storage and precision, indexing/concurrency and package/TFM gates remain scoped in the app Doc75.

Models/Engine 3.1.3 also offer explicit read-only scalar transport validation on bounded requests. The producer still transports every declared scalar; after writable fields load, the client validates supplied getter-only values without ignoring a cell. Existing request defaults remain writable-only. This does not make stale stored calculated columns valid or authorize schema repair.

## Complete coherent snapshot traversal — 2026-10-25

Models/Engine3.2.2 and RDB2.2.2 add optional IBoundedSnapshotPagedDataSource and UnitofWork<T>.VisitSnapshotAsync. Applications consume the UnitOfWork method, not direct datasource/SQL calls. It reuses the existing read admission, metadata/tenant fences and strict typed page decoder without replacing live Units. Callback batches are private synchronous computations; they must not perform provider I/O or publish partial results. Callback failure poisons the traversal epoch; an ignored rejection/retry cannot become successful completion. Closed or concurrent callbacks refuse.

The same RDBSource bounded query owner serves single-page and complete traversal APIs. A traversal starts at page1, owns one independent connection/transaction and filtered count, and visits all numbered batches inside that transaction. ForPage preserves correlation, copied filters/order, scalar validation and per-batch bounds. A batch size is not a total-row cap. Exact count, batch/row totals and completion correlation must agree; the receipt is observable only after commit and native reader/command/connection cleanup. Cancellation never abandons physical work. Legacy single-page signatures and semantics remain available.

The UoW traversal currently refuses related predicates until that boundary is qualified. Per-batch encoded-byte/cell bounds may refuse a window; no clipped prefix is acknowledged. The API does not establish an immutable session across independent invocations or prove arbitrary provider, collation, writer-latency, resource or platform behavior. Long Serializable verification may block writers. Audit hash/timestamp policy is owned by AuditService, not this transport.

Beep.DoitAll Doc193 records actual LocalDB/SQLite SDK/MigrationManager/UoW controls, including5013-row checks and corruption beyond row5000. Its isolated fixtures use UnitOfWork CRUD, not application SQL. Current execution status, original failed export-isolation evidence, warning debt and remaining ordinary/browser/operational acceptance are in that application tracker. This note is API documentation, not whole-product or cross-platform certification.
