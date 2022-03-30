using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
 
using BeepWebApiCore.Helpers;
using BeepWebApiCore.Others;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Containers.Services;
using TheTechIdea.Beep.Containers.UserManagement;

namespace BeepBlazor.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataSourceController : ControllerBase
    {
        private IBeepDMService DMService;
        private IDMEEditor _dMEEditor;
        private List<object> ls;
        private int PageSize = 10;
        private IUriService uriService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        public DataSourceController(IBeepDMService pDMService)
        {
            DMService = pDMService;
             _dMEEditor = DMService.DMEEditor;
         
          
        }
        [HttpGet("GetData/{datasource}/{entity}")]
        public IActionResult GetData(string datasource, string entity, [FromQuery] PaginationFilter filter)
        {


            IDataSource ds = _dMEEditor.GetDataSource(datasource);
            if (ds != null)
            {
                ds.Openconnection();

                if (ds.ConnectionStatus == System.Data.ConnectionState.Open)
                {
                    var route = Request.Path.Value;
                    var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
                    GetlsDataPages(datasource, entity);


                    if (ls != null)
                    {
                        if (ls.Count() > 0)
                        {
                            var pagedData = ls.Skip((validFilter.PageNumber - 1) * validFilter.PageSize).Take(validFilter.PageSize).ToList();
                            var totalRecords = ls.Count();
                            var pagedReponse = PaginationHelper.CreatePagedReponse<List<object>>(pagedData, validFilter, totalRecords, uriService, route);
                            return Ok(pagedReponse);

                            // retval = GetPage(ls, pageid, PageSize);
                            //   return retval;
                        }
                    }
                    return Ok("No Records Retrieved");
                    ds.Closeconnection();
                }
                else
                    return Ok("Connection to Datasource not Established");
            }
            else
                return Ok("Cannot Find DataSource Definition or Connection Properties"); ;

        }
        private int GetlsDataPages(string datasource, string entity)
        {
            IDataSource ds = _dMEEditor.GetDataSource(datasource);
            if (ds != null)
            {
                ds.Openconnection();
                if (ds.ConnectionStatus == System.Data.ConnectionState.Open)
                {
                    var t = Task.Run<object>(() => { return ds.GetEntity(entity, null); });
                    t.Wait();
                    ls = (List<object>)t.Result;
                    if (ls != null)
                    {
                        if (ls.Count() > 0)
                        {
                            return ls.Count() / PageSize;
                        }
                        else
                            return 0;
                    }
                    else
                        return -1;

                }
                else
                    return -1;
            }
            else
                return -1;
        }
        IEnumerable<object> GetPage(IEnumerable<object> input, int page, int pagesize)
        {
            return input.Skip(page * pagesize).Take(pagesize);
        }
      
    }
}
