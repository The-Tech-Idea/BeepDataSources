using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using DataManagementModels.DriversConfigurations;
using DataManagementModels.Editor;
using System.Text.RegularExpressions;
using Supabase.Storage;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace SupabaseDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.WEBAPI, DatasourceType = DataSourceType.Supabase)]
    public class SupabaseDataSource : IDataSource
    {
        private bool disposedValue;
        public string CurrentDatabase { get { return Dataconnection.ConnectionProp.Database; } set { Dataconnection.ConnectionProp.Database = value; } }
        public string ColumnDelimiter { get  ; set  ; }
        public string ParameterDelimiter { get  ; set  ; }
        public string GuidID { get  ; set  ; }
        public DataSourceType DatasourceType { get  ; set  ; }= DataSourceType.Supabase;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.WEBAPI;
        public IDataConnection Dataconnection { get  ; set  ; }
        public string DatasourceName { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public string Id { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public List<string> EntitiesNames { get  ; set  ; }=new List<string>();
        public List<EntityStructure> Entities { get  ; set  ; }=new List<EntityStructure>();    
        public IDMEEditor DMEEditor { get  ; set  ; }
        public ConnectionState ConnectionStatus { get  ; set  ; }

        public event EventHandler<PassedArgs> PassEvent;

        string _connectionString;
        Supabase.Client client;
        Supabase.SupabaseOptions options;
        public List<Bucket> Buckets { get; set; }=new List<Bucket>();
        public SupabaseDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.WEBAPI;

            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject

            };
            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
                GuidID = Dataconnection.ConnectionProp.GuidID;
            }
            else
            {
                ConnectionDriversConfig driversConfig = DMEEditor.ConfigEditor.DataDriversClasses.FirstOrDefault(p => p.DatasourceType == databasetype);
                Dataconnection.ConnectionProp = new ConnectionProperties
                {
                    ConnectionName = datasourcename,
                    ConnectionString = driversConfig.ConnectionString,
                    DriverName = driversConfig.PackageName,
                    DriverVersion = driversConfig.version,
                    DatabaseType = DataSourceType.Supabase,
                    Category = DatasourceCategory.WEBAPI
                };
                GuidID = Guid.NewGuid().ToString();
            }

            Dataconnection.ConnectionProp.Category = DatasourceCategory.WEBAPI;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.Supabase;
            _connectionString = Dataconnection.ConnectionProp.ConnectionString;
            CurrentDatabase = Dataconnection.ConnectionProp.Database;
           // Settings = new MongoClientSettings();
            //if (CurrentDatabase != null)
            //{
            //    if (CurrentDatabase.Length > 0)
            //    {
            //        _client = new MongoClient(_connectionString);
            //        GetEntitesList();
            //    }
            //}

        }


        #region "Data Manipulation"
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo Commit(PassedArgs args)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }
    

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
        #endregion "Data Manipulation"
        #region "Data Definition"
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }
        #endregion "Data Definition"
        #region "Data Retrieval"
        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }

        public Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public double GetScalar(string query)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetScalarAsync(string query)
        {
            throw new NotImplementedException();
        }
        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }
        #endregion "Data Retrieval"
        #region "Connection Management"
        public void HandleConnectionString()
        {
            if (_connectionString.Contains("}"))
            {
                // Create a dictionary to map placeholders to their respective values
                var replacements = new Dictionary<string, string>
        {
            { "{Host}", Dataconnection.ConnectionProp.Host },
            { "{Port}", Dataconnection.ConnectionProp.Port.ToString() },
            { "{Database}", Dataconnection.ConnectionProp.Database }
        };

                // Optionally add Username and Password to the replacements dictionary
                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.UserID) ||
                    !string.IsNullOrEmpty(Dataconnection.ConnectionProp.Password))
                {
                    replacements.Add("{Username}", Dataconnection.ConnectionProp.UserID);
                    replacements.Add("{Password}", Dataconnection.ConnectionProp.Password);
                }

                // Use a regular expression to replace placeholders, ignoring case
                foreach (var replacement in replacements)
                {
                    if (!string.IsNullOrEmpty(replacement.Value))
                    {
                        _connectionString = Regex.Replace(_connectionString, Regex.Escape(replacement.Key), replacement.Value, RegexOptions.IgnoreCase);
                    }
                }

                // Remove any remaining username and password placeholders if they were not replaced
                _connectionString = Regex.Replace(_connectionString, @"\{Username\}:\{Password\}@", string.Empty, RegexOptions.IgnoreCase);
                _connectionString = Regex.Replace(_connectionString, @"\{Username\}:\{Password\}", string.Empty, RegexOptions.IgnoreCase);
            }

            // get database name from connection string if CurrentDatabase is not set
            if (string.IsNullOrEmpty(CurrentDatabase))
            {
                var match = Regex.Match(_connectionString, @"\/(?<database>[^\/\?]+)(\?|$)");
                if (match.Success)
                {
                    CurrentDatabase = match.Groups["database"].Value;
                }
            }
        }
        public  ConnectionState Openconnection()
        {
            try
            {
                options = new Supabase.SupabaseOptions
                {
                    AutoConnectRealtime = true
                };
                client = new Supabase.Client(Dataconnection.ConnectionProp.Url, Dataconnection.ConnectionProp.KeyToken, options);
                var r = Task.Run(() => client.InitializeAsync());
                r.Wait();
                ConnectionStatus = ConnectionState.Open;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Error", $"Error in opening connection {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }
           
            return ConnectionStatus;
        }
        public ConnectionState Closeconnection()
        {
            throw new NotImplementedException();
        }
        #endregion "Connection Management"
        #region "Supporting Methods From Supabase"
        public void GetBuckets()
        {
            throw new NotImplementedException();
        }
        public void GetBucket(string bucketname)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFiles(string bucketname)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4, string path4)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4, string path4, string filename5)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4, string path4, string filename5, string path5)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4, string path4, string filename5, string path5, string filename6)
        {
            throw new NotImplementedException();
        }
        public void GetBucketFile(string bucketname, string filename, string path, string filename2, string path2, string filename3, string path3, string filename4, string path4, string filename5, string path5, string filename6, string path6)
        {
            throw new NotImplementedException();
        }

        public List<string> GetTablesFromSupabase()
        {
            List<string> tables = new List<string>();
           // var db = client.(CurrentDatabase);
            return tables;


        }
        #endregion "Supporting Methods From Supabase"
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SupabaseDataSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
