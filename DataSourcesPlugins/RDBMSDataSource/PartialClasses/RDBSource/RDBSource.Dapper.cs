using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using System.Linq;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        #region "Dapper"
        public virtual List<T> GetData<T>(string sql)
        {
            // DMEEditor.OpenDataSource(ds.DatasourceName);
            if (Dataconnection.ConnectionStatus == ConnectionState.Open)
            {
                List<T> t = RDBMSConnection.DbConn.Query<T>(sql).AsList<T>();

                return t;
            }
            else
                return null;
        }
        public virtual Task SaveData<T>(string sql, T parameters)
        {
            if (Dataconnection.ConnectionStatus == ConnectionState.Open)
            {
                return RDBMSConnection.DbConn.ExecuteAsync(sql, parameters);
            }
            else
                return null;


        }
        #endregion
    }
}
