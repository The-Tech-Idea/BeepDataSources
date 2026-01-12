using System;
using System.Data;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        #region "IDataSource Interface Methods"
        public virtual ConnectionState Openconnection()
        {
            if (RDBMSConnection != null)
            {
                ConnectionStatus = RDBMSConnection.OpenConnection();
            }
            return ConnectionStatus;
        }
        public virtual ConnectionState Closeconnection()
        {
            if (RDBMSConnection != null)
            {
                ConnectionStatus = RDBMSConnection.CloseConn();
                Dataconnection.CloseConn();
            }
            if (Dataconnection != null)
            {

                Dataconnection.CloseConn();
            }
            return ConnectionStatus;
        }
        #endregion
    }
}
