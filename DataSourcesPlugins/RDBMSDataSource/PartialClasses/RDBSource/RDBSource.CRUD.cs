using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        /// <summary>
        /// Updates a specific record in the database for the given entity based on provided data.
        /// </summary>
        /// <param name="EntityName">The name of the entity (e.g., table name) in which the record will be updated.</param>
        /// <param name="UploadDataRow">The data row that contains the updated values for the entity.</param>
        /// <returns>IErrorsInfo object containing information about the operation's success or failure.</returns>
        /// <remarks>
        /// This method constructs and executes an SQL update command based on the provided data row. 
        /// It also handles transaction management and logs the operation's outcome.
        /// </remarks>
        /// 
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
            SetObjects(EntityName);
            ErrorObject.Flag = Errors.Ok;

            try
            {
                UpdateFieldSequnce = new List<EntityField>();
                usedParameterNames = new HashSet<string>();
                string updatestring = GetUpdateString(EntityName, DataStruct);
                
                using (var cmd = GetDataCommand())
                {
                    cmd.CommandText = updatestring;
                    CreateUpdateCommandParameters(cmd, UploadDataRow, DataStruct);

                    int rowsUpdated = cmd.ExecuteNonQuery();
                    if (rowsUpdated == 0)
                    {
                        string msg = $"No records updated in {EntityName}";
                        DMEEditor.AddLogMessage("Beep", msg, DateTime.Now, 0, null, Errors.Failed);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, EntityName, "Update");
            }

            return ErrorObject;
        }
        /// <summary>
        /// Deletes a specific record from the database for the given entity.
        /// </summary>
        /// <param name="EntityName">The name of the entity (e.g., table name) from which the record will be deleted.</param>
        /// <param name="DeletedDataRow">The data row that identifies the record to be deleted.</param>
        /// <returns>IErrorsInfo object containing information about the success or failure of the operation.</returns>
        /// <remarks>
        /// This method constructs and executes an SQL delete command. It uses transactions to ensure data integrity and logs the outcome of the operation.
        /// </remarks>
        public virtual IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            SetObjects(EntityName);
            ErrorObject.Flag = Errors.Ok;

            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;

            try
            {
                usedParameterNames = new HashSet<string>();
                string deleteString = GetDeleteString(EntityName, DataStruct);
                
                using (var cmd = GetDataCommand())
                {
                    cmd.CommandText = deleteString;
                    CreateDeleteCommandParameters(cmd, DeletedDataRow, DataStruct);
                    
                    int rowsDeleted = cmd.ExecuteNonQuery();
                    if (rowsDeleted == 0)
                    {
                        string msg = $"No records deleted from {EntityName}";
                        DMEEditor.AddLogMessage("Beep", msg, DateTime.Now, 0, null, Errors.Failed);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, EntityName, "Delete");
            }

            return ErrorObject;
        }
        /// <summary>
        /// Inserts a new record into the database for the specified entity.
        /// </summary>
        /// <param name="EntityName">The name of the entity (e.g., table name) in which the new record will be inserted.</param>
        /// <param name="InsertedData">The data row representing the new record to be inserted.</param>
        /// <returns>IErrorsInfo object with information about the success or failure of the insert operation.</returns>
        /// <remarks>
        /// This method prepares and executes an SQL insert command based on the data provided. It logs the operation's outcome for debugging and error handling purposes.
        /// </remarks>
        public virtual IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            SetObjects(EntityName);
            ErrorObject.Flag = Errors.Ok;
            
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;

            try
            {
                usedParameterNames = new HashSet<string>();
                string insertString = GetInsertString(EntityName, DataStruct);
                
                using (var cmd = GetDataCommand())
                {
                    cmd.CommandText = insertString;
                    CreateCommandParameters(cmd, InsertedData, DataStruct);

                    int rowsInserted = cmd.ExecuteNonQuery();
                    if (rowsInserted > 0)
                    {
                        DMEEditor.ErrorObject.Message = $"Successfully inserted record to {EntityName}";
                        DMEEditor.ErrorObject.Flag = Errors.Ok;
                        
                        // Fetch auto-generated identity if applicable
                        string fetchIdentityQuery = RDBMSHelper.GenerateFetchLastIdentityQuery(DatasourceType);
                        if (fetchIdentityQuery.ToUpper().Contains("SELECT") && DataStruct.PrimaryKeys.Count() > 0)
                        {
                            cmd.CommandText = fetchIdentityQuery;
                            object result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                var primaryKeyProperty = InsertedData.GetType().GetProperty(DataStruct.PrimaryKeys.First().fieldname);
                                if (primaryKeyProperty != null && primaryKeyProperty.CanWrite)
                                {
                                    var primaryKeyType = primaryKeyProperty.PropertyType;
                                    Type underlyingType = Nullable.GetUnderlyingType(primaryKeyType) ?? primaryKeyType;
                                    var convertedIdentity = Convert.ChangeType(result, underlyingType);
                                    primaryKeyProperty.SetValue(InsertedData, convertedIdentity);
                                    DMEEditor.ErrorObject.Message = $"Successfully inserted record to {EntityName} with ID {convertedIdentity}";
                                }
                            }
                            else
                            {
                                DMEEditor.ErrorObject.Message = "Failed to retrieve the identity of the inserted record";
                                DMEEditor.ErrorObject.Flag = Errors.Failed;
                            }
                        }
                    }
                    else
                    {
                        DMEEditor.ErrorObject.Message = $"No records inserted to {EntityName}";
                        DMEEditor.ErrorObject.Flag = Errors.Failed;
                    }
                }
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, EntityName, "Insert");
            }

            return ErrorObject;
        }

        public virtual IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            SetObjects(EntityName);

            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
            if (UploadData != null)
            {
                IList<object> srcList = null;


                //           DMTypeBuilder.CreateNewObject(DMEEditor, null, srcentitystructure.EntityName, SourceFields);
                if (UploadData.GetType().FullName.Contains("DataTable"))
                {
                    srcList = DMEEditor.Utilfunction.GetListByDataTable((DataTable)UploadData, DMTypeBuilder.MyType, DataStruct);

                }
                else
                 if (UploadData.GetType().FullName.Contains("ObservableBindingList"))
                {
                    IBindingListView t = (IBindingListView)UploadData;
                    srcList = new List<object>();

                    foreach (var item in t)
                    {
                        srcList.Add((object)item);
                    }

                }
                else
                if (UploadData.GetType().FullName.Contains("List"))
                {
                    srcList = (IList<object>)UploadData;

                }
                else
                if (UploadData.GetType().FullName.Contains("IEnumerable"))
                {
                    srcList = (IList<object>)UploadData;
                }





                #region "Update Code"

                ErrorObject.Flag = Errors.Ok;

                string str = "";
                string errorstring = "";
                int CurrentRecord = 0;
                DMEEditor.ETL.CurrentScriptRecord = 0;
                DMEEditor.ETL.ScriptCount += srcList.Count;
                int highestPercentageReached = 0;
                int numberToCompute = DMEEditor.ETL.ScriptCount;
                try
                {
                    if (srcList != null)
                    {
                        numberToCompute = srcList.Count;
                        // int i = 0;

                        for (int i = 0; i < srcList.Count; i++)
                        {
                            try
                            {
                                object r = srcList[i];

                                DMEEditor.ErrorObject = InsertEntity(EntityName, r);
                                CurrentRecord = i;


                                string msg = "";
                                //int rowsUpdated = command.ExecuteNonQuery();
                                int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
                                if (percentComplete > highestPercentageReached)
                                {
                                    highestPercentageReached = percentComplete;

                                }
                                PassedArgs args = new PassedArgs
                                {
                                    CurrentEntity = EntityName,
                                    DatasourceName = DatasourceName,
                                    DataSource = this,
                                    EventType = "UpdateEntity",
                                };
                                args.ParameterInt1 = percentComplete;
                                //         UpdateEvents(EntityName, msg, highestPercentageReached, CurrentRecord, numberToCompute, this);
                                if (progress != null)
                                {
                                    PassedArgs ps = new PassedArgs { Messege = msg, ParameterInt1 = CurrentRecord, ParameterInt2 = DMEEditor.ETL.ScriptCount, ParameterString1 = null };
                                    progress.Report(ps);
                                }
                                //   PassEvent?.Invoke(this, args);
                                //   DMEEditor.RaiseEvent(this, args);
                            }
                            catch (Exception er)
                            {
                                string msg = $"Fail to I/U/D  Record {i} to {EntityName} ";
                                if (progress != null)
                                {
                                    PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentRecord, ParameterInt2 = DMEEditor.ETL.ScriptCount, ParameterString1 = msg };
                                    progress.Report(ps);
                                }
                                DMEEditor.AddLogMessage("Fail", msg, DateTime.Now, i, EntityName, Errors.Failed);
                            }
                        }
                        DMEEditor.ETL.CurrentScriptRecord = DMEEditor.ETL.ScriptCount;
                        //command.Dispose();
                        DMEEditor.AddLogMessage("Success", $"Finished Uploading Data to {EntityName}", DateTime.Now, 0, null, Errors.Ok);


                    }


                }
                catch (Exception ex)
                {
                    ErrorObject.Ex = ex;
                    command.Dispose();


                }
                #endregion
            }
            return ErrorObject;
        }

        public virtual IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            try
            {
                foreach (var item in entities)
                {
                    try
                    {
                        CreateEntityAs(item);
                    }
                    catch (Exception ex)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = ex.Message;
                        DMEEditor.AddLogMessage("Fail", $"Could not Create Entity {item.EntityName}" + ex.Message, DateTime.Now, -1, ex.Message, Errors.Failed);
                    }

                }
            }
            catch (Exception ex1)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex1.Message;
                DMEEditor.AddLogMessage("Fail", " Could not Complete Create Entities" + ex1.Message, DateTime.Now, -1, ex1.Message, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
    }
}
