using System;
using System.Data;
using System.Data.Common;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    internal interface IRdbParameterBinding
    {
        void Bind(IDbDataParameter parameter, object value);
    }

    internal sealed class NativeRdbParameterBinding : IRdbParameterBinding
    {
        public void Bind(IDbDataParameter parameter, object value) => parameter.Value = value ?? DBNull.Value;
    }

    /// <summary>
    /// SQL Server's default DateTime mapping is datetime2. Inferred datetime parameters
    /// lose precision and may fail a datetime2 equality guard with identical rendered values.
    /// Preserve seven-digit transport; legacy datetime-column qualification is separate.
    /// </summary>
    internal sealed class SqlServerRdbParameterBinding : IRdbParameterBinding
    {
        public void Bind(IDbDataParameter parameter, object value)
        {
            parameter.Value = value ?? DBNull.Value;
            if (value is DateTime) parameter.DbType = DbType.DateTime2;
        }
    }

    public partial class RDBSource
    {
        private static readonly IRdbParameterBinding NativeRdbParameters = new NativeRdbParameterBinding();
        private static readonly IRdbParameterBinding SqlServerRdbParameters = new SqlServerRdbParameterBinding();
        private IRdbParameterBinding RdbParameters => DatasourceType switch
        {
            DataSourceType.SqlServer or DataSourceType.AzureSQL => SqlServerRdbParameters,
            _ => NativeRdbParameters
        };
    }
}
