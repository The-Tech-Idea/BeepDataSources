﻿using System;
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

namespace RealMDataSource
{
    public class RealMDataSource : IDataSource
    {
        public RealMDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;
           
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();

        }




        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }
        public List<EntityStructure> Entities { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; }
        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }

        public event EventHandler<PassedArgs> PassEvent;
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public ConnectionState Closeconnection()
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

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
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

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }

        public ConnectionState Openconnection()
        {
            throw new NotImplementedException();
        }

        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
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
    }
}