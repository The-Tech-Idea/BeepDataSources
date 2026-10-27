using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor.SchemaMigration;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Native SQL Server LocalDB catalog operations for MigrationManager's pre-open lifecycle.
    /// The configured driver factory owns connection/command types; callers supply no SQL.</summary>
    public sealed class SQLServerDatabaseTargetMigrationProvider : IDatabaseTargetMigrationProvider
    {
        private readonly DbProviderFactory _factory;
        private readonly string _connectionString, _masterConnectionString, _ownershipScope;
        public DatabaseTargetMigrationIdentity Identity { get; }
        public string ConnectionString => _connectionString;
        public SQLServerDatabaseTargetMigrationProvider(DbProviderFactory factory, string connectionString, string ownershipScope)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            var target = factory.CreateConnectionStringBuilder() ?? throw new NotSupportedException("The SQL Server driver has no connection-string builder.");
            target.ConnectionString = connectionString;
            var server = Convert.ToString(target["Data Source"], CultureInfo.InvariantCulture) ?? string.Empty;
            var database = Convert.ToString(target["Initial Catalog"], CultureInfo.InvariantCulture) ?? string.Empty;
            if (!server.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase) ||
                !Convert.ToBoolean(target["Integrated Security"], CultureInfo.InvariantCulture) ||
                !string.IsNullOrEmpty(Convert.ToString(target["AttachDBFilename"], CultureInfo.InvariantCulture)) ||
                Convert.ToBoolean(target["User Instance"], CultureInfo.InvariantCulture) ||
                !Regex.IsMatch(database, @"\A[A-Za-z][A-Za-z0-9_]{0,79}\z", RegexOptions.CultureInvariant) ||
                new[] { "master", "model", "msdb", "tempdb" }.Contains(database, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("An explicit integrated-security LocalDB user database is required.", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(ownershipScope) || ownershipScope.Length > 128)
                throw new ArgumentException("A bounded native ownership scope is required.", nameof(ownershipScope));
            target["Connect Timeout"] = 5; target["Pooling"] = false;
            _connectionString = target.ConnectionString; _ownershipScope = ownershipScope;
            target["Initial Catalog"] = "master"; _masterConnectionString = target.ConnectionString;
            Identity = new("SqlServerLocalDB", server, database, ownershipScope);
        }
        private DbConnection Connection(bool master = false)
        {
            var connection = _factory.CreateConnection() ?? throw new NotSupportedException("The SQL Server driver has no native connection.");
            try { connection.ConnectionString = master ? _masterConnectionString : _connectionString; return connection; }
            catch { connection.Dispose(); throw; }
        }
        private static DbCommand Command(DbConnection connection, string sql)
        {
            var command = connection.CreateCommand();
            try { command.CommandText = sql; command.CommandTimeout = 10; return command; }
            catch { command.Dispose(); throw; }
        }
        private static void Parameter(DbCommand command, string name, string value)
        {
            var parameter = command.CreateParameter(); parameter.ParameterName = name;
            parameter.DbType = DbType.String; parameter.Size = 128; parameter.Value = value; command.Parameters.Add(parameter);
        }
        public bool DatabaseExists()
        {
            using var connection = Connection(true); connection.Open();
            using var command = Command(connection, "SELECT COUNT(*) FROM sys.databases WHERE name=@database");
            Parameter(command, "@database", Identity.Database);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
        }
        public bool OwnerMatches(Guid owner)
        {
            if (owner == Guid.Empty) throw new ArgumentException("A nonempty owner is required.", nameof(owner));
            using var connection = Connection(); connection.Open();
            using var command = Command(connection, "SELECT CONVERT(nvarchar(128),value) FROM sys.extended_properties WHERE class=0 AND name=@property");
            Parameter(command, "@property", _ownershipScope);
            return Guid.TryParse(command.ExecuteScalar() as string, out var actual) && actual == owner;
        }
        public async Task CreateDatabaseAsync(CancellationToken cancellationToken)
        {
            await using var connection = Connection(true); await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = Command(connection, "CREATE DATABASE [" + Identity.Database + "]");
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        public async Task StampOwnerAsync(Guid owner, CancellationToken cancellationToken)
        {
            if (owner == Guid.Empty) throw new ArgumentException("A nonempty owner is required.", nameof(owner));
            await using var connection = Connection(); await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = Command(connection, "EXEC sys.sp_addextendedproperty @name=@property,@value=@owner");
            Parameter(command, "@property", _ownershipScope); Parameter(command, "@owner", owner.ToString("D"));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
