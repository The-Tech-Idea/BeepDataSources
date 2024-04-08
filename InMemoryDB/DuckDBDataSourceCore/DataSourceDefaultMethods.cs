using Beep.Vis.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;

using TheTechIdea.Beep.Vis;
using TheTechIdea.Util;
using DuckDBDataSourceCore;


namespace TheTechIdea.Beep
{
    public static class DataSourceDefaultMethods
    {
        public static IErrorsInfo RefreshEntities(IBranch DatabaseBranch,IDMEEditor DMEEditor, IVisManager Visutil)
        {
            ITree tree = (ITree)Visutil.Tree;
            string BranchText = DatabaseBranch.BranchText;
            string DataSourceName = DatabaseBranch.DataSourceName;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            PassedArgs passedArgs = new PassedArgs { DatasourceName = BranchText };
            try
            {
                string iconimage;
                IDataSource DataSource = DMEEditor.GetDataSource(BranchText);
                if (DataSource != null)
                {

                    Visutil.ShowWaitForm(passedArgs);
                    DataSource.Openconnection();
                    if (DataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                        if (Visutil.Controlmanager.InputBoxYesNo("Beep DM", "Are you sure, this might take some time?") == DialogResult.Yes)
                        {
                            passedArgs.Messege = "Connection Successful";
                            Visutil.PasstoWaitForm(passedArgs);
                            passedArgs.Messege = "Getting Entities";
                            Visutil.PasstoWaitForm(passedArgs);
                            DataSource.Entities.Clear();
                            DataSource.GetEntitesList();
                            tree.treeBranchHandler.RemoveChildBranchs(DatabaseBranch);
                            int i = 0;
                            passedArgs.Messege = $"Getting {DataSource.EntitiesNames.Count} Entities";
                            Visutil.PasstoWaitForm(passedArgs);
                            foreach (string tb in DataSource.EntitiesNames)
                            {
                                if (!tree.Branches.Where(x => x.Name.Equals(tb, StringComparison.InvariantCultureIgnoreCase) && x.ParentBranchID==DatabaseBranch.ID).Any())
                                {
                                    //EntityStructure ent = DataSource.GetEntityStructure(tb, true);
                                    //if (ent.Created == false)
                                    //{
                                    //    iconimage = "entitynotcreated.png";
                                    //}
                                    //else
                                    //{
                                        iconimage = "databaseentities.png";
                                  //  }
                                    DuckDBDataSourceEntityNode dbent = new DuckDBDataSourceEntityNode(tree, DMEEditor, DatabaseBranch, tb, tree.SeqID, EnumPointType.Entity, iconimage, DataSource);
                                    dbent.DataSourceName = DataSource.DatasourceName;
                                    dbent.DataSource = DataSource;
                                    tree.treeBranchHandler.AddBranch(DatabaseBranch, dbent);
                                    i += 1;
                                }
                            }
                            passedArgs.Messege = "Done";
                            Visutil.PasstoWaitForm(passedArgs);
                            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new TheTechIdea.Beep.ConfigUtil.DatasourceEntities { datasourcename = DataSourceName, Entities = DataSource.Entities });
                        }
                    }
                    else
                    {
                        passedArgs.Messege = "Could not Open Connection";
                        Visutil.PasstoWaitForm(passedArgs);
                    }


                }

                Visutil.CloseWaitForm();


            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Connecting to DataSource ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                passedArgs.Messege = "Could not Open Connection";
                Visutil.PasstoWaitForm(passedArgs);
                Visutil.CloseWaitForm();
            }
            return DMEEditor.ErrorObject;
        }
        public static IErrorsInfo GetEntities(IBranch DatabaseBranch, IDMEEditor DMEEditor, IVisManager Visutil)
        {
            ITree tree = (ITree)Visutil.Tree;
            string BranchText = DatabaseBranch.BranchText;
            string DataSourceName= DatabaseBranch.DataSourceName;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            PassedArgs passedArgs = new PassedArgs { DatasourceName = BranchText };
            try
            {
                string iconimage;
                IDataSource DataSource = DMEEditor.GetDataSource(BranchText);
                if (DataSource != null)
                {

                    Visutil.ShowWaitForm(passedArgs);
                    if(DataSource.ConnectionStatus!= System.Data.ConnectionState.Open)
                    {
                        DataSource.Openconnection();
                        if (DataSource.Entities.Count == 0)
                        {
                            var ents = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(BranchText);
                            if (ents != null)
                            {
                                if (ents.Entities.Count > 0)
                                {
                                    DataSource.Entities = ents.Entities;
                                }
                            }

                        }
                    }

                       
                    if (DataSource.ConnectionStatus == System.Data.ConnectionState.Open)
                    {
                      

                        passedArgs.Messege = "Connection Successful";
                        Visutil.PasstoWaitForm(passedArgs);
                        passedArgs.Messege = "Getting Entities";
                        Visutil.PasstoWaitForm(passedArgs);
                        DataSource.GetEntitesList();
                        int i = 0;
                        EntityStructure ent;
                        passedArgs.Messege = $"Getting {DataSource.EntitiesNames.Count} Entities";
                        Visutil.PasstoWaitForm(passedArgs);
                        foreach (string tb in DataSource.EntitiesNames)
                        {
                            if (!tree.Branches.Any(x => x.BranchText!=null && x.BranchText.Equals(tb, StringComparison.InvariantCultureIgnoreCase) && x.ParentBranchID == DatabaseBranch.ID))
                            {
                                //if (!DataSource.Entities.Any(p => p.EntityName.Equals(tb, StringComparison.InvariantCultureIgnoreCase)))
                                //{
                                //    ent = DataSource.GetEntityStructure(tb, true);
                                //}
                                //else
                                //{
                                //    ent = DataSource.GetEntityStructure(tb, false);
                                //}
                                
                                //if (ent.Created == false)
                                //{
                                //    iconimage = "entitynotcreated.png";
                                //}
                                //else
                                //{
                                    iconimage = "databaseentities.png";
                              //  }
                                DuckDBDataSourceEntityNode dbent = new DuckDBDataSourceEntityNode(tree, DMEEditor, DatabaseBranch, tb, tree.SeqID, EnumPointType.Entity, iconimage, DataSource);
                                dbent.DataSourceName = DataSource.DatasourceName;
                                dbent.DataSource = DataSource;
                                tree.treeBranchHandler.AddBranch(DatabaseBranch, dbent);
                                i += 1;
                            }
                        }
                        passedArgs.Messege = "Done";
                        Visutil.PasstoWaitForm(passedArgs);
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new TheTechIdea.Beep.ConfigUtil.DatasourceEntities { datasourcename = DataSourceName, Entities = DataSource.Entities });
                       
                    }
                    else
                    {
                        passedArgs.Messege = "Could not Open Connection";
                        Visutil.PasstoWaitForm(passedArgs);
                    }


                }

                Visutil.CloseWaitForm();


            }
            catch (Exception ex)
            {
                DMEEditor.Logger.WriteLog($"Error in Connecting to DataSource ({ex.Message}) ");
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                passedArgs.Messege = "Could not Open Connection";
                Visutil.PasstoWaitForm(passedArgs);
                Visutil.CloseWaitForm();
            }
            return DMEEditor.ErrorObject;
        }
    }
}
