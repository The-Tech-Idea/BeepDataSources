using System;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// SQL Server Compact Edition (SQL CE 4.0) data source — inherits BeginTransaction /
    /// EndTransaction / Commit / CRUD / paging / type mapping from <see cref="RDBSource"/>.
    /// </summary>
    /// <remarks>
    /// This class did not exist. `ConnectionHelper.CreateSqlCompactConfig()` in BeepDM has always
    /// declared `classHandler = "SQLCompactDataSource"` — the plugin discovery system (see
    /// CLAUDE.md's "three separate registrations") has been looking, by reflection, for a class with
    /// this exact name ever since that config was written, and finding nothing. SQL Compact was
    /// registerable in the connection UI but unusable: selecting it could never construct a working
    /// datasource instance.
    ///
    /// No ADO.NET provider package is referenced here, deliberately, and for the same reason
    /// `FireBirdDataSource` (which references nothing beyond `RDBSource`) needs none either:
    /// `RDBDataConnection` resolves the actual provider types (`DbConnectionType`, `AdapterType`,
    /// `CommandBuilderType`) from `ConnectionDriversConfig` by name at runtime. This driver needs a
    /// provider-specific override only where the generic `IDbConnection`/`IDbCommand` surface is not
    /// enough — Oracle's `ConfigureCommand` for `BindByName` is the model — and SQL CE needs none.
    ///
    /// One thing this driver genuinely cannot do anything about: `CreateSqlCompactConfig()` names
    /// the provider package as `System.Data.SqlServerCe` 4.0.0.0, which was never published to
    /// nuget.org (confirmed — the package ID has no versions there) and, being SQL Server Compact's
    /// native ADO.NET provider, is Windows/.NET-Framework-only regardless; it was never ported to
    /// .NET Core or later. That is an environment/runtime limitation of the discontinued database
    /// engine itself, not something a driver class can work around, and it does not block this class
    /// from existing, compiling, registering, or handling every dialect decision RDBSource asks a
    /// driver to specialise.
    /// </remarks>
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlCompact)]
    public class SQLCompactDataSource : RDBSource, IDataSource
    {
        public SQLCompactDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        // No ColumnDelimiter/ParameterDelimiter override: SQL CE uses SQL Server's own square-bracket
        // identifiers and "@"-prefixed named parameters, which are already what QuoteIdentifier and
        // RDBSource's base ParameterDelimiter produce for DataSourceType.SqlCompact — see
        // RDBSource.Utilities.cs's QuoteIdentifier switch, which already lists SqlCompact alongside
        // SqlServer/AzureSQL/VistaDB. No paging override either: RDBMSHelper.GetPagingSyntax and
        // DatabaseDMLUtilities already emit "OFFSET ... ROWS FETCH NEXT ... ROWS ONLY" for
        // DataSourceType.SqlCompact (SQL CE 4.0 supports the ANSI OFFSET/FETCH clause).

        /// <summary>
        /// SQL Server Compact has no equivalent of SQL Server's <c>ALTER TABLE ... NOCHECK
        /// CONSTRAINT</c> — a foreign key in SQL CE cannot be disabled without dropping and
        /// recreating it. Reported as a no-op success, the same pattern Spanner/Presto/Snowflake use
        /// for a constraint-toggle operation their engine genuinely does not support, so callers that
        /// toggle FK checks uniformly across every RDBMS driver do not have to special-case this one.
        /// </summary>
        public override string DisableFKConstraints(EntityStructure t1)
        {
            const string message = "SQL Server Compact does not support disabling individual foreign key constraints; drop and recreate the constraint instead.";
            DMEEditor.ErrorObject.Message = message;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            return message;
        }

        /// <inheritdoc cref="DisableFKConstraints"/>
        public override string EnableFKConstraints(EntityStructure t1)
        {
            const string message = "SQL Server Compact does not support enabling individual foreign key constraints; drop and recreate the constraint instead.";
            DMEEditor.ErrorObject.Message = message;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            return message;
        }
    }
}
