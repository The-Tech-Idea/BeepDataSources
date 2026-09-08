using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Dapper;
using System.Reflection;
using System.Data.Common;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using System.Data.SqlTypes;
using TheTechIdea.Beep.Helpers;
using System.Diagnostics;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using System.Text;
using System.Collections;
using static TheTechIdea.Beep.Utils.Util;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;
using TheTechIdea.Beep.DataBase.Helpers;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        // Per-operation statement-building state, reset together by ResetParameterAllocation().
        //
        // These are instance fields, so an RDBSource is NOT safe for concurrent use: a second call
        // on the same datasource resets them out from under the first one mid-build. (An earlier
        // comment here claimed the reset made them "thread-safe per-operation"; the reset is
        // exactly what makes them unsafe.) Serialise access per datasource, or give each caller
        // its own instance.
        HashSet<string> usedParameterNames = new HashSet<string>();

        /// <summary>
        /// Field name to the parameter name allocated for it while the statement text was built.
        /// </summary>
        /// <remarks>
        /// Binding has to recover which parameter name belongs to which field. It used to do that by
        /// searching <see cref="usedParameterNames"/> for a name that merely STARTED WITH the field
        /// name — and, for the UPDATE primary-key clause, one that merely CONTAINED it. A HashSet has
        /// no defined enumeration order, so with columns Name and NameSuffix the lookup for Name
        /// could return NameSuffix; with primary key Id and a SET-clause column ProductId the WHERE
        /// clause became "where Id = @p_ProductId". The UPDATE then landed on a row chosen by the
        /// wrong column's value, silently. Keyed lookup removes the guesswork.
        /// </remarks>
        Dictionary<string, string> parameterNamesByField = new Dictionary<string, string>(StringComparer.Ordinal);

        List<EntityField> UpdateFieldSequnce = new List<EntityField>();
        public event EventHandler<PassedArgs> PassEvent;
        
        // Thread-safe entity structure cache
        private EntityStructureCache _entityCache;
        private EntityStructureCache EntityCache
        {
            get
            {
                if (_entityCache == null)
                {
                    _entityCache = new EntityStructureCache((name, refresh) => LoadEntityStructure(name, refresh));
                }
                return _entityCache;
            }
        }

        /// <summary>
        /// Unique identifier for the RDBSource instance, generated using Guid.
        /// </summary>
        public string GuidID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// General identifier of the RDBSource instance.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the data source.
        /// </summary>
        public string DatasourceName { get; set; }

        /// <summary>
        /// Type of the data source, indicating the specific relational database system (e.g., SQL Server, MySQL).
        /// </summary>
        public DataSourceType DatasourceType { get; set; }

        /// <summary>
        /// Current state of the database connection.
        /// </summary>
        public ConnectionState ConnectionStatus { get => Dataconnection.ConnectionStatus; set { } }

        /// <summary>
        /// Category of the data source, typically RDBMS for relational databases.
        /// </summary>
        public DatasourceCategory Category { get; set; } = DatasourceCategory.RDBMS;

        /// <summary>
        /// Object to handle error information.
        /// </summary>
        public IErrorsInfo ErrorObject { get; set; }

        /// <summary>
        /// Logger instance for logging activities and events.
        /// </summary>
        public IDMLogger Logger { get; set; }

        /// <summary>
        /// List of names of entities (e.g., tables) available in the database.
        /// </summary>
        public List<string> EntitiesNames { get; set; } = new List<string>();

        /// <summary>
        /// Editor instance for managing various database operations.
        /// </summary>
        public IDMEEditor DMEEditor { get; set; }

        /// <summary>
        /// List of entity structures representing database schemas.
        /// </summary>
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();

        /// <summary>
        /// Connection object to interact with the database.
        /// </summary>
        public IDataConnection Dataconnection { get; set; }

        /// <summary>
        /// Specialized connection object for relational databases.
        /// </summary>
        public RDBDataConnection RDBMSConnection { get { return (RDBDataConnection)Dataconnection; } }

        /// <summary>
        /// Delimiter used for columns in queries, specific to the database syntax.
        /// </summary>
        public virtual string ColumnDelimiter { get; set; } = "''";

        /// <summary>
        /// Delimiter used for parameters in queries, specific to the database syntax.
        /// </summary>
        public virtual string ParameterDelimiter { get; set; } = "@";

        /// <summary>
        /// Initializes a new instance of the RDBSource class.
        /// </summary>
        /// <param name="datasourcename">Name of the data source.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="pDMEEditor">DMEEditor instance for database operations.</param>
        /// <param name="databasetype">Type of the database.</param>
        /// <param name="per">Error information object.</param>
        protected static int recNumber = 0;
        protected string recEntity = "";

        /// <summary>
        /// Get List of Tables that connection has that is not on that same user
        /// </summary>
        ///
        public string GetListofEntitiesSql { get; set; } = string.Empty;
        #region "Insert or Update or Delete Objects"
        EntityStructure DataStruct = null;
        IDbCommand command = null;
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        #endregion
        public RDBSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.RDBMS;
            Dataconnection = new RDBDataConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,
            };
        }
    }
}
