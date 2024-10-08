﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using System.Data;
using DataManagmentEngineShared.WebAPI;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.DataManagment_Engine.WebAPI
{
    public class WebAPIDataSource : IDataSource
    {
        public event EventHandler<PassedArgs> PassEvent;
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public List<object> Records { get; set; }
        public ConnectionState ConnectionStatus { get; set; }
        public DataTable SourceEntityData { get; set; }
       // IHttpClientFactory ClientFactory;
        public HttpClient client { get; set; } = new HttpClient();
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        WebAPIDataConnection cn;
        // WebAPIReader Reader;
        public WebAPIDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
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
            client = new HttpClient();
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            cn =(WebAPIDataConnection) Dataconnection;
          //  Reader = new WebAPIReader(Dataconnection.ConnectionProp.FileName, DMEEditor, cn, null);
            cn.OpenConnection();
            cn.ConnectionStatus = ConnectionStatus;
           // GetEntitesList();
        }
        //private bool GetData()
        //{
        //    try
        //    {
        //        if (GetHttpClient())
        //        {
        //            Task<string> retval = GetResponseAsync();
                    
        //            SourceEntityData =(DataTable)  DMEEditor.ConfigEditor.JsonLoader.DeserializeObjectString<DataTable>(retval.ToString());

        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {

        //        return false;
        //    }
           
        //}
        //private bool GetHttpClient()
        //{
        //    try
        //    {
        //        _client = ClientFactory.CreateClient(DatasourceName);
        //      //  _client.DefaultRequestHeaders. = Dataconnection.ConnectionProp.KeyToken;
        //        _client.BaseAddress = new Uri(cn.ConnectionProp.Url);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {

        //        return false;
        //    }

        //}
        //private async Task<string> GetResponseAsync()
        //{
        //    try
        //    {
        //        string retstring = await _client.GetFromJsonAsync<string>("");
        //        return retstring;
        //    }
        //    catch (Exception ex)
        //    {

        //        return "Error";
        //    }
        //}
        public virtual bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public virtual bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public virtual List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public virtual DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            throw new NotImplementedException();
        }

        public virtual  IDataReader GetDataReader(string querystring)
        {
            throw new NotImplementedException();
        }

        public virtual List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;

            try

            {

                return Dataconnection.ConnectionProp.Entities.Select(x => x.EntityName).ToList();

            //    Logger.WriteLog("Successfully Retrieve Entites list ");

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Retrieve Entites list {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            return EntitiesNames;
        }
       
        
        public virtual async Task<object> GetEntityDataAsync(string entityname, string filterstr)
        {
        var request = new HttpRequestMessage();
        request.Method = HttpMethod.Get;
        request.RequestUri = new Uri(Dataconnection.ConnectionProp.Url + "/" + filterstr);
        foreach (WebApiHeader item in Dataconnection.ConnectionProp.Headers)
        {
            request.Headers.Add(item.headername, item.headervalue);
        }

        //string retval = SendAsync(request).Result;

        using (var response = await client.SendAsync(request))
            {
            //    response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
          
                dynamic x= DMEEditor.ConfigEditor.JsonLoader.DeserializeObjectString<dynamic>(body);
                return x;
            }

            
        }

        public virtual DataTable GetEntity(string EntityName, string filterstr)
        {
            throw new NotImplementedException();
        }

      
        public virtual  DataTable RunQuery( string qrystr)
        {

            throw new NotImplementedException();

        }

        public virtual List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public virtual EntityStructure GetEntityStructure(string EntityName,bool refresh=false )
        {
            throw new NotImplementedException();
        }

        public virtual DataTable GetEntityDataTable(string EntityName, string filterstr)
        {
            throw new NotImplementedException();
        }

        public virtual Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        {
            throw new NotImplementedException();
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {


            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            throw new NotImplementedException();
        }

        public virtual EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }
     

        object IDataSource.RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        List<ETLScriptDet> IDataSource.GetCreateEntityScript(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

    

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }

        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            throw new NotImplementedException();
        }

        public ConnectionState Openconnection()
        {
            throw new NotImplementedException();
        }

        public ConnectionState Closeconnection()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
    }
}