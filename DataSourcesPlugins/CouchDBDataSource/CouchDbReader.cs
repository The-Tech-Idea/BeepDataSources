
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace  TheTechIdea.Beep.NOSQL.CouchDB
{
    public class CouchDBReader : WebApiHeader
    {
        public CouchDBReader(string datasourcename, string databasename, IDMEEditor pDMEEditor, IDataConnection pConn, List<EntityField> pfields = null) : base(datasourcename,  databasename,pDMEEditor, pConn, pfields)
        {
            
            
        }

    }
}
