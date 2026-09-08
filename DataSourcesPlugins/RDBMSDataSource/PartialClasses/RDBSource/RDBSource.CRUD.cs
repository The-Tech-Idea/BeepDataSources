using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
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
        /// Finds a property on the given type using case-insensitive matching.
        /// First tries exact (case-sensitive) match for performance, then falls back to case-insensitive.
        /// This ensures that database column names like "name" correctly match C# properties like "Name".
        /// </summary>
        private static PropertyInfo FindPropertyCaseInsensitive(Type type, string propertyName)
        {
            if (type == null || string.IsNullOrEmpty(propertyName))
                return null;

            // Fast path: try exact match first (most common case)
            var prop = type.GetProperty(propertyName);
            if (prop != null)
                return prop;

            // Slow path: case-insensitive fallback
            return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }
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
                ResetParameterAllocation();
                string updatestring = GetUpdateString(EntityName, DataStruct);
                
                using (var cmd = GetDataCommand())
                {
                    cmd.CommandText = updatestring;
                    CreateUpdateCommandParameters(cmd, UploadDataRow, DataStruct);

                    int rowsUpdated = cmd.ExecuteNonQuery();
                    ReportAffectedRows(rowsUpdated, EntityName, "updated", isUpdate: true);
                    if (rowsUpdated != 0)
                    {
                        // Invalidate cache after successful update
                        InvalidateEntityCache(EntityName);
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
                ResetParameterAllocation();
                string deleteString = GetDeleteString(EntityName, DataStruct);
                
                using (var cmd = GetDataCommand())
                {
                    cmd.CommandText = deleteString;
                    CreateDeleteCommandParameters(cmd, DeletedDataRow, DataStruct);
                    
                    int rowsDeleted = cmd.ExecuteNonQuery();
                    ReportAffectedRows(rowsDeleted, EntityName, "deleted", isUpdate: false);
                    if (rowsDeleted != 0)
                    {
                        // Invalidate cache after successful delete
                        InvalidateEntityCache(EntityName);
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
                ResetParameterAllocation();
                string insertString = GetInsertString(EntityName, DataStruct);
                
                using (var cmd = GetDataCommand())
                {
                    cmd.CommandText = insertString;
                    CreateCommandParameters(cmd, InsertedData, DataStruct);

                    int rowsInserted = cmd.ExecuteNonQuery();
                    if (rowsInserted > 0)
                    {
                        // Invalidate cache after successful insert
                        InvalidateEntityCache(EntityName);
                        
                        SetSuccess($"Successfully inserted record to {EntityName}");
                        
                        // Fetch auto-generated identity if applicable. Gated on the primary key
                        // actually BEING an auto-increment column: SQLite's last_insert_rowid() (and
                        // every provider's equivalent) always returns a value after a successful
                        // insert regardless of the declared PK — every SQLite table has an implicit
                        // ROWID even when the real PK is a client-generated TEXT/GUID column. Without
                        // this guard, that implicit rowid silently overwrote the caller's own primary
                        // key value on every insert, corrupting the in-memory entity (the row itself,
                        // written from the caller's value, stayed correct — only the C# object's PK
                        // no longer matched what was actually persisted).
                        string fetchIdentityQuery = RDBMSHelper.GenerateFetchLastIdentityQuery(DatasourceType);
                        var pkField = DataStruct.PrimaryKeys.Count() > 0 ? DataStruct.PrimaryKeys.First() : null;
                        if (fetchIdentityQuery.ToUpper().Contains("SELECT") && pkField != null && pkField.IsAutoIncrement)
                        {
                            cmd.CommandText = fetchIdentityQuery;
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                var pkFieldName = pkField.FieldName;
                                var primaryKeyProperty = FindPropertyCaseInsensitive(InsertedData.GetType(), pkFieldName);
                                if (primaryKeyProperty != null && primaryKeyProperty.CanWrite)
                                {
                                    var primaryKeyType = primaryKeyProperty.PropertyType;
                                    Type underlyingType = Nullable.GetUnderlyingType(primaryKeyType) ?? primaryKeyType;
                                    var convertedIdentity = Convert.ChangeType(result, underlyingType);
                                    primaryKeyProperty.SetValue(InsertedData, convertedIdentity);
                                    SetSuccess($"Successfully inserted record to {EntityName} with ID {convertedIdentity}");
                                }
                            }
                            else
                            {
                                SetFailure("Failed to retrieve the identity of the inserted record", EntityName);
                            }
                        }
                    }
                    else
                    {
                        SetFailure($"No records inserted to {EntityName}", EntityName);
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
                    // Converting a DataTable to typed rows is the engine's job, and there is no
                    // local fallback. Name the missing service instead of throwing
                    // NullReferenceException from inside a bulk upload.
                    if (DMEEditor?.Utilfunction == null)
                    {
                        SetFailure($"Cannot upload a DataTable to {EntityName}: the engine's Utilfunction service is not available to convert it.", EntityName);
                        return ErrorObject;
                    }

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

                int CurrentRecord = 0;

                // The ETL script counters are progress bookkeeping for the engine's script runner.
                // They were dereferenced unguarded, so this method -- which needs nothing else from
                // the editor to do its actual work -- threw NullReferenceException before touching
                // the database whenever no editor or no ETL service was attached.
                var etl = DMEEditor?.ETL;
                if (etl != null)
                {
                    etl.CurrentScriptRecord = 0;
                    etl.ScriptCount += srcList.Count;
                }

                int highestPercentageReached = 0;
                int numberToCompute = etl?.ScriptCount ?? srcList.Count;
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

                                // Read the row's result; do not re-point DMEEditor.ErrorObject at
                                // this datasource's error object (K38's fifth site). The result was
                                // also never inspected, so a row that failed to update was counted
                                // as progress and the method still reported "Finished Uploading".
                                var rowResult = UpdateEntity(EntityName, r);
                                CurrentRecord = i;

                                if (rowResult != null && rowResult.Flag == Errors.Failed)
                                {
                                    DMEEditor?.AddLogMessage("Fail",
                                        $"Record {i} of {EntityName} was not updated: {rowResult.Message}",
                                        DateTime.Now, i, EntityName, Errors.Failed);
                                }


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
                                    PassedArgs ps = new PassedArgs { Messege = msg, ParameterInt1 = CurrentRecord, ParameterInt2 = etl?.ScriptCount ?? numberToCompute, ParameterString1 = null };
                                    progress.Report(ps);
                                }
                                // Raise it. `args` above was built on every row and thrown away
                                // because this line was commented out, which is why PassEvent --
                                // declared on RDBSource and part of the IDataSource surface -- was
                                // never fired by anything in the class (CS0067).
                                PassEvent?.Invoke(this, args);
                                //   DMEEditor.RaiseEvent(this, args);
                            }
                            catch (Exception er)
                            {
                                // The exception was caught and its message dropped (CS0168), so a
                                // per-row failure reported the row number and nothing about why.
                                string msg = $"Fail to I/U/D  Record {i} to {EntityName}: {er.Message}";
                                if (progress != null)
                                {
                                    PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentRecord, ParameterInt2 = etl?.ScriptCount ?? numberToCompute, ParameterString1 = msg };
                                    progress.Report(ps);
                                }
                                DMEEditor?.AddLogMessage("Fail", msg, DateTime.Now, i, EntityName, Errors.Failed);
                            }
                        }
                        if (etl != null)
                        {
                            etl.CurrentScriptRecord = etl.ScriptCount;
                        }
                        //command.Dispose();
                        DMEEditor?.AddLogMessage("Success", $"Finished Uploading Data to {EntityName}", DateTime.Now, 0, null, Errors.Ok);


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
                        DMEEditor?.AddLogMessage("Fail", $"Could not Create Entity {item.EntityName}" + ex.Message, DateTime.Now, -1, ex.Message, Errors.Failed);
                    }

                }
            }
            catch (Exception ex1)
            {
                HandleDatabaseError(ex1, DatasourceName, "create entities in");
            }

            // Return the object the failure branches above actually wrote to. This used to set
            // ErrorObject and return DMEEditor.ErrorObject, which are only the same instance by
            // an aliasing coincidence of the standard creation path.
            return ErrorObject;
        }

        #region "Async CRUD Methods"

        /// <summary>
        /// Asynchronously inserts a new record into the database for the specified entity.
        /// </summary>
        public virtual async Task<IErrorsInfo> InsertEntityAsync(string EntityName, object InsertedData)
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
                ResetParameterAllocation();
                string insertString = GetInsertString(EntityName, DataStruct);
                
                using (var cmd = GetDataCommand())
                {
                    cmd.CommandText = insertString;
                    CreateCommandParameters(cmd, InsertedData, DataStruct);

                    int rowsInserted = await ExecuteNonQueryAsync(cmd).ConfigureAwait(false);
                    if (rowsInserted > 0)
                    {
                        // Invalidate cache after successful insert
                        InvalidateEntityCache(EntityName);
                        
                        SetSuccess($"Successfully inserted record to {EntityName}");
                        
                        // Fetch auto-generated identity if applicable. Gated on the primary key
                        // actually BEING an auto-increment column — see InsertEntity's identical
                        // guard for why: SQLite's last_insert_rowid() always returns the implicit
                        // ROWID after any insert, even for a table whose real PK is a client-generated
                        // TEXT/GUID column, and would otherwise silently overwrite the caller's PK.
                        string fetchIdentityQuery = RDBMSHelper.GenerateFetchLastIdentityQuery(DatasourceType);
                        var pkField = DataStruct.PrimaryKeys.Count() > 0 ? DataStruct.PrimaryKeys.First() : null;
                        if (fetchIdentityQuery.ToUpper().Contains("SELECT") && pkField != null && pkField.IsAutoIncrement)
                        {
                            cmd.CommandText = fetchIdentityQuery;
                            object result = await ExecuteScalarAsync(cmd).ConfigureAwait(false);
                            if (result != null && result != DBNull.Value)
                            {
                                var pkFieldName = pkField.FieldName;
                                var primaryKeyProperty = FindPropertyCaseInsensitive(InsertedData.GetType(), pkFieldName);
                                if (primaryKeyProperty != null && primaryKeyProperty.CanWrite)
                                {
                                    var primaryKeyType = primaryKeyProperty.PropertyType;
                                    Type underlyingType = Nullable.GetUnderlyingType(primaryKeyType) ?? primaryKeyType;
                                    var convertedIdentity = Convert.ChangeType(result, underlyingType);
                                    primaryKeyProperty.SetValue(InsertedData, convertedIdentity);
                                    SetSuccess($"Successfully inserted record to {EntityName} with ID {convertedIdentity}");
                                }
                            }
                        }
                    }
                    else
                    {
                        SetFailure($"No records inserted to {EntityName}", EntityName);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, EntityName, "Insert");
            }

            return ErrorObject;
        }

        /// <summary>
        /// Asynchronously updates a specific record in the database for the given entity.
        /// </summary>
        public virtual async Task<IErrorsInfo> UpdateEntityAsync(string EntityName, object UploadDataRow)
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
                ResetParameterAllocation();
                string updatestring = GetUpdateString(EntityName, DataStruct);
                
                using (var cmd = GetDataCommand())
                {
                    cmd.CommandText = updatestring;
                    CreateUpdateCommandParameters(cmd, UploadDataRow, DataStruct);

                    int rowsUpdated = await ExecuteNonQueryAsync(cmd).ConfigureAwait(false);
                    ReportAffectedRows(rowsUpdated, EntityName, "updated", isUpdate: true);
                    if (rowsUpdated != 0)
                    {
                        // Invalidate cache after successful update
                        InvalidateEntityCache(EntityName);
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
        /// Asynchronously deletes a specific record from the database for the given entity.
        /// </summary>
        public virtual async Task<IErrorsInfo> DeleteEntityAsync(string EntityName, object DeletedDataRow)
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
                ResetParameterAllocation();
                string deleteString = GetDeleteString(EntityName, DataStruct);
                
                using (var cmd = GetDataCommand())
                {
                    cmd.CommandText = deleteString;
                    CreateDeleteCommandParameters(cmd, DeletedDataRow, DataStruct);
                    
                    int rowsDeleted = await ExecuteNonQueryAsync(cmd).ConfigureAwait(false);
                    ReportAffectedRows(rowsDeleted, EntityName, "deleted", isUpdate: false);
                    if (rowsDeleted != 0)
                    {
                        // Invalidate cache after successful delete
                        InvalidateEntityCache(EntityName);
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
        /// Helper method to execute a command asynchronously if DbCommand is available, otherwise falls back to sync.
        /// </summary>
        private async Task<int> ExecuteNonQueryAsync(IDbCommand cmd)
        {
            if (cmd is DbCommand dbCommand)
            {
                return await dbCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Helper method to execute scalar asynchronously if DbCommand is available, otherwise falls back to sync.
        /// </summary>
        private async Task<object> ExecuteScalarAsync(IDbCommand cmd)
        {
            if (cmd is DbCommand dbCommand)
            {
                return await dbCommand.ExecuteScalarAsync().ConfigureAwait(false);
            }
            return cmd.ExecuteScalar();
        }

        #endregion
    }
}
