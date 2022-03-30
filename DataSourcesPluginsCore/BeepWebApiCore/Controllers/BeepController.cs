using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using TheTechIdea.Beep;
using TheTechIdea.Beep.Containers.Services;

namespace BeepBlazor.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BeepController : Controller
    {
        private IBeepDMService DMService;
        public BeepController(IBeepDMService pDMService)
        {

            DMService = pDMService;




        }
        [HttpGet("GetConnections")]
        public IEnumerable<string> GetConnections()
        {
            return DMService.Configeditor.DataConnections.Select(p => p.ConnectionName).ToArray();
        }
        [HttpGet("GetDatasourcesClasses")]
        public IEnumerable<string> GetDatasourcesClasses()
        {
            return DMService.Configeditor.DataSourcesClasses.Select(p => p.className).ToArray();
        }
        [HttpGet("GetDataDrivers")]
        public IEnumerable<string> GetDataDrivers()
        {
            return DMService.Configeditor.DataDriversClasses.Select(p => p.DriverClass).ToArray();
        }
        [HttpGet("GetDataDriversDefinition")]
        public IEnumerable<string> GetDataDriversDefinition()
        {
            return DMService.Configeditor.DriverDefinitionsConfig.Select(p => p.DriverClass).ToArray();
        }


    }
}
