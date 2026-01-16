using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.IO;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

using TheTechIdea.Beep.WebAPI;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using TheTechIdea.Beep.DriversConfigurations;

namespace OPCUADataSource
{
    [AddinAttribute(Category = DatasourceCategory.STREAM, DatasourceType = DataSourceType.OPC)]
    public class OPCDataSource : IDataSource, IDisposable
    {
        private Session session;
        private ApplicationInstance application;
        private bool disposedValue;

        public OPCDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.STREAM;

            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject
            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            if (Dataconnection.ConnectionProp == null)
            {
                ConnectionDriversConfig driversConfig = DMEEditor.ConfigEditor.DataDriversClasses.FirstOrDefault(p => p.DatasourceType == databasetype);
                Dataconnection.ConnectionProp = new ConnectionProperties
                {
                    ConnectionName = datasourcename,
                    ConnectionString = driversConfig?.ConnectionString ?? "",
                    DriverName = driversConfig?.PackageName ?? "",
                    DriverVersion = driversConfig?.version ?? "",
                    DatabaseType = DataSourceType.OPC,
                    Category = DatasourceCategory.STREAM
                };
            }

            EntitiesNames = new List<string>();
            Entities = new List<EntityStructure>();
            GuidID = Guid.NewGuid().ToString();
            ConnectionStatus = ConnectionState.Closed;
        }

        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get  ; set  ; }
        public DatasourceCategory Category { get  ; set  ; }
        public IDataConnection Dataconnection { get  ; set  ; }
        public string DatasourceName { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public string Id { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public List<string> EntitiesNames { get  ; set  ; }
        public List<EntityStructure> Entities { get  ; set  ; }
        public IDMEEditor DMEEditor { get  ; set  ; }
        public ConnectionState ConnectionStatus { get  ; set  ; }
        public string ColumnDelimiter { get  ; set  ; }
        public string ParameterDelimiter { get  ; set  ; }
        public string CurrentDatabase { get; set; }

        public event EventHandler<PassedArgs> PassEvent;
        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }
        public virtual double GetScalar(string query)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Assuming you have a database connection and command objects.

                //using (var command = GetDataCommand())
                //{
                //    command.CommandText = query;
                //    var result = command.ExecuteScalar();

                //    // Check if the result is not null and can be converted to a double.
                //    if (result != null && double.TryParse(result.ToString(), out double value))
                //    {
                //        return value;
                //    }
                //}


                // If the query executed successfully but didn't return a valid double, you can handle it here.
                // You might want to log an error or throw an exception as needed.
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }

            // Return a default value or throw an exception if the query failed.
            return 0.0; // You can change this default value as needed.
        }

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            // OPC UA doesn't support traditional transactions
            return ErrorObject;
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                if (EntitiesNames != null && EntitiesNames.Count > 0)
                {
                    return EntitiesNames.Any(e => e.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    GetEntitesList();
                    return EntitiesNames.Any(e => e.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error checking entity existence: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (session != null)
                {
                    session.Close();
                    session.Dispose();
                    session = null;
                }
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor?.AddLogMessage("Beep", "OPC UA connection closed successfully", DateTime.Now, -1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                DMEEditor?.AddLogMessage("Beep", $"Error closing OPC UA connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ConnectionStatus;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            // OPC UA doesn't support traditional transactions
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // OPC UA nodes are typically defined in the server, not created via client
                foreach (var entity in entities)
                {
                    if (!Entities.Any(p => p.EntityName == entity.EntityName))
                    {
                        Entities.Add(entity);
                    }
                    if (!EntitiesNames.Contains(entity.EntityName))
                    {
                        EntitiesNames.Add(entity.EntityName);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                // OPC UA nodes are typically defined in the server, not created via client
                if (!Entities.Any(p => p.EntityName == entity.EntityName))
                {
                    Entities.Add(entity);
                }
                if (!EntitiesNames.Contains(entity.EntityName))
                {
                    EntitiesNames.Add(entity.EntityName);
                }
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in CreateEntityAs: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data deleted successfully." };
            try
            {
                // OPC UA nodes are typically defined in the server, not deleted via client
                retval.Flag = Errors.Failed;
                retval.Message = "Delete operation not typically supported for OPC UA nodes via client";
                DMEEditor?.AddLogMessage("Beep", "Delete operation not typically supported for OPC UA nodes", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error deleting entity: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error deleting entity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        Closeconnection();
                    }
                    catch (Exception ex)
                    {
                        DMEEditor?.AddLogMessage("Beep", $"Error disposing OPC UA connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                disposedValue = true;
            }
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            // OPC UA doesn't support traditional transactions
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // OPC UA doesn't support SQL
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "SQL execution not supported for OPC UA";
                DMEEditor?.AddLogMessage("Beep", "SQL execution not supported for OPC UA", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            // OPC UA doesn't have traditional child tables
            return new List<ChildRelation>();
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            List<ETLScriptDet> scripts = new List<ETLScriptDet>();
            try
            {
                var entitiesToScript = entities ?? Entities;
                if (entitiesToScript != null && entitiesToScript.Count > 0)
                {
                    foreach (var entity in entitiesToScript)
                    {
                        var script = new ETLScriptDet
                        {
                            SourceEntity = entity,
                           ScriptType= DDLScriptType.CreateEntity
                           
                        };
                        scripts.Add(script);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetCreateEntityScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return scripts;
        }

        public IEnumerable<string> GetEntitesList()
        {
            try
            {
                if (session == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (session != null && ConnectionStatus == ConnectionState.Open)
                {
                    EntitiesNames.Clear();
                    Entities.Clear();

                    // Browse the root node to get all entities
                    BrowseRequest browseRequest = new BrowseRequest
                    {
                        NodesToBrowse = new BrowseDescriptionCollection
                        {
                            new BrowseDescription
                            {
                                NodeId = ObjectIds.ObjectsFolder,
                                BrowseDirection = BrowseDirection.Forward,
                                NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable),
                                ResultMask = (uint)BrowseResultMask.All
                            }
                        }
                    };

                    BrowseResultCollection browseResults;
                    DiagnosticInfoCollection browseDiagnosticInfos;
                    session.Browse(null, null, 0u, browseRequest.NodesToBrowse, out browseResults, out browseDiagnosticInfos);

                    if (browseResults != null && browseResults.Count > 0)
                    {
                        foreach (var result in browseResults)
                        {
                            if (result.References != null)
                            {
                                foreach (var reference in result.References)
                                {
                                    string nodeName = reference.DisplayName?.Text ?? reference.BrowseName?.Name ?? "";
                                    if (!string.IsNullOrEmpty(nodeName) && !EntitiesNames.Contains(nodeName))
                                    {
                                        EntitiesNames.Add(nodeName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntitesList: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return EntitiesNames ?? new List<string>();
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            List<object> results = new List<object>();
            try
            {
                if (session == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (session != null && ConnectionStatus == ConnectionState.Open)
                {
                    // Find node by display name
                    BrowseRequest browseRequest = new BrowseRequest
                    {
                        NodesToBrowse = new BrowseDescriptionCollection
                        {
                            new BrowseDescription
                            {
                                NodeId = ObjectIds.ObjectsFolder,
                                BrowseDirection = BrowseDirection.Forward,
                                NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable),
                                ResultMask = (uint)BrowseResultMask.All,
                                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences
                            }
                        }
                    };

                    BrowseResultCollection browseResults;
                    DiagnosticInfoCollection browseDiagnosticInfos;
                    session.Browse(null, null, 0u, browseRequest.NodesToBrowse, out browseResults, out browseDiagnosticInfos);

                    if (browseResults != null && browseResults.Count > 0)
                    {
                        foreach (var result in browseResults)
                        {
                            if (result.References != null)
                            {
                                foreach (var reference in result.References)
                                {
                                    string nodeName = reference.DisplayName?.Text ?? reference.BrowseName?.Name ?? "";
                                    if (nodeName.Equals(EntityName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Read the node value
                                        ReadValueIdCollection nodesToRead = new ReadValueIdCollection
                                        {
                                            new ReadValueId
                                            {
                                                NodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris),
                                                AttributeId = Attributes.Value
                                            }
                                        };

                                        DataValueCollection values;
                                        DiagnosticInfoCollection diagnosticInfos;
                                        session.Read(null, 0, TimestampsToReturn.Both, nodesToRead, out values, out diagnosticInfos);

                                        if (values != null && values.Count > 0)
                                        {
                                            var value = values[0];
                                            Dictionary<string, object> nodeData = new Dictionary<string, object>
                                            {
                                                { "NodeId", reference.NodeId.ToString() },
                                                { "DisplayName", nodeName },
                                                { "Value", value.Value },
                                                { "StatusCode", value.StatusCode.ToString() },
                                                { "SourceTimestamp", value.SourceTimestamp },
                                                { "ServerTimestamp", value.ServerTimestamp }
                                            };

                                            // Apply filters if provided
                                            bool matches = true;
                                            if (filter != null && filter.Count > 0)
                                            {
                                                foreach (var f in filter)
                                                {
                                                    if (nodeData.ContainsKey(f.FieldName))
                                                    {
                                                        bool fieldMatches = EvaluateFilter(nodeData[f.FieldName], f.Operator, f.FilterValue);
                                                        if (!fieldMatches)
                                                        {
                                                            matches = false;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }

                                            if (matches)
                                            {
                                                results.Add(nodeData);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult pagedResult = new PagedResult();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                var allResults = GetEntity(EntityName, filter).ToList();
                int totalRecords = allResults.Count;
                int offset = (pageNumber - 1) * pageSize;
                var pagedResults = allResults.Skip(offset).Take(pageSize).ToList();

                pagedResult.Data = pagedResults;
                pagedResult.TotalRecords = totalRecords;
                pagedResult.PageNumber = pageNumber;
                pagedResult.PageSize = pageSize;
                pagedResult.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
                pagedResult.HasNextPage = pageNumber < pagedResult.TotalPages;
                pagedResult.HasPreviousPage = pageNumber > 1;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntity with pagination: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return pagedResult;
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // OPC UA doesn't have traditional foreign keys
            return new List<RelationShipKeys>();
        }

        public int GetEntityIdx(string entityName)
        {
            try
            {
                if (Entities != null && Entities.Count > 0)
                {
                    return Entities.FindIndex(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                }
                return -1;
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityIdx: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return -1;
            }
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                if (!refresh && Entities != null && Entities.Count > 0)
                {
                    var entity = Entities.FirstOrDefault(e => e.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                    if (entity != null)
                    {
                        return entity;
                    }
                }

                if (session == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (session != null && ConnectionStatus == ConnectionState.Open)
                {
                    EntityStructure entityStructure = new EntityStructure
                    {
                        EntityName = EntityName,
                        Fields = new List<EntityField>()
                    };

                    // Find the node and get its attributes
                    BrowseRequest browseRequest = new BrowseRequest
                    {
                        NodesToBrowse = new BrowseDescriptionCollection
                        {
                            new BrowseDescription
                            {
                                NodeId = ObjectIds.ObjectsFolder,
                                BrowseDirection = BrowseDirection.Forward,
                                NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable),
                                ResultMask = (uint)BrowseResultMask.All
                            }
                        }
                    };

                    BrowseResultCollection browseResults;
                    DiagnosticInfoCollection browseDiagnosticInfos;
                    session.Browse(null, null, 0u, browseRequest.NodesToBrowse, out browseResults, out browseDiagnosticInfos);

                    if (browseResults != null && browseResults.Count > 0)
                    {
                        foreach (var result in browseResults)
                        {
                            if (result.References != null)
                            {
                                foreach (var reference in result.References)
                                {
                                    string nodeName = reference.DisplayName?.Text ?? reference.BrowseName?.Name ?? "";
                                    if (nodeName.Equals(EntityName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Read node attributes
                                        ReadValueIdCollection nodesToRead = new ReadValueIdCollection
                                        {
                                            new ReadValueId { NodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris), AttributeId = Attributes.DataType },
                                            new ReadValueId { NodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris), AttributeId = Attributes.ValueRank },
                                            new ReadValueId { NodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris), AttributeId = Attributes.Value }
                                        };

                                        DataValueCollection values;
                                        DiagnosticInfoCollection diagnosticInfos;
                                        session.Read(null, 0, TimestampsToReturn.Both, nodesToRead, out values, out diagnosticInfos);

                                        if (values != null && values.Count > 0)
                                        {
                                            entityStructure.Fields.Add(new EntityField
                                            {
                                                FieldName = "Value",
                                                Fieldtype = GetDotNetType(values[2].Value),
                                                BaseColumnName = "Value"
                                            });

                                            entityStructure.Fields.Add(new EntityField
                                            {
                                                FieldName = "NodeId",
                                                Fieldtype = "System.String",
                                                BaseColumnName = "NodeId"
                                            });

                                            entityStructure.Fields.Add(new EntityField
                                            {
                                                FieldName = "DisplayName",
                                                Fieldtype = "System.String",
                                                BaseColumnName = "DisplayName"
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!Entities.Any(e => e.EntityName == EntityName))
                    {
                        Entities.Add(entityStructure);
                    }

                    return entityStructure;
                }
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityStructure: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return new EntityStructure { EntityName = EntityName, Fields = new List<EntityField>() };
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd.EntityName, refresh);
        }

        public Type GetEntityType(string EntityName)
        {
            try
            {
                var entityStructure = GetEntityStructure(EntityName, false);
                if (entityStructure != null && entityStructure.Fields != null && entityStructure.Fields.Count > 0)
                {
                        if (DMEEditor != null)
                        {
                            string code = DMTypeBuilder.ConvertPOCOClassToEntity(DMEEditor, entityStructure, "OPCUAGeneratedTypes");
                            return DMEEditor.classCreator.CreateTypeFromCode(code, EntityName);
                        }
                    }
                    return typeof(object);
                }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in GetEntityType: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return typeof(object);
            }
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data inserted successfully." };
            try
            {
                if (session == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (session != null && ConnectionStatus == ConnectionState.Open)
                {
                    // OPC UA write operation
                    // Find the node and write value
                    // This is a simplified version - in practice you'd need to find the correct node
                    retval.Flag = Errors.Failed;
                    retval.Message = "Insert operation requires specific node implementation";
                    DMEEditor?.AddLogMessage("Beep", "Insert operation requires specific node implementation", DateTime.Now, -1, null, Errors.Failed);
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Connection is not open";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error inserting entity: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error inserting entity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public ConnectionState Openconnection()
        {
            try
            {
                string endpointUrl = Dataconnection.ConnectionProp?.Url ?? "opc.tcp://localhost:4840";
                string username = Dataconnection.ConnectionProp?.UserID ?? "";
                string password = Dataconnection.ConnectionProp?.Password ?? "";

                // Create application configuration (optional config file)
                ApplicationConfiguration configuration = null;
                var configFilePath = Path.Combine(AppContext.BaseDirectory, "OPCUA_Client_Config.xml");
                if (File.Exists(configFilePath))
                {
                    configuration = ApplicationConfiguration.Load(configFilePath, ApplicationType.Client).Result;
                }

                if (configuration == null)
                {
                    configuration = new ApplicationConfiguration
                    {
                        ApplicationName = "OPC UA Client",
                        ApplicationUri = Utils.Format(@"urn:{0}:OPCUA:Client", System.Net.Dns.GetHostName()),
                        ApplicationType = ApplicationType.Client,
                        SecurityConfiguration = new SecurityConfiguration
                        {
                            ApplicationCertificate = new CertificateIdentifier { StoreType = "Directory", StorePath = "OPC Foundation/CertificateStores/MachineDefault", SubjectName = "OPC UA Client" },
                            TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "OPC Foundation/CertificateStores/UA Certificate Authorities" },
                            TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = "OPC Foundation/CertificateStores/UA Applications" },
                            RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = "OPC Foundation/CertificateStores/RejectedCertificates" },
                            AutoAcceptUntrustedCertificates = true,
                            AddAppCertToTrustedStore = true
                        },
                        TransportConfigurations = new TransportConfigurationCollection(),
                        TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                        ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                        TraceConfiguration = new TraceConfiguration()
                    };

                    configuration.Validate(ApplicationType.Client).Wait();
                }

                // Create application instance
                if (application == null)
                {
                    application = new ApplicationInstance
                    {
                        ApplicationName = "OPC UA Client",
                        ApplicationType = ApplicationType.Client,
                        ApplicationConfiguration = configuration
                    };
                }

                // Discover endpoints
                EndpointDescription selectedEndpoint = CoreClientUtils.SelectEndpoint(configuration, endpointUrl, false);
                EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(configuration);
                ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                // Create session
                UserIdentity userIdentity = string.IsNullOrEmpty(username) 
                    ? new UserIdentity(new AnonymousIdentityToken()) 
                    : new UserIdentity(username, Encoding.UTF8.GetBytes(password));

                session = Session.Create(configuration, endpoint, false, "OPC UA Client", 60000, userIdentity, null).Result;

                if (session != null && session.Connected)
                {
                    ConnectionStatus = ConnectionState.Open;
                    DMEEditor?.AddLogMessage("Beep", "OPC UA connection opened successfully", DateTime.Now, -1, null, Errors.Ok);
                    GetEntitesList();
                }
                else
                {
                    ConnectionStatus = ConnectionState.Closed;
                    DMEEditor?.AddLogMessage("Beep", "Failed to open OPC UA connection", DateTime.Now, -1, null, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor?.AddLogMessage("Beep", $"Failed to open OPC UA connection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return ConnectionStatus;
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            List<object> results = new List<object>();
            try
            {
                // OPC UA doesn't support SQL queries
                // Could implement OPC UA query service if needed
                DMEEditor?.AddLogMessage("Beep", "Query execution not fully supported for OPC UA", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error in RunQuery: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return results;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // OPC UA doesn't support scripts
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Script execution not supported for OPC UA";
                DMEEditor?.AddLogMessage("Beep", "Script execution not supported for OPC UA", DateTime.Now, -1, null, Errors.Failed);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data updated successfully." };
            try
            {
                if (UploadData is IEnumerable<object> dataList)
                {
                    int count = 0;
                    foreach (var item in dataList)
                    {
                        UpdateEntity(EntityName, item);
                        count++;
                        progress?.Report(new PassedArgs { Messege = $"Updated {count} records" });
                    }
                    retval.Message = $"Updated {count} records successfully.";
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "UploadData must be an IEnumerable<object>.";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error in UpdateEntities: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error in UpdateEntities: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data updated successfully." };
            try
            {
                if (session == null || ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                if (session != null && ConnectionStatus == ConnectionState.Open)
                {
                    // OPC UA write operation
                    // Find the node and write value
                    // This is a simplified version - in practice you'd need to find the correct node and write to it
                    retval.Flag = Errors.Failed;
                    retval.Message = "Update operation requires specific node implementation";
                    DMEEditor?.AddLogMessage("Beep", "Update operation requires specific node implementation", DateTime.Now, -1, null, Errors.Failed);
                }
                else
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Connection is not open";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error updating entity: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"Error updating entity: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return retval;
        }

        #region "Helper Methods"
        private string GetDotNetType(object value)
        {
            if (value == null) return "System.Object";
            Type type = value.GetType();
            if (type == typeof(bool)) return "System.Boolean";
            if (type == typeof(byte)) return "System.Byte";
            if (type == typeof(sbyte)) return "System.SByte";
            if (type == typeof(short)) return "System.Int16";
            if (type == typeof(ushort)) return "System.UInt16";
            if (type == typeof(int)) return "System.Int32";
            if (type == typeof(uint)) return "System.UInt32";
            if (type == typeof(long)) return "System.Int64";
            if (type == typeof(ulong)) return "System.UInt64";
            if (type == typeof(float)) return "System.Single";
            if (type == typeof(double)) return "System.Double";
            if (type == typeof(decimal)) return "System.Decimal";
            if (type == typeof(string)) return "System.String";
            if (type == typeof(DateTime)) return "System.DateTime";
            return "System.Object";
        }

        private bool EvaluateFilter(object value, string op, string filterValue)
        {
            try
            {
                if (value == null) return false;

                switch (op)
                {
                    case "==":
                    case "=":
                        return value.ToString().Equals(filterValue, StringComparison.OrdinalIgnoreCase);
                    case "!=":
                    case "<>":
                        return !value.ToString().Equals(filterValue, StringComparison.OrdinalIgnoreCase);
                    case ">":
                        if (double.TryParse(value.ToString(), out double val1) && double.TryParse(filterValue, out double val2))
                            return val1 > val2;
                        return string.Compare(value.ToString(), filterValue, StringComparison.OrdinalIgnoreCase) > 0;
                    case "<":
                        if (double.TryParse(value.ToString(), out double val3) && double.TryParse(filterValue, out double val4))
                            return val3 < val4;
                        return string.Compare(value.ToString(), filterValue, StringComparison.OrdinalIgnoreCase) < 0;
                    case ">=":
                        if (double.TryParse(value.ToString(), out double val5) && double.TryParse(filterValue, out double val6))
                            return val5 >= val6;
                        return string.Compare(value.ToString(), filterValue, StringComparison.OrdinalIgnoreCase) >= 0;
                    case "<=":
                        if (double.TryParse(value.ToString(), out double val7) && double.TryParse(filterValue, out double val8))
                            return val7 <= val8;
                        return string.Compare(value.ToString(), filterValue, StringComparison.OrdinalIgnoreCase) <= 0;
                    default:
                        return value.ToString().Contains(filterValue, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
