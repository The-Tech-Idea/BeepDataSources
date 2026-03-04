using System.Data;

namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLiteDataSource
    {
        public override ConnectionState Openconnection()
        {
            var token = new CancellationTokenSource();
            var progress = new Progress<PassedArgs>(_ => { });

            EnsureConnectionProp();
            InMemory = Dataconnection.ConnectionProp.IsInMemory;
            Dataconnection.InMemory = InMemory;

            if (Dataconnection.ConnectionStatus == ConnectionState.Open)
            {
                ConnectionStatus = ConnectionState.Open;
                return ConnectionStatus;
            }

            if (InMemory)
            {
                OpenDatabaseInMemory(Dataconnection.ConnectionProp.Database);
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                    LoadStructure(progress, token.Token, false);
                    CreateStructure(progress, token.Token);
                    ConnectionStatus = ConnectionState.Open;
                    Dataconnection.ConnectionStatus = ConnectionState.Open;
                }
                else
                {
                    ConnectionStatus = ConnectionState.Closed;
                    Dataconnection.ConnectionStatus = ConnectionState.Closed;
                }
                return ConnectionStatus;
            }

            base.Openconnection();
            ConnectionStatus = Dataconnection.ConnectionStatus;
            return ConnectionStatus;
        }

        public override ConnectionState Closeconnection()
        {
            try
            {
                if (base.RDBMSConnection?.DbConn == null)
                {
                    ConnectionStatus = ConnectionState.Closed;
                    Dataconnection.ConnectionStatus = ConnectionState.Closed;
                    return ConnectionStatus;
                }

                if (Dataconnection.ConnectionProp?.IsInMemory == true)
                {
                    SaveStructure();
                }

                if (base.RDBMSConnection.DbConn.State == ConnectionState.Open)
                {
                    base.RDBMSConnection.DbConn.Close();
                }

                ConnectionStatus = ConnectionState.Closed;
                Dataconnection.ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Beep", "Closing connection to SQLite database", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                SetError(Errors.Failed, "Error closing SQLite connection.", ex);
                DMEEditor.AddLogMessage("Beep", $"Error closing connection to SQLite database: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }

            return ConnectionStatus;
        }
    }
}
