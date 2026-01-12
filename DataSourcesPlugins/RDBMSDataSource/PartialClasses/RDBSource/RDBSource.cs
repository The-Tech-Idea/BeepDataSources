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

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        HashSet<string> usedParameterNames = new HashSet<string>();
        List<EntityField> UpdateFieldSequnce = new List<EntityField>();
        public event EventHandler<PassedArgs> PassEvent;
        // Static random number generator used for various purposes within the class.
        static Random r = new Random();

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
