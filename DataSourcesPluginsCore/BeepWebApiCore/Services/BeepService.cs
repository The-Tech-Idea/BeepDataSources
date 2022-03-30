using KocSharedLib.KocClasses;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Tools;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Tools;
using TheTechIdea.Tools.AssemblyHandling;
using TheTechIdea.Util;
namespace KocWebApi.Services
{
    public class BeepService : IBeepService
    {
        #region "Properties"
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource src { get; set; }
        private IConfigEditor Config_editor { get; set; }
        public IWorkFlowEditor WorkFlowEditor { get; set; }
        public IDMLogger lg { get; set; }
        public IUtil util { get; set; }
        public IErrorsInfo Erinfo { get; set; }
        public IJsonLoader jsonLoader { get; set; }
        public IAssemblyHandler LLoader { get; set; }
        public IClassCreator classCreator { get; set; }
        public IDataTypesHelper typesHelper { get; set; }
        public IETL eTL { get; set; }
      
        #endregion
        public BeepService(IServiceCollection services)
        {
            CreateBeepEnv();
            services.AddSingleton<IDMLogger>(lg);
            services.AddSingleton<IJsonLoader>(jsonLoader);
            services.AddSingleton<IUtil>(util);
            services.AddSingleton<IErrorsInfo>(Erinfo);
            services.AddSingleton<IConfigEditor>(Config_editor);
            services.AddSingleton<IETL>(eTL);
            services.AddSingleton<IWorkFlowEditor>(WorkFlowEditor);
            services.AddSingleton<IAssemblyHandler>(LLoader);
            services.AddSingleton<IClassCreator>(classCreator);
            services.AddSingleton<IDataTypesHelper>(typesHelper);

            services.AddScoped<IDMEEditor, DMEEditor>();
        }
        #region "Init Config"
        private bool CreateBeepEnv()
        {
            try
            {
                lg = new DMLogger();
                jsonLoader = new JsonLoader();
                util = new Util(lg, Erinfo, Config_editor);
                Erinfo = new ErrorsInfo(lg);
                Config_editor = new ConfigEditor(lg, Erinfo, jsonLoader);
                eTL = new ETL();
                WorkFlowEditor = new WorkFlowEditor();
                typesHelper = new DataTypesHelper();
                classCreator = new ClassCreatorv2();
                LLoader = new AssemblyHandlerCore(Config_editor, Erinfo, lg, util);
                LLoader.LoadAllAssembly();
                Config_editor.LoadedAssemblies = LLoader.Assemblies.Select(c => c.DllLib).ToList();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion
       public IDataSource OpenKocConnection()
        {
            try
            {
                string connname = $"Koc_{DataSourceType.Oracle.ToString()}";
                AddKocConnection(connname, DataSourceType.Oracle);
                src = DMEEditor.GetDataSource(connname);
                src.Openconnection();
                return src;
            }
            catch (Exception ex)
            {
                return null;

            }
        }
        public bool AddKocConnection(string pConnectionName, DataSourceType dataSourceType)
        {
            bool retval = true;
            ConnectionProperties connection;
            ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections.Where(o => o.ConnectionName.Equals(pConnectionName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            DMEEditor.ConfigEditor.DataConnections.Remove(cn);
            ConnectionDriversConfig configdr = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.DatasourceType == dataSourceType).FirstOrDefault();
            if (!DMEEditor.ConfigEditor.DataConnections.Where(o => o.ConnectionName.Equals(pConnectionName, StringComparison.OrdinalIgnoreCase)).Any())
            {
                //ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections.Where(o => o.ConnectionName.Equals(pConnectionName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                //DMEEditor.ConfigEditor.DataConnections.Remove(cn);
                if (configdr != null)
                {
                    connection = new ConnectionProperties()
                    {
                        Category = DatasourceCategory.RDBMS,
                        ConnectionName = pConnectionName,
                        Database = "xe",
                        UserID = "kocuser",
                        Password = "xyz",
                        Host = "localhost",
                        Port = 1521,
                        DatabaseType = dataSourceType,
                        ConnectionString = configdr.ConnectionString,
                        DriverName = configdr.DriverClass,
                        DriverVersion = configdr.version
                    };
                    DMEEditor.ConfigEditor.AddDataConnection(connection);
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                }
                else
                    retval = false;
            }
            return retval;
        }
    }
}
