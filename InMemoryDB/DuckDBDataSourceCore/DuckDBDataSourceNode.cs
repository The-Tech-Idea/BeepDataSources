using Beep.Vis.Module;
using DataManagementModels.DataBase;
using System;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Util;
using DataManagementModels.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using DuckDBDataSourceCore;
namespace Beep.InMemory.Nodes
{
    [AddinAttribute(Caption = "InMemory", Name = "DuckDBDataSourceNode.Beep", misc = "Beep", iconimage = "duckdb.png", menu = "Beep", ObjectType = "Beep")]
    [AddinVisSchema(BranchType = EnumPointType.DataPoint, BranchClass = "DuckDB.INMEMORY")]
    public class DuckDBDataSourceNode : IBranch, IOrder
    {
        public DuckDBDataSourceNode()
        {
                
        }
        
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string ParentGuidID { get; set; }
        public string DataSourceConnectionGuidID { get; set; }
        public string EntityGuidID { get; set; }
        public string MiscStringID { get; set; }
        #region "Properties"
        public IBranch ParentBranch { get; set; }
        public string ObjectType { get; set; } = "Beep";
        public int ID { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public int Order { get; set; } = 4;
        public string Name { get; set; }
        public string BranchText { get; set; } = "DuckDB InMemory";
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; } = EnumPointType.DataPoint;
        public int BranchID { get; set; }
        public string IconImageName { get; set; } = "duckdb.png";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "DuckDB.INMEMORY";
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; }
        public object TreeStrucure { get; set; }
        public IVisManager Visutil { get; set; }
        public int MiscID { get; set; }
        public IInMemoryDB memoryDB { get; set; }
        DuckDBDataSource DuckDBDataSource { get; set; }
        public ConnectionProperties ConnectionProperties { get; set; }
        public bool IsDataSourceNode { get; set; } = false;

