using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Models;

namespace SAP_API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase {
        // GET: api/Permission
        [Authorize]
        [HttpGet]
        public IEnumerable<Permission.PermissionsOutput> Get() {
            List<Permission.PermissionsOutput> permissions = Permission.Get();
            return permissions;
        }
    }
}
