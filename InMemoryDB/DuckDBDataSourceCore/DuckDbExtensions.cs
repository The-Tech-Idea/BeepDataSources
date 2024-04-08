using System;
using System.Linq;
using Beep.Vis.Module;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Util;


namespace TheTechIdea.Beep
{
    [AddinAttribute(Caption = "DuckDb Menu", Name = "DuckDbExtensions", misc = "IFunctionExtension", menu = "Beep", ObjectType = "Beep", addinType = AddinType.Class, iconimage = "duckdb.png", order = 3, Showin = ShowinType.Menu)]
    public  class DuckDbExtensions :IFunctionExtension
    {
        public IDMEEditor DMEEditor { get  ; set  ; }
        public IPassedArgs Passedargs { get  ; set  ; }

       
    }
}
