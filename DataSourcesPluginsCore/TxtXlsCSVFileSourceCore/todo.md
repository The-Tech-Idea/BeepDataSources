# TxtXlsCSVFileSource Enhancement TODO

Tracking enhancement work for `TxtXlsCSVFileSource` (file-based IDataSource).

## Completed Tasks

✅ **1. Stabilize reads** 
   - Fixed double-increment bug in `GetSheets`
   - Fixed `GetSheetNumber` loop logic
   - Added defensive index checks in `GetEntityIdx`, `GetEntityStructure`, `GetEntityType`, `GetEntityDataType`, `GetSheetEntity`
   - Fixed ToLine filter bug in `GetEntity`

✅ **2. Implement CSV write (Option A)**
   - Added `WriteRowsToCsv` with append/overwrite support
   - Implemented `InsertEntity` to append rows to CSV
   - Implemented `UpdateEntities` to overwrite CSV
   - Added CSV helpers: `EscapeCsvValue`, `SerializeObjectToCsvRow`

✅ **3. Implement Excel write**
   - Added `WriteRowsToExcel` using NPOI for append/overwrite
   - Updated `CreateEntityAs` to create Excel headers (.xlsx/.xls)
   - Added NPOI package references to csproj

✅ **4. Dispose fixes**
   - Implemented `Dispose(bool disposing)` with reader/stream cleanup
   - Replaced silent catches with logging in dispose block

✅ **5. Error-handling sweep**
   - Hardened catch blocks in: GetScalar, BeginTransaction, EndTransaction, Commit
   - Hardened catches in: CreateEntityAs, RunQuery, GetCreateEntityScript
   - Hardened catches in: field scanning, type/size detection, GetSheetEntity, pagination
   - Set `ErrorObject` and added `Logger` calls throughout

## Pending Tasks

⏳ **6. Build & verify compilation**
   - Run: `dotnet restore` && `dotnet build` for TxtXlsCSVFileSourceCore
   - Verify NPOI packages resolve and code compiles

⏳ **7. Manual validation** (optional)
   - Test CSV append/overwrite with sample files
   - Test Excel write (.xlsx/.xls) with sample data

⏳ **8. Async I/O** (future enhancement)
   - Replace `Task.Run` wrappers with native async FileStream APIs
   - Implement async versions: `GetEntityAsync`, `GetScalarAsync`, `ReadDataTable`

---

## Notes
- No unit tests added per original request
- NPOI added for Excel writes (CSV uses StreamWriter)
- All error handling now surfaces via `IErrorsInfo` with logging
- Atomic writes for CSV/Excel overwrite via temp file + replace pattern
