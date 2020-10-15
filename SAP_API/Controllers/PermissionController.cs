using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Models;

namespace SAP_API.Controllers {

    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase {

        /// <summary>
        /// Get Permission List From External Database.
        /// </summary>
        /// <returns>Permission List</returns>
        /// <response code="200">Permission List </response>
        // GET: api/Permission
        [ProducesResponseType(typeof(Permission.PermissionsOutput[]), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> Get() {
            List<Permission.PermissionsOutput> permissions = Permission.Get();
            return Ok(permissions);
        }

    }
}