        // public event EventHandler<PassedArgs> BranchSelected;
        // public event EventHandler<PassedArgs> BranchDragEnter;
        // public event EventHandler<PassedArgs> BranchDragDrop;
        // public event EventHandler<PassedArgs> BranchDragLeave;
        // public event EventHandler<PassedArgs> BranchDragClick;
        // public event EventHandler<PassedArgs> BranchDragDoubleClick;
        // public event EventHandler<PassedArgs> ActionNeeded;
        #endregion "Properties"
        #region "Interface Methods"
        public IErrorsInfo CreateChildNodes()
        {
            try
            {
                DataSourceDefaultMethods.GetEntities(this, DMEEditor, Visutil);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo ExecuteBranchAction(string ActionName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo MenuItemClicked(string ActionNam)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RemoveChildNodes()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SetConfig(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename)
        {
            try
            {
                TreeEditor = pTreeEditor;
                DMEEditor = pDMEEditor;
                //ParentBranchID = pParentNode.ID;
                //BranchText = pBranchText;
                //BranchType = pBranchType;
                //IconImageName = pimagename;
                //if (pID != 0)
                //{
                //    ID = pID;
                //}

                //   DMEEditor.AddLogMessage("Success", "Set Config OK", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Set Config";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion "Interface Methods"
        #region "Exposed Interface"
        [CommandAttribute(Caption = "Get Entities", Hidden = false, iconimage = "getentities.png")]
        public IErrorsInfo GetEntities()
        {

            try
            {
                if(memoryDB==null)
                {
                    memoryDB = DMEEditor.GetDataSource(DataSourceName) as IInMemoryDB;
                }
                if(memoryDB==null)
                {
                    DMEEditor.AddLogMessage("Error", "Could not Get InMemory Database", DateTime.Now, -1, "Error", Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                DuckDBDataSource= memoryDB as DuckDBDataSource;
                if (memoryDB.IsStructureCreated==false)
                {
                    PassedArgs args=new PassedArgs();  
                    CancellationToken token = new CancellationToken();
                    args.Messege= $"Loadin InMemory Structure {DataSourceName}";
                    Visutil.ShowWaitForm(args);
                    Visutil.PasstoWaitForm(args);
                    var progress = new Progress<PassedArgs>(percent =>
                    {
                       
                        if(!string.IsNullOrEmpty(percent.Messege))
                        {
                            Visutil.PasstoWaitForm(percent);
                        }
                        if (percent.EventType == "Stop")
                        {
                                token.ThrowIfCancellationRequested();   
                            }
                        
                    });
                   
            
                   
                    memoryDB.LoadStructure(progress, token);
                    memoryDB.CreateStructure(progress, token);
                    //if(memoryDB.IsStructureCreated==true)
                    //{
                    //    args.Messege = $"Loading InMemory Data {DataSourceName}";
                    //    Visutil.PasstoWaitForm(args);
                    //    memoryDB.LoadData(progress, token);
                    //    memoryDB.IsLoaded = true;
                    //}
                 
                   
                }
                if(memoryDB.IsLoaded == true || memoryDB.InMemoryStructures.Count>0)
                {
                    DataSourceDefaultMethods.GetEntities(this, DMEEditor, Visutil);
                }
                Visutil.CloseWaitForm();
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Refresh Data", Hidden = false, iconimage = "refresh.png")]
        public IErrorsInfo RefreshData()
        {

            try
            {
                CancellationToken token = new CancellationToken();
                PassedArgs args = new PassedArgs();
                var progress = new Progress<PassedArgs>(percent =>
                {

                    if (!string.IsNullOrEmpty(percent.Messege))
                    {
                        Visutil.PasstoWaitForm(percent);
                    }
                    if (percent.EventType == "Stop")
                    {
                        token.ThrowIfCancellationRequested();
                    }

                });
              
                if (memoryDB == null)
                {
                    memoryDB = DMEEditor.GetDataSource(DataSourceName) as IInMemoryDB;
                }
                if (memoryDB == null)
                {
                    DMEEditor.AddLogMessage("Error", "Could not Get InMemory Database", DateTime.Now, -1, "Error", Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                if (memoryDB.IsLoaded )
                {
                    var retval =Visutil.Controlmanager.InputBoxYesNo("Warning", "This will refresh the data in memory, Do you want to continue?");
                    if (retval == DialogResult.Yes)
                    {
                        args.Messege = $"Loadin Data in InMemory  {DataSourceName}";
                        Visutil.ShowWaitForm(args);
                        Visutil.PasstoWaitForm(args);
                        memoryDB.RefreshData(progress, token);
                        Visutil.CloseWaitForm();
                    }
                   
                }
                   

              
              
               
            }
            catch (Exception ex)
            {
                Visutil.CloseWaitForm();
                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Remove all", Hidden = false, iconimage = "remove.png")]
        public IErrorsInfo remove()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }

        public IBranch CreateCategoryNode(CategoryFolder p)
        {
            return this;
        }
        #endregion Exposed Interface"
        #region "Other Methods"

        [CommandAttribute(Caption = "Import CSV", Hidden = false, iconimage = "csv.png")]
        public IErrorsInfo ImportCSV()
        {

            try
            {
                bool fileloaded = false;    
                if (memoryDB == null)
                {
                    memoryDB = DMEEditor.GetDataSource(DataSourceName) as IInMemoryDB;
                }
                if (memoryDB == null)
                {
                    DMEEditor.AddLogMessage("Error", "Could not Get InMemory Database", DateTime.Now, -1, "Error", Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                DuckDBDataSource = memoryDB as DuckDBDataSource;
                DuckDBDataSource.Openconnection();
                       PassedArgs args = new PassedArgs();
                    CancellationToken token = new CancellationToken();
                  
                    var progress = new Progress<PassedArgs>(percent =>
                    {

                        if (!string.IsNullOrEmpty(percent.Messege))
                        {
                            Visutil.PasstoWaitForm(percent);
                        }
                        if (percent.EventType == "Stop")
                        {
                            token.ThrowIfCancellationRequested();
                        }

                    });
                    string filepath=Visutil.Controlmanager.LoadFileDialog("CSV Files (*.csv)|*.csv|All files (*.*)|*.*", "Select CSV File",null);
                    if (!string.IsNullOrEmpty(filepath))
                    {
                        string tablename = "";
                        string filename= System.IO.Path.GetFileName(filepath);
                        Visutil.Controlmanager.InputBox("Enter Table Name", "Table Name", ref tablename);
                        if(!string.IsNullOrEmpty(tablename))
                        {
                            args.Messege = $"Loadin CSV File {tablename}";
                            Visutil.ShowWaitForm(args);
                            Visutil.PasstoWaitForm(args);
                            try
                            {
                                DuckDBDataSource.ImportCSV(filepath, tablename);
                                fileloaded = true;
                            }
                            catch (Exception ex)
                            {
                                DMEEditor.AddLogMessage("Beep",$"Error: Could not Load CSV file {filename} {ex.Message}", DateTime.Now, -1, "Error", Errors.Failed);

                            }
                         

                        }
                        
                  

                


                }
                if (fileloaded)
                {
                    DataSourceDefaultMethods.GetEntities(this, DMEEditor, Visutil);
                }
                Visutil.CloseWaitForm();
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion"Other Methods"
    }
}
