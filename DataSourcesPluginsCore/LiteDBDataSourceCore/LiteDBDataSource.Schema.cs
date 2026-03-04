using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using LiteDB;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;
using DataManagementModels.Editor;

namespace LiteDBDataSourceCore
{
    public partial class LiteDBDataSource
    {
        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            // LiteDB doesn't have child tables
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
                            EntityName = entity.EntityName,
                            ScriptType = "CREATE",
                            ScriptText = $"# LiteDB collection: {entity.EntityName}\n# Collections are created automatically when first document is inserted"
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
            EntitiesNames = new List<string>();
            try
            {
                if (!EnsureConnectionReady(nameof(GetEntitesList)))
                {
                    return EntitiesNames;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    var collectionNames = session.GetCollectionNames().ToList();
                    foreach (var item in collectionNames)
                    {
                        EntitiesNames.Add(item);
                    }
                }
                if (Entities == null)
                {
                    Entities = new List<EntityStructure>();
                }

                if (Entities != null)
                {
                    var entitiesToRemove = Entities.Where(e => !EntitiesNames.Contains(e.EntityName) && !string.IsNullOrEmpty(e.CustomBuildQuery)).ToList();
                    foreach (var item in entitiesToRemove)
                    {
                        Entities.Remove(item);
                    }
                    var entitiesToAdd = EntitiesNames.Where(e => !Entities.Any(x => x.EntityName == e)).ToList();
                    foreach (var item in entitiesToAdd)
                    {
                        Entities.Add(GetEntityStructure(item, true));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {nameof(GetEntitesList)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return EntitiesNames;
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            // LiteDB doesn't have foreign keys
            return new List<RelationShipKeys>();
        }

        public int GetEntityIdx(string entityName)
        {
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return -1;
            }
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            EntityStructure result = new EntityStructure();

            try
            {
                if (!refresh && Entities != null && Entities.Count > 0)
                {
                    result = Entities.Find(c => c.EntityName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
                    if (result != null)
                    {
                        return result;
                    }
                }

                if (!EnsureConnectionReady(nameof(GetEntityStructure)))
                {
                    return null;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    var collection = session.GetCollection<BsonDocument>(EntityName);
                    var list = collection.FindAll().ToList();
                    if (list.Count > 0)
                    {
                        DataStruct = CompileSchemaFromDocuments(list, EntityName);
                        ObjectsCreated = true;
                        enttype = GetEntityType(EntityName);
                    }
                    else
                    {
                        DataStruct = null;
                    }
                }

                if (DataStruct != null)
                {
                    SyncEntityCaches(EntityName, DataStruct);
                }

                result = DataStruct;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {nameof(GetEntityStructure)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }

            return result;
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            EntityStructure result = fnd;
            string EntityName = fnd.EntityName;
            try
            {
                if (refresh == false && Entities.Count > 0)
                {
                    result = Entities.Find(c => c.EntityName.Equals(EntityName, StringComparison.CurrentCultureIgnoreCase));
                    if (result != null)
                    {
                        return result;
                    }
                }
                if (!EnsureConnectionReady(nameof(GetEntityStructure)))
                {
                    return null;
                }
                result = GetEntityStructure(EntityName, refresh);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                result = null;
                DMEEditor.AddLogMessage("Beep", $"error in {nameof(GetEntityStructure)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
        }

        public Type GetEntityType(string EntityName)
        {
            Type result = null;
            //         SetObjects(EntityName);
            try
            {
                if (!EnsureConnectionReady(nameof(GetEntityType)))
                {
                    return null;
                }

                if (EntityName == lastentityname && enttype != null)
                {
                    result = enttype;
                }
                if (result == null)
                {
                    if (DataStruct == null)
                    {
                        lastentityname = EntityName;
                        DataStruct = GetEntityStructure(EntityName, false);
                    }

                    if (DataStruct?.Fields == null || DataStruct.Fields.Count == 0)
                    {
                        return null;
                    }

                    DMTypeBuilder.CreateNewObject(DMEEditor, "Beep." + DatasourceName, EntityName, DataStruct.Fields);
                    result = DMTypeBuilder.myType;
                    if (result != null)
                    {
                        enttype = result;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                result = null;
                DMEEditor.AddLogMessage("Beep", $"error in {nameof(GetEntityType)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
        }

        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;

            try
            {
                if (!EnsureConnectionReady(nameof(CheckEntityExist)))
                {
                    return false;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    var collection = session.GetCollection<BsonDocument>(EntityName);
                    long count = collection.Count();

                    if (count > 0)
                    {
                        retval = true;
                    }
                    else
                    {
                        retval = false;
                        DMEEditor.AddLogMessage("Beep", "Collection does not exist.", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
            }
            catch (Exception ex)
            {
                string methodName = nameof(CheckEntityExist);
                retval = false;
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Error checking existence of the entity {EntityName}: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"Error in {methodName} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (dDLScripts != null && !string.IsNullOrEmpty(dDLScripts.ScriptText))
                {
                    // Execute script as SQL command
                    ExecuteSql(dDLScripts.ScriptText);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in RunScript: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "All entities processed successfully." };
            try
            {
                if (!EnsureConnectionReady(nameof(CreateEntities)))
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                    return retval;
                }
                if (Entities == null)
                {
                    Entities = new List<EntityStructure>();
                }

                if (EntitiesNames == null)
                {
                    EntitiesNames = new List<string>();
                }
                if (entities == null || entities.Count == 0)
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "No entities supplied.";
                    return retval;
                }

                foreach (var item in entities)
                {
                    if (!CreateEntityAs(item))
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = $"Failed to create one or more entities. Last entity: {item?.EntityName}";
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error creating entities: {ex.Message}";
            }

            return retval;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            ErrorsInfo retval = new ErrorsInfo();
            retval.Flag = Errors.Ok;
            retval.Message = "Executed Successfully ";
            bool success = false;
            try
            {
                if (!EnsureConnectionReady(nameof(CreateEntityAs)))
                {
                    return false;
                }

                if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName))
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Entity structure is null or missing EntityName.";
                    return false;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    if (session.CollectionExists(entity.EntityName))
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "Collection already exists.";
                        DMEEditor?.AddLogMessage("Beep", "Collection already exists.", DateTime.Now, -1, null, Errors.Failed);
                    }
                    else
                    {
                        var collection = session.GetCollection<BsonDocument>(entity.EntityName);
                        collection.EnsureIndex("_id", true);

                        if (entity.Fields != null)
                        {
                            foreach (var field in entity.Fields)
                            {
                                if (field != null && !string.IsNullOrWhiteSpace(field.fieldname) && field.IsKey && !field.fieldname.Equals("_id", StringComparison.OrdinalIgnoreCase))
                                {
                                    collection.EnsureIndex(field.fieldname);
                                }
                            }
                        }

                        SyncEntityCaches(entity.EntityName, entity);
                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                DMEEditor.AddLogMessage("Beep", $"error in {nameof(CreateEntityAs)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return success;
        }
    }
}
