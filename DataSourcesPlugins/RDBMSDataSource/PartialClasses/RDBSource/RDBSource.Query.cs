using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        #region "Repo Methods"
        /// <summary>
        /// Executes a SQL query asynchronously and retrieves the first column of the first row in the result set.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>The scalar value as a double. Returns 0.0 if the query fails or doesn't return a valid double.</returns>
        /// <remarks>
        /// This method uses true async/await when the underlying provider supports it (DbCommand),
        /// falling back to Task.Run for legacy IDbCommand-only providers.
        /// </remarks>
        public virtual async Task<double> GetScalarAsync(string query)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                using (var command = GetDataCommand())
                {
                    command.CommandText = query;
                    
                    // Try to use async if DbCommand is available
                    if (command is System.Data.Common.DbCommand dbCommand)
                    {
                        var result = await dbCommand.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToDouble(result);
                        }
                    }
                    else
                    {
                        // Fallback for IDbCommand-only providers
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToDouble(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }

            return 0.0;
        }

        /// <summary>
        /// Synchronously retrieves a single scalar value from the database.
        /// </summary>
        /// <param name="query">The SQL query to be executed.</param>
        /// <returns>A task representing the asynchronous operation, resulting in the scalar value.</returns>
        public virtual double GetScalar(string query)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Assuming you have a database connection and command objects.

                using (var command = GetDataCommand())
                {
                    command.CommandText = query;
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var result = reader.GetDecimal(0); // Assuming the result is a decimal value
                            return Convert.ToDouble(result);
                        }
                    }
                }


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
        /// <summary>
        /// Executes a SQL command that does not return a result set.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <returns>An IErrorsInfo object indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// Use this method for SQL commands like INSERT, UPDATE, DELETE, etc.
        /// </remarks>
        public virtual IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            // CurrentSql = sql;
            IDbCommand cmd = GetDataCommand();
            if (cmd != null)
            {
                try
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                    //    DMEEditor.AddLogMessage("Success", "Executed Sql Successfully", DateTime.Now, -1, "Ok", Errors.Ok);
                }
                catch (Exception ex)
                {

                    cmd.Dispose();
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $" Could not run Script - {sql} -" + ex.Message;
                    DMEEditor.AddLogMessage("Fail", $" Could not run Script - {sql} -" + ex.Message, DateTime.Now, -1, ex.Message, Errors.Failed);

                }

            }

            return ErrorObject;
        }
        /// <summary>
        /// Executes a SQL query and returns the result set.
        /// </summary>
        /// <param name="qrystr">The SQL query string.</param>
        /// <returns>A DataTable containing the query results or null if an error occurs.</returns>
        /// <remarks>
        /// This method is suitable for queries that return multiple rows.
        /// </remarks>
        public virtual IEnumerable<object> RunQuery(string qrystr)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (string.IsNullOrWhiteSpace(qrystr))
                {
                    DMEEditor.AddLogMessage("Fail", "RunQuery: query string is null or empty", DateTime.Now, 0, "", Errors.Failed);
                    return Enumerable.Empty<object>();
                }

                if (Dataconnection.ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                using (var cmd = GetDataCommand())
                {
                    if (cmd == null)
                    {
                        DMEEditor.AddLogMessage("Fail", "RunQuery: failed to create data command", DateTime.Now, 0, "", Errors.Failed);
                        return Enumerable.Empty<object>();
                    }

                    cmd.CommandText = qrystr;

                    using (var reader = cmd.ExecuteReader(CommandBehavior.Default))
                    {
                        var dt = new DataTable();
                        dt.Load(reader);
                        return dt.AsEnumerable().Select(row => row.ItemArray);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error executing query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                return Enumerable.Empty<object>();
            }
        }

        // <summary>
        /// Dynamically builds an SQL query based on the original query and provided filters.
        /// </summary>
        /// <param name="originalquery">The base SQL query string.</param>
        /// <param name="Filter">List of filters to be applied to the query.</param>
        /// <returns>The dynamically built SQL query string.</returns>
        /// <remarks>
        /// This method enhances flexibility in data retrieval by allowing dynamic query modifications based on runtime conditions and parameters.
        /// </remarks>
        /// /// <summary>
        /// Dynamically builds an SQL query based on the original query and provided filters.
        /// Uses caching to improve performance for repeated queries.
        /// </summary>
        /// <param name="originalquery">The base SQL query string.</param>
        /// <param name="Filter">List of filters to be applied to the query.</param>
        /// <param name="entityName">Optional entity name for cache key generation.</param>
        /// <returns>The dynamically built SQL query string.</returns>
        /// <remarks>
        /// This method creates flexible, database-agnostic queries by properly handling 
        /// SQL syntax, filter operators, and parameter names for prepared statements.
        /// </remarks>

        private string BuildQuery(string originalquery, List<AppFilter> Filter, string? entityName = null)
        {
            // Try to get from cache first
            if (!string.IsNullOrEmpty(entityName))
            {
                var cacheKey = GenerateQueryCacheKey(entityName, Filter);
                if (TryGetCachedQuery(cacheKey, out var cachedQuery) && !string.IsNullOrEmpty(cachedQuery))
                {
                    return cachedQuery;
                }

                // Build the query
                var builtQuery = BuildQueryInternal(originalquery, Filter);

                // Cache the result
                CacheQuery(cacheKey, builtQuery);

                return builtQuery;
            }

            // If no entity name provided, build without caching
            return BuildQueryInternal(originalquery, Filter);
        }

        /// <summary>
        /// Internal method that performs the actual query building logic.
        /// </summary>
        private string BuildQueryInternal(string originalquery, List<AppFilter> Filter)
        {
            string retval;
            string[] stringSeparators;
            string[] sp;
            string qrystr = "Select ";
            bool FoundWhere = false;
            QueryBuild queryStructure = new QueryBuild();
            try
            {
                //stringSeparators = new string[] {"select ", " from ", " where ", " group by "," having ", " order by " };
                // Get Selected Fields
                originalquery = GetTableName(originalquery.ToLower());
                stringSeparators = new string[] { "select", "from", "where", "group by", "having", "order by" };
                sp = originalquery.ToLower().Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                queryStructure.FieldsString = sp[0];
                string[] Fieldsp = sp[0].Split(',');
                queryStructure.Fields.AddRange(Fieldsp);
                // Get From  Tables
                queryStructure.EntitiesString = sp[1];
                string[] Tablesdsp = sp[1].Split(',');
                queryStructure.Entities.AddRange(Tablesdsp);

                if (GetSchemaName() == null)
                {
                    qrystr += queryStructure.FieldsString + " " + " from " + queryStructure.EntitiesString;
                }
                else
                    qrystr += queryStructure.FieldsString + $" from {GetSchemaName().ToLower()}." + queryStructure.EntitiesString;

                qrystr += Environment.NewLine;

                if (Filter != null)
                {
                    if (Filter.Count > 0)
                    {
                        if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {
                            qrystr += Environment.NewLine;
                            if (FoundWhere == false)
                            {
                                qrystr += " where " + Environment.NewLine;
                                FoundWhere = true;
                            }

                            foreach (AppFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)))
                            {
                                if (!string.IsNullOrEmpty(item.FilterValue) && !string.IsNullOrWhiteSpace(item.FilterValue))
                                {
                                    //  EntityField f = ent.Fields.Where(i => i.FieldName == item.FieldName).FirstOrDefault();
                                    if (item.Operator.ToLower() == "between")
                                    {
                                        qrystr += item.FieldName + " " + item.Operator + $" {ParameterDelimiter}p_" + item.FieldName + $" and  {ParameterDelimiter}p_" + item.FieldName + "1 " + Environment.NewLine;
                                    }
                                    else
                                    {
                                        qrystr += item.FieldName + " " + item.Operator + $" {ParameterDelimiter}p_" + item.FieldName + " " + Environment.NewLine;
                                    }

                                }



                            }
                        }
                    }
                }
                if (originalquery.ToLower().Contains("where"))
                {
                    qrystr += Environment.NewLine;

                    string[] whereSeparators = new string[] { "where", "group by", "having", "order by" };

                    string[] spwhere = originalquery.ToLower().Split(whereSeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.WhereCondition = spwhere[0];
                    if (FoundWhere == false)
                    {
                        qrystr += " where " + Environment.NewLine;
                        FoundWhere = true;
                    }
                    qrystr += spwhere[1];
                    qrystr += Environment.NewLine;



                }
                if (originalquery.ToLower().Contains("group by"))
                {
                    string[] groupbySeparators = new string[] { "group by", "having", "order by" };

                    string[] groupbywhere = originalquery.ToLower().Split(groupbySeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.GroupbyCondition = groupbywhere[1];
                    qrystr += " group by " + groupbywhere[1];
                    qrystr += Environment.NewLine;
                }
                if (originalquery.ToLower().Contains("having"))
                {
                    string[] havingSeparators = new string[] { "having", "order by" };

                    string[] havingywhere = originalquery.ToLower().Split(havingSeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.HavingCondition = havingywhere[1];
                    qrystr += " having " + havingywhere[1];
                    qrystr += Environment.NewLine;
                }
                if (originalquery.ToLower().Contains("order by"))
                {
                    string[] orderbySeparators = new string[] { "order by" };

                    string[] orderbywhere = originalquery.ToLower().Split(orderbySeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.OrderbyCondition = orderbywhere[1];
                    qrystr += " order by " + orderbywhere[1];

                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Unable Build Query Object {originalquery}- {ex.Message}", DateTime.Now, 0, "Error", Errors.Failed);
            }
            return qrystr;
        }

        /// <summary>
        /// Parses SQL query components into a QueryBuild structure.
        /// </summary>
        private QueryBuild ParseQueryComponents(string query)
        {
            QueryBuild queryStructure = new QueryBuild();

            // Define the SQL clause keywords to split by
            string[] clauseKeywords = { "select", "from", "where", "group by", "having", "order by" };

            // Split the query by clause keywords
            string[] parts = query.Split(clauseKeywords, StringSplitOptions.RemoveEmptyEntries);

            // Parse SELECT clause
            if (parts.Length > 0)
            {
                queryStructure.FieldsString = parts[0].Trim();
                queryStructure.Fields.AddRange(parts[0].Split(',').Select(f => f.Trim()));
            }

            // Parse FROM clause
            if (parts.Length > 1)
            {
                queryStructure.EntitiesString = parts[1].Trim();
                queryStructure.Entities.AddRange(parts[1].Split(',').Select(e => e.Trim()));
            }

            // Extract additional clauses if present in original query
            if (query.Contains("where"))
            {
                int wherePos = query.IndexOf("where", StringComparison.OrdinalIgnoreCase) + 5;
                int endPos = FindNextClausePosition(query, wherePos, new[] { "group by", "having", "order by" });
                queryStructure.WhereCondition = query.Substring(wherePos, endPos - wherePos).Trim();
            }

            if (query.Contains("group by"))
            {
                int groupByPos = query.IndexOf("group by", StringComparison.OrdinalIgnoreCase) + 8;
                int endPos = FindNextClausePosition(query, groupByPos, new[] { "having", "order by" });
                queryStructure.GroupbyCondition = query.Substring(groupByPos, endPos - groupByPos).Trim();
            }

            if (query.Contains("having"))
            {
                int havingPos = query.IndexOf("having", StringComparison.OrdinalIgnoreCase) + 6;
                int endPos = FindNextClausePosition(query, havingPos, new[] { "order by" });
                queryStructure.HavingCondition = query.Substring(havingPos, endPos - havingPos).Trim();
            }

            if (query.Contains("order by"))
            {
                int orderByPos = query.IndexOf("order by", StringComparison.OrdinalIgnoreCase) + 8;
                queryStructure.OrderbyCondition = query.Substring(orderByPos).Trim();
            }

            return queryStructure;
        }

        /// <summary>
        /// Finds the position of the next SQL clause in the query.
        /// </summary>
        private int FindNextClausePosition(string query, int startPos, string[] clauses)
        {
            int nextPos = query.Length;

            foreach (string clause in clauses)
            {
                int pos = query.IndexOf(clause, startPos, StringComparison.OrdinalIgnoreCase);
                if (pos > 0 && pos < nextPos)
                {
                    nextPos = pos;
                }
            }

            return nextPos;
        }

        /// <summary>
        /// Gets the schema prefix for the query.
        /// </summary>
        private string GetSchemaPrefix()
        {
            string schemaName = GetSchemaName();
            return !string.IsNullOrEmpty(schemaName) ? $"{schemaName}." : string.Empty;
        }

        /// <summary>
        /// Builds the WHERE clause including any filters.
        /// </summary>
        private string BuildWhereClause(QueryBuild queryStructure, List<AppFilter> filters, bool hasExistingWhere)
        {
            StringBuilder whereBuilder = new StringBuilder();
            bool hasFilters = filters != null && filters.Any(f => IsValidFilter(f));

            // Determine if we need to add a WHERE clause
            if (hasExistingWhere || hasFilters)
            {
                whereBuilder.Append("WHERE ");

                // Add filters if present
                if (hasFilters)
                {
                    bool firstFilter = true;
                    foreach (AppFilter filter in filters.Where(IsValidFilter))
                    {
                        if (!firstFilter)
                        {
                            whereBuilder.AppendLine(" AND ");
                        }

                        whereBuilder.Append(FormatFilterCondition(filter));
                        firstFilter = false;
                    }
                }

                // Add existing where clause if present
                if (hasExistingWhere && !string.IsNullOrEmpty(queryStructure.WhereCondition))
                {
                    if (hasFilters)
                    {
                        whereBuilder.AppendLine(" AND ");
                    }
                    whereBuilder.Append(queryStructure.WhereCondition);
                }
            }

            return whereBuilder.ToString();
        }

        /// <summary>
        /// Checks if an AppFilter has valid values for SQL generation.
        /// </summary>
        private bool IsValidFilter(AppFilter filter)
        {
            return filter != null &&
                   !string.IsNullOrEmpty(filter.FieldName) &&
                   !string.IsNullOrWhiteSpace(filter.FieldName) &&
                   !string.IsNullOrEmpty(filter.Operator) &&
                   !string.IsNullOrWhiteSpace(filter.Operator) &&
                   !string.IsNullOrEmpty(filter.FilterValue) &&
                   !string.IsNullOrWhiteSpace(filter.FilterValue);
        }

        /// <summary>
        /// Formats a filter condition for SQL.
        /// </summary>
        private string FormatFilterCondition(AppFilter filter)
        {
            string FieldName = filter.FieldName;
            string paramName = SanitizeParameterName(filter.FieldName);

            if (filter.Operator.ToLower() == "between")
            {
                return $"{FieldName} BETWEEN {ParameterDelimiter}p_{paramName} AND {ParameterDelimiter}p_{paramName}1";
            }
            else
            {
                return $"{FieldName} {filter.Operator} {ParameterDelimiter}p_{paramName}";
            }
        }

        /// <summary>
        /// Sanitizes a parameter name to ensure it's valid for SQL.
        /// </summary>
        private string SanitizeParameterName(string FieldName)
        {
            // Replace spaces with underscores and ensure name is valid
            string paramName = Regex.Replace(FieldName, @"\s+", "_");

            // Truncate if needed (for databases with name length limits)
            if (paramName.Length > 30 && (DatasourceType == DataSourceType.Oracle || DatasourceType == DataSourceType.Postgre))
            {
                paramName = paramName.Substring(0, 30);
            }

            return paramName;
        }

        /// <summary>
        /// Appends a SQL clause to the query builder if it exists.
        /// </summary>
        private void AppendClauseIfExists(StringBuilder queryBuilder, string clauseContent, string clauseName)
        {
            if (!string.IsNullOrEmpty(clauseContent))
            {
                queryBuilder.AppendLine($"{clauseName} {clauseContent}");
            }
        }

        /// <summary>
        /// Retrieves data for a specified entity from the database, with the option to apply filters.
        /// </summary>
        /// <param name="EntityName">The name of the entity (table) to retrieve data from.</param>
        /// <param name="Filter">A list of filters to apply to the query.</param>
        /// <remarks>
        /// This method supports both direct table queries and custom queries. It uses dynamic SQL generation and can adapt to different database types. The method also converts the retrieved DataTable to a list of objects based on the entity's structure and type.
        /// </remarks>
        /// <returns>An object representing the data retrieved, which could be a list or another type based on the entity structure.</returns>
        /// <exception cref="Exception">Catches and logs any exceptions that occur during the data retrieval process.</exception>
        public virtual IEnumerable<object> GetEntity(string EntityName, List<AppFilter> Filter)
        {
            ErrorObject.Flag = Errors.Ok;
            string inname = string.Empty;
            string qrystr = "select * from ";

            // Determine base query (table name vs full select)
            if (!string.IsNullOrWhiteSpace(EntityName))
            {
                if (!EntityName.Contains("select", StringComparison.OrdinalIgnoreCase) &&
                    !EntityName.Contains("from", StringComparison.OrdinalIgnoreCase))
                {
                    qrystr = "select * from " + EntityName;
                    qrystr = GetTableName(qrystr.ToLower());
                    inname = EntityName;
                }
                else
                {
                    EntityName = GetTableName(EntityName);
                    string[] stringSeparators = { " from ", " where ", " group by ", " order by " };
                    var sp = EntityName.ToLower().Split(stringSeparators, StringSplitOptions.None);
                    qrystr = EntityName;
                    if (sp.Length > 1)
                        inname = sp[1].Trim();
                }
            }

            // Allow custom query from metadata
            try
            {
                var ent = GetEntityStructure(inname);
                if (ent != null && !string.IsNullOrEmpty(ent.CustomBuildQuery))
                {
                    qrystr = ent.CustomBuildQuery;
                }
            }
            catch { /* ignore metadata errors for streaming */ }

            // Inject filter placeholders (adds parameter tokens only)
            qrystr = BuildQuery(qrystr, Filter, inname);

            if (Dataconnection.ConnectionStatus != ConnectionState.Open)
                Openconnection();

            IDbCommand cmd = null;
            IDataReader reader = null;

            // Prepare command & reader inside try (no yield here)
            try
            {
                cmd = GetDataCommand();
                if (cmd == null)
                    yield break;

                cmd.CommandText = qrystr;

                // Add parameters matching placeholders
                if (Filter != null)
                {
                    foreach (var f in Filter.Where(p =>
                             !string.IsNullOrWhiteSpace(p.FieldName) &&
                             !string.IsNullOrWhiteSpace(p.Operator) &&
                             !string.IsNullOrWhiteSpace(p.FilterValue)))
                    {
                        string paramBase = SanitizeParameterName(f.FieldName);

                        var p = cmd.CreateParameter();
                        p.ParameterName = $"p_{paramBase}";
                        if (f.valueType == "System.DateTime" && DateTime.TryParse(f.FilterValue, out var dt))
                        {
                            p.DbType = DbType.DateTime;
                            p.Value = dt;
                        }
                        else
                        {
                            p.Value = f.FilterValue;
                        }
                        cmd.Parameters.Add(p);

                        if (f.Operator.Equals("between", StringComparison.OrdinalIgnoreCase))
                        {
                            var p2 = cmd.CreateParameter();
                            p2.ParameterName = $"p_{paramBase}1";
                            if (f.valueType == "System.DateTime" && DateTime.TryParse(f.FilterValue1, out var dt2))
                            {
                                p2.DbType = DbType.DateTime;
                                p2.Value = dt2;
                            }
                            else
                            {
                                p2.Value = f.FilterValue1;
                            }
                            cmd.Parameters.Add(p2);
                        }
                    }
                }

                reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Fail", $"Error preparing entity stream ({ex.Message})", DateTime.Now, 0, inname, Errors.Failed);
                if (reader != null) { try { reader.Close(); } catch { } }
                cmd?.Dispose();
                yield break;
            }

            // Streaming loop using DataStreamer helper
            using (cmd)
            {
                foreach (var row in DataBase.Helpers.DataStreamer.Stream(reader))
                {
                    yield return row;
                }
            }
        }
        /// <summary>
        /// Retrieves data for a specified entity from the database with pagination support.
        /// </summary>
        /// <param name="EntityName">The name of the entity (table) to retrieve data from.</param>
        /// <param name="Filter">A list of filters to apply to the query.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <returns>A PagedResult object containing the data and pagination metadata.</returns>
        public virtual PagedResult GetEntity(string EntityName, List<AppFilter> Filter, int pageNumber, int pageSize)
        {
            ErrorObject.Flag = Errors.Ok;

            if (string.IsNullOrWhiteSpace(EntityName))
            {
                DMEEditor.AddLogMessage("Fail", "Entity name cannot be null or empty", DateTime.Now, 0, "", Errors.Failed);
                return null;
            }
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            bool isCustomQuery = EntityName.Contains("select", StringComparison.OrdinalIgnoreCase)
                                 && EntityName.Contains("from", StringComparison.OrdinalIgnoreCase);

            string baseQuery = isCustomQuery ? EntityName : $"SELECT * FROM {EntityName}";
            string inname = EntityName;
            string entityForStruct = isCustomQuery ? ExtractFirstTableName(baseQuery) ?? EntityName : EntityName;

            // Try to resolve structure (may override with CustomBuildQuery)
            EntityStructure ent = null;
            try
            {
                ent = GetEntityStructure(entityForStruct);
                if (ent != null && !string.IsNullOrEmpty(ent.CustomBuildQuery))
                {
                    baseQuery = ent.CustomBuildQuery;
                    isCustomQuery = true;
                }
            }
            catch { /* non-fatal */ }

            // Ensure deterministic ORDER BY for paging
            if (!baseQuery.Contains("order by", StringComparison.OrdinalIgnoreCase))
            {
                if (ent?.PrimaryKeys != null && ent.PrimaryKeys.Count > 0)
                {
                    baseQuery += $" ORDER BY {GetFieldName(ent.PrimaryKeys[0].FieldName)}";
                }
                else
                {
                    baseQuery += " ORDER BY 1";
                }
            }

            // Build filtered query (adds WHERE and parameter placeholders)
            string filteredQuery = BuildQuery(baseQuery, Filter, entityForStruct);

            // Count query
            string countQuery;
            if (!isCustomQuery)
            {
                string tablePart = ExtractFirstTableName(baseQuery) ?? EntityName;
                countQuery = $"SELECT COUNT(*) FROM {tablePart}";
                string whereClause = ExtractWhereClause(filteredQuery);
                if (!string.IsNullOrEmpty(whereClause))
                {
                    countQuery += " " + whereClause;
                }
            }
            else
            {
                string noOrder = StripTrailingOrderBy(filteredQuery);
                countQuery = $"SELECT COUNT(*) FROM ({noOrder}) __q";
            }

            // Paging syntax (e.g. OFFSET/FETCH, LIMIT/OFFSET, etc.)
            string pagingSyntax = RDBMSHelper.GetPagingSyntax(DatasourceType, pageNumber, pageSize);
            string pagedQuery = $"{filteredQuery} {pagingSyntax}";

            int totalRecords = 0;
            if (Dataconnection.ConnectionStatus != ConnectionState.Open)
                Openconnection();

            // Execute count
            try
            {
                using var countCmd = GetDataCommand();
                if (countCmd == null) return null;
                countCmd.CommandText = countQuery;
                AddFilterParameters(countCmd, Filter);
                totalRecords = (int)Convert.ToInt64(countCmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Warning", $"Count failed: {ex.Message}", DateTime.Now, 0, EntityName, Errors.Warning);
            }

            // Execute paged data query
            var rows = new List<object>();
            try
            {
                using var dataCmd = GetDataCommand();
                if (dataCmd == null) return null;
                dataCmd.CommandText = pagedQuery;
                AddFilterParameters(dataCmd, Filter);

                using var reader = dataCmd.ExecuteReader(CommandBehavior.SequentialAccess);
                // Use DataStreamer helper for efficient data streaming
                foreach (var row in DataBase.Helpers.DataStreamer.Stream(reader))
                {
                    rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error executing paginated query: {ex.Message}", DateTime.Now, 0, EntityName, Errors.Failed);
                return null;
            }

            return new PagedResult
            {
                Data = rows,                 // IEnumerable<object>
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 0,
                HasNextPage = pageNumber * pageSize < totalRecords,
                HasPreviousPage = pageNumber > 1
            };
        }

        // Reuse the same parameter injection logic as streaming GetEntity
        /// <summary>
        /// Adds filter parameters to a database command using the FilterParameterBinder helper.
        /// This eliminates duplicate parameter binding logic by delegating to the centralized helper.
        /// </summary>
        private void AddFilterParameters(IDbCommand cmd, List<AppFilter> filters)
        {
            DataBase.Helpers.FilterParameterBinder.Bind(cmd, filters, SanitizeParameterName);
        }
        // Helper: remove trailing ORDER BY for wrapping in COUNT
        private static string StripTrailingOrderBy(string sql)
        {
            int idx = sql.LastIndexOf(" order by ", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                // Ensure no closing parenthesis after (naive but sufficient here)
                string tail = sql.Substring(idx);
                if (!tail.Contains(")"))
                {
                    return sql.Substring(0, idx);
                }
            }
            return sql;
        }

        // Helper: crude extraction of first table identifier (used only for fallback scenarios)
        private static string ExtractFirstTableName(string sql)
        {
            try
            {
                var low = sql.ToLower();
                int fromIdx = low.IndexOf(" from ");
                if (fromIdx < 0) return null;
                int start = fromIdx + 6;
                int end = low.IndexOfAny(new[] { ' ', '\r', '\n', '\t', ',' }, start);
                if (end < 0) end = sql.Length;
                string token = sql.Substring(start, end - start).Trim();
                // Remove schema alias patterns
                if (token.Contains(")")) return null;
                if (token.Equals("select", StringComparison.OrdinalIgnoreCase)) return null;
                return token;
            }
            catch { return null; }
        }
        /// <summary>
        /// Asynchronously retrieves data for a specified entity from the database, with the option to apply filters.
        /// </summary>
        /// <param name="EntityName">The name of the entity (table) to retrieve data from.</param>
        /// <param name="Filter">A list of filters to apply to the query.</param>
        /// <remarks>
        /// This method is an asynchronous wrapper around GetEntity, providing the same functionality but in an async manner. It is particularly useful for operations that might take a longer time to complete, ensuring that the application remains responsive.
        /// </remarks>
        /// <returns>A task representing the asynchronous operation, which, when completed, will return an object representing the data retrieved.</returns>
        public virtual Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.FromResult(GetEntity(EntityName, Filter));
        }

        // Helper method to extract WHERE clause from a query
        private string ExtractWhereClause(string query)
        {
            string lowerQuery = query.ToLower();
            int wherePos = lowerQuery.IndexOf(" where ");

            if (wherePos >= 0)
            {
                // Find the position after "where"
                int startPos = wherePos + 7; // length of " where "

                // Find the next clause, if any
                int endPos = lowerQuery.Length;
                string[] endClauses = { " group by ", " having ", " order by " };

                foreach (string clause in endClauses)
                {
                    int pos = lowerQuery.IndexOf(clause, startPos);
                    if (pos >= 0 && pos < endPos)
                    {
                        endPos = pos;
                    }
                }

                return "WHERE " + query.Substring(startPos, endPos - startPos).Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Sets up necessary objects and structures for database operations based on the provided entity name.
        /// </summary>
        /// <param name="Entityname">The name of the entity for which the database command objects will be set up.</param>
        /// <remarks>
        /// This method is essential for initializing and reusing database commands and structures, improving efficiency and maintainability.
        /// </remarks>
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, false);
                command = RDBMSConnection.DbConn.CreateCommand();
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }

        public virtual IDataReader GetDataReader(string querystring)
        {
            IDbCommand cmd = GetDataCommand();
            cmd.CommandText = querystring;
            IDataReader dt = cmd.ExecuteReader();

            return dt;

        }

        #endregion
    }
}
