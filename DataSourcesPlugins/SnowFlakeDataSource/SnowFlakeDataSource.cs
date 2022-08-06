using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SnowFlake)]
    public class SnowFlakeDataSource :  RDBSource
    {

        public SnowFlakeDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            ColumnDelimiter = "'";
            ParameterDelimiter = "()";

        }
        public override string ColumnDelimiter { get; set; } = "'";
        public override string ParameterDelimiter { get; set; } = ":";
        public override  string GetInsertString(string EntityName, EntityStructure DataStruct)
        {
            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();
            // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
            //   map= Mapping.FldMapping;
            //    EntityName = Regex.Replace(EntityName, @"\s+", "");
            string Insertstr = "insert into " + EntityName + " (";
            Insertstr = GetTableName(Insertstr.ToLower());
            string Valuestr = ") values (";
            var insertfieldname = "";
            // string datafieldname = "";
            string typefield = "";
            int i = DataStruct.Fields.Count();
            int t = 0;
            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {
                Insertstr += $"{GetFieldName(item.fieldname)},";
                Valuestr += $"{ParameterDelimiter}p_" + Regex.Replace(item.fieldname, @"\s+", "_") + ",";

                t += 1;
            }
            Insertstr = Insertstr.Remove(Insertstr.Length - 1);
            Valuestr = Valuestr.Remove(Valuestr.Length - 1);
            Valuestr += ")";
            return Insertstr + Valuestr;
        }
        public override string GetUpdateString(string EntityName, EntityStructure DataStruct)
        {
            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();

            string Updatestr = @"Update " + EntityName + "  set " + Environment.NewLine;
            Updatestr = GetTableName(Updatestr.ToLower());
            string Valuestr = "";

            int i = DataStruct.Fields.Count();
            int t = 0;
            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {
                if (!DataStruct.PrimaryKeys.Any(l => l.fieldname == item.fieldname))
                {
                    Updatestr += $"{GetFieldName(item.fieldname)}= ";
                    Updatestr += $"{ParameterDelimiter}p_" + item.fieldname + ",";
                }
                t += 1;
            }

            Updatestr = Updatestr.Remove(Updatestr.Length - 1);

            Updatestr += @" where " + Environment.NewLine;
            i = DataStruct.PrimaryKeys.Count();
            t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys)
            {

                if (t == 1)
                {
                    Updatestr += $"{GetFieldName(item.fieldname)}= ";
                }
                else
                {
                    Updatestr += $" and {GetFieldName(item.fieldname)}= ";
                }
                Updatestr += $"{ParameterDelimiter}p_" + item.fieldname + "";

                t += 1;
            }
            //  Updatestr = Updatestr.Remove(Valuestr.Length - 1);
            return Updatestr;
        }
        public override string GetDeleteString(string EntityName, EntityStructure DataStruct)
        {

            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();
            string Updatestr = @"Delete from " + EntityName + "  ";
            Updatestr = GetTableName(Updatestr.ToLower());
            int i = DataStruct.Fields.Count();
            int t = 0;
            Updatestr += @" where ";
            i = DataStruct.PrimaryKeys.Count();
            t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys)
            {

                if (t == 1)
                {
                    Updatestr += $"{GetFieldName(item.fieldname)}= ";
                }
                else
                {
                    Updatestr += $" and  {GetFieldName(item.fieldname)}= ";
                }
                Updatestr += $"{ParameterDelimiter}p_" + item.fieldname + "";
                t += 1;
            }
            return Updatestr;
        }

    }
}
