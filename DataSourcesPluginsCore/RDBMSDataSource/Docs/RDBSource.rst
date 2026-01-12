RDBSource Class
================

.. class:: RDBSource

   Concrete relational implementation of :code:`IDataSource` providing metadata discovery, CRUD, DDL generation, filtering, pagination and relationship introspection for SQL databases.

   Responsibilities
   ----------------
   - Connection management (open / close)
   - Entity & field metadata loading
   - Dynamic runtime type construction for rows
   - Parameterized CRUD (Insert / Update / Delete)
   - Query execution & filtering
   - Pagination (provider aware)
   - DDL script generation (tables + FKs)
   - Foreign key & child relation discovery
   - Scalar retrieval & ad-hoc SQL

   Key Properties
   --------------
   .. attribute:: GuidID
      :type: string
      Unique instance identifier.

   .. attribute:: DatasourceName
      :type: string
      Logical name of the data source.

   .. attribute:: DatasourceType
      :type: DataSourceType
      Backend provider (SqlServer, Oracle, MySql, Postgre, SqlLite ...).

   .. attribute:: Entities
      :type: List[EntityStructure]
      In-memory cached schema metadata.

   .. attribute:: EntitiesNames
      :type: List[string]
      Cached table names.

   .. attribute:: Dataconnection
      :type: IDataConnection
      Underlying connection & driver info wrapper.

   .. attribute:: ColumnDelimiter
      :type: string
      Delimiter for quoted identifiers when spaces exist.

   .. attribute:: ParameterDelimiter
      :type: string
      Prefix for SQL parameters (e.g. '@' / ':' ).

   Core Methods (Selected)
   -----------------------
   .. method:: Openconnection()

      Opens physical connection; returns :code:`ConnectionState`.

   .. method:: Closeconnection()

      Closes connection and releases underlying handles.

   .. method:: GetEntitesList()

      Returns list of table names using provider specific metadata query.

   .. method:: GetEntityStructure(EntityName, refresh=False)

      Loads / refreshes :code:`EntityStructure` including fields, primary keys and relations.

   .. method:: GetEntity(EntityName, filters)

      Executes a SELECT (table or custom query) and materializes rows into :code:`ObservableBindingList[T]`.

   .. method:: GetEntity(EntityName, filters, pageNumber, pageSize)

      Paginated variant returning :code:`PagedResult` (data + metadata).

   .. method:: InsertEntity(EntityName, obj)

      Builds INSERT with parameter collision avoidance then executes & (optionally) retrieves identity.

   .. method:: UpdateEntity(EntityName, obj)

      Builds UPDATE ordering non-PK fields then PK predicates.

   .. method:: DeleteEntity(EntityName, obj)

      Builds DELETE predicate from composite PK.

   .. method:: ExecuteSql(sql)

      Executes non-query SQL (DDL / DML) returning :code:`IErrorsInfo`.

   .. method:: GetScalar(query)

      Executes scalar query returning numeric value.

   .. method:: GetCreateEntityScript(entities)

      Produces ordered DDL scripts for tables & foreign keys.

   .. method:: RunScript(script)

      Executes provided :code:`ETLScriptDet` DDL script.

   .. method:: CreateEntityAs(EntityStructure)

      Creates physical table if not exists using generated DDL.

   .. method:: GetEntityforeignkeys(entity, schema)

      Returns list of :code:`RelationShipKeys` (FK metadata).

   .. method:: GetChildTablesList(table, schema, params)

      Returns child relation rows (:code:`ChildRelation`).

   Internals & Helpers
   -------------------
   - :code:`BuildQuery` merges filters into base SELECT preserving GROUP/HAVING/ORDER
   - :code:`GetInsertString`, :code:`GetUpdateString`, :code:`GetDeleteString` construct provider neutral SQL
   - :code:`GenerateCreateEntityScript` + :code:`CreatePrimaryKeyString` build CREATE TABLE
   - :code:`CreateForKeyRelationScripts` generates ALTER TABLE ADD CONSTRAINT statements
   - :code:`MapOracleFloatToDotNetType` maps Oracle FLOAT precision
   - :code:`GetData<T>()` / :code:`SaveData<T>()` expose lightweight Dapper access

   Extension Points
   ----------------
   Override virtual members in derived classes to customize vendor quirks (naming, identity retrieval, paging logic, schema translation).

   Error Handling
   --------------
   All operations populate :code:`ErrorObject` and log via :code:`IDMEEditor.AddLogMessage`.
