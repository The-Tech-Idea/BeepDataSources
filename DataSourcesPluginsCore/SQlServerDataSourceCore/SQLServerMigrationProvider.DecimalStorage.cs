using System;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLServerMigrationProvider : IDecimalColumnMigrationProvider
    {
        public string DecimalMigrationTargetIdentity
        {
            get
            {
                if (_decimalOwner is not RDBSource source || source.RDBMSConnection.DbConn is not System.Data.Common.DbConnection connection ||
                    connection.State != System.Data.ConnectionState.Open || string.IsNullOrWhiteSpace(connection.Database))
                    throw new InvalidOperationException("An open SQL Server target is required for decimal migration ownership.");
                return "SQLServer:" + connection.DataSource.ToUpperInvariant() + "/" + connection.Database.ToUpperInvariant();
            }
        }

        public IErrorsInfo WidenDecimalColumn(string entityName, EntityField expectedColumn, EntityField desiredColumn, string expectedTargetIdentity)
        {
            try
            {
                if (DecimalMigrationTargetIdentity != expectedTargetIdentity)
                    return SchemaMigrationResults.Fail("The decimal migration target changed after admission.");
                var expectedDatabase = ((RDBSource)_decimalOwner).RDBMSConnection.DbConn.Database.Replace("'", "''");
                if (!Identifier(entityName) || expectedColumn == null || desiredColumn == null ||
                    !Identifier(expectedColumn.FieldName) || !string.Equals(expectedColumn.FieldName, desiredColumn.FieldName, StringComparison.Ordinal) ||
                    expectedColumn.Fieldtype != typeof(decimal).FullName || desiredColumn.Fieldtype != typeof(decimal).FullName ||
                    expectedColumn.AllowDBNull != desiredColumn.AllowDBNull ||
                    expectedColumn.NumericPrecision < 1 || expectedColumn.NumericPrecision > 38 || desiredColumn.NumericPrecision < 1 || desiredColumn.NumericPrecision > 38 ||
                    !DataTypeMappingLookup.ContainsDecimalStorageShape(desiredColumn.NumericPrecision, desiredColumn.NumericScale,
                        expectedColumn.NumericPrecision, expectedColumn.NumericScale))
                    return SchemaMigrationResults.Fail("An exact SQL Server decimal widening with unchanged nullability and simple identifiers is required.");
                var (ddl, ok, error) = Helper.GenerateAlterColumnSql(entityName, expectedColumn.FieldName, desiredColumn);
                if (!ok || string.IsNullOrWhiteSpace(ddl)) return SchemaMigrationResults.Fail(error ?? "Decimal widening DDL is unavailable.");
                // Identifier validation excludes quotes/dots and binds the exact default-schema table
                // in both metadata and DDL. Native table locks guard the observation and ALTER together.
                var table = Helper.QuoteIdentifier(entityName);
                var column = expectedColumn.FieldName;
                var nullable = expectedColumn.AllowDBNull ? 1 : 0;
                var sql = $@"
IF @@TRANCOUNT <> 0 THROW 50174, 'Decimal widening requires its own transaction.', 1;
IF DB_NAME() <> N'{expectedDatabase}' THROW 50174, 'The decimal database changed after admission.', 1;
DECLARE @previousXactAbort bit = CASE WHEN (16384 & @@OPTIONS) = 16384 THEN 1 ELSE 0 END;
SET LOCK_TIMEOUT 10000;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @locked bit;
 SELECT TOP (1) @locked = 1 FROM {table} WITH (TABLOCKX, HOLDLOCK);
 DECLARE @p int, @s int, @n bit, @valid bit;
 SELECT @p=c.precision, @s=c.scale, @n=c.is_nullable,
 @valid=CASE WHEN c.system_type_id IN (106,108) AND c.user_type_id=c.system_type_id
   AND c.is_computed=0 AND c.is_identity=0 AND c.is_sparse=0 AND c.is_column_set=0
   AND c.is_filestream=0 AND c.generated_always_type=0 AND c.encryption_type IS NULL
   AND t.temporal_type=0 AND t.is_memory_optimized=0 AND t.is_filetable=0 THEN 1 ELSE 0 END
 FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id
 WHERE c.object_id=OBJECT_ID(N'{table}', N'U') AND c.name=N'{column}';
 IF ISNULL(@valid,0) <> 1 OR @n <> {nullable}
   THROW 50174, 'The exact supported decimal column or its nullability changed.', 1;
 IF @p={desiredColumn.NumericPrecision} AND @s={desiredColumn.NumericScale}
 BEGIN
   COMMIT TRANSACTION;
 END
 ELSE
 BEGIN
   IF @p <> {expectedColumn.NumericPrecision} OR @s <> {expectedColumn.NumericScale}
     THROW 50174, 'The source decimal shape changed; reconcile before retrying.', 1;
   {ddl};
   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'{table}', N'U') AND name=N'{column}'
      AND precision={desiredColumn.NumericPrecision} AND scale={desiredColumn.NumericScale} AND is_nullable={nullable})
     THROW 50174, 'The target decimal shape was not confirmed.', 1;
   COMMIT TRANSACTION;
 END;
 IF @previousXactAbort=0 SET XACT_ABORT OFF;
END TRY
BEGIN CATCH
 IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
 IF @previousXactAbort=0 SET XACT_ABORT OFF;
 THROW;
END CATCH;";
                // SET options inside sp_executesql revert when its scope exits, including errors.
                // SET LOCK_TIMEOUT cannot take a variable; restoring it in a nested EXEC would
                // restore only that nested scope. Keep the entire operation in one native scope.
                var scopedSql = "EXEC sys.sp_executesql N'" + sql.Replace("'", "''") + "';";
                var result = _decimalOwner.ExecuteSql(scopedSql);
                return result?.Flag == Errors.Ok
                    ? SchemaMigrationResults.Ok("Exact decimal widening confirmed; current stored values preserved.")
                    : SchemaMigrationResults.Fail(result?.Message ?? "Decimal widening was not acknowledged; reconcile live metadata before retrying.", result?.Ex);
            }
            catch (Exception ex)
            {
                _decimalOwner.DMEEditor?.AddLogMessage("SQLServerMigrationProvider", "Guarded decimal widening failed: " + ex, DateTime.Now, 0, null, Errors.Failed);
                return SchemaMigrationResults.Fail("Guarded decimal widening failed; reconcile live metadata before retrying.", ex);
            }
        }

        private static bool Identifier(string value) => value != null && Regex.IsMatch(value, @"\A[A-Za-z_][A-Za-z0-9_]{0,127}\z", RegexOptions.CultureInvariant);
    }
}
