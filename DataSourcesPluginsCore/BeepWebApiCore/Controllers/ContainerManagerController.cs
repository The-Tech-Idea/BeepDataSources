using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Containers.ContainerManagement;
using TheTechIdea.Beep.Containers.Containers;
using TheTechIdea.Util;

namespace BeepBlazor.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ContainerManagerController : ControllerBase
    {
        private readonly ILogger<ContainerManagerController> _logger;
        private readonly ICantainerManager _containerManager;
        public ContainerManagerController(ICantainerManager containerManager, ILogger<ContainerManagerController> logger)
        {
            _logger = logger;
            _containerManager = containerManager;
        }
        [HttpGet("Get/{name}")]
        public ActionResult<BeepContainer> GetContainer(string name)
        {
            try
            {
                BeepContainer result = (BeepContainer)_containerManager.Containers.Where(p => p.ContainerName.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (result == null)
                {
                    return NotFound();
                }

                return result;
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error retrieving data from the database");
            }
        }
        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var t = Task.Run<IEnumerable<BeepContainer>>(() => _containerManager.Containers.AsEnumerable());
                return Ok(await t );

            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error retrieving data from the database");
            }
        }
        [HttpPost]
        public async Task<ActionResult<ErrorsInfo>>   CreateUpdateContainer([FromBody] BeepContainer beepcontainer)
        {
            try
            {
                if (beepcontainer == null)
                    return BadRequest();

                ErrorsInfo err = await _containerManager.AddUpdateContainer(beepcontainer);
                return err;
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error creating new Container");
            }
        }
    }
}
