using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using LiteDB;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using DataManagementModels.Editor;

namespace LiteDBDataSourceCore
{
    public partial class LiteDBDataSource
    {
        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Entity deleted successfully." };
            try
            {
                if (!EnsureConnectionReady(nameof(DeleteEntity)))
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                    return retval;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    var collection = session.GetCollection<BsonDocument>(EntityName);
                    BsonValue idValue = GetIdentifierValue(UploadDataRow);
                    var result = collection.Delete(idValue);
                    if (!result)
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "No document found with the specified identifier.";
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error deleting entity: {ex.Message}";
                DMEEditor?.AddLogMessage("Beep", $"error in {nameof(DeleteEntity)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Batch update initiated." };
            int count = 0;
            int successCount = 0;
            IEnumerable<object> items;
            try
            {
                if (!EnsureConnectionReady(nameof(UpdateEntities)))
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                    return retval;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    var collection = session.GetCollection<BsonDocument>(EntityName);
                    items = UploadData as IEnumerable<object>;

                    if (items == null)
                    {
                        DMEEditor.AddLogMessage("Beep", $"UploadData must be an IEnumerable type.", DateTime.Now, -1, null, Errors.Failed);
                        return new ErrorsInfo { Flag = Errors.Failed, Message = "UploadData must be an IEnumerable type." };
                    }

                    foreach (var item in items)
                    {
                        BsonDocument docToUpdate = ConvertToBsonDocument(item);
                        if (!docToUpdate.ContainsKey("_id"))
                        {
                            DMEEditor.AddLogMessage("Beep", $"Each document must contain an '_id' field for updates.", DateTime.Now, -1, null, Errors.Failed);
                        }
                        else
                        {
                            var id = docToUpdate["_id"];
                            bool result = collection.Update(id, docToUpdate);

                            if (result)
                            {
                                successCount++;
                            }
                        }

                        count++;
                        progress?.Report(new PassedArgs { Messege = $"Updating {count} of {items}", ParameterInt1 = count });
                    }

                    retval.Message = $"{successCount} out of {count} entities updated successfully.";
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error during batch update: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"Error in {nameof(UpdateEntities)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        public ILiteCollection<T> GetLiteCollection<T>(string EntityName)
        {
            ILiteCollection<T> retval = null;
            try
            {
                if (!EnsureConnectionReady(nameof(GetLiteCollection)))
                {
                    return null;
                }
                retval = db.GetCollection<T>(EntityName);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error getting LiteCollection: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Data inserted successfully." };
            EntityStructure entity = null;
            try
            {
                if (!EnsureConnectionReady(nameof(InsertEntity)))
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                    return retval;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    SetObjects(EntityName);

                    var collection = session.GetCollection<BsonDocument>(EntityName);
                    if (collection.Count() == 0)
                    {
                        collection.EnsureIndex("_id", true);
                    }
                    BsonDocument docToInsert = ConvertToBsonDocument(InsertedData);
                    collection.Insert(docToInsert);

                    retval.Flag = Errors.Ok;
                    retval.Message = "Data inserted successfully.";
                }
                if (!Entities.Any(p => p.EntityName == EntityName))
                {
                    entity = GetEntityStructure(EntityName, false);
                }
                SyncEntityCaches(EntityName, entity);
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = "Error inserting data: " + ex.Message;
                DMEEditor.AddLogMessage("Beep", $"error in {nameof(InsertEntity)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Document updated successfully." };
            try
            {
                if (!EnsureConnectionReady(nameof(UpdateEntity)))
                {
                    retval.Flag = Errors.Failed;
                    retval.Message = "Database connection is not open.";
                    return retval;
                }

                using (var session = new LiteDatabase(_connectionString))
                {
                    var collection = session.GetCollection<BsonDocument>(EntityName);
                    BsonDocument docToUpdate = ConvertToBsonDocument(UploadDataRow);

                    if (!docToUpdate.ContainsKey("_id"))
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "Document must contain an '_id' field for updates.";
                        DMEEditor.AddLogMessage("Beep", retval.Message, DateTime.Now, -1, null, Errors.Failed);
                        return retval;
                    }

                    var result = collection.Update(docToUpdate);
                    if (!result)
                    {
                        retval.Flag = Errors.Failed;
                        retval.Message = "No document found with the specified ID.";
                    }
                }
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Error updating document: {ex.Message}";
                DMEEditor.AddLogMessage("Beep", $"error in {nameof(UpdateEntity)} in {DatasourceName} - {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return retval;
        }
    }
}
