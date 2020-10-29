using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Entities;
using SAP_API.Models;

namespace SAP_API.Controllers {

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase {

        // Atributtes
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        // Constructor
        public RoleController(ApplicationDbContext context,
                              RoleManager<IdentityRole> roleManager) {
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// Get Role List From External Database.
        /// </summary>
        /// <returns>Role List</returns>
        /// <response code="200">Role List </response>
        // GET: api/Role
        [ProducesResponseType(typeof(IdentityRole[]), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> Get() {
            return Ok(_context.Roles);
        }

        class RoleOutput {
            public string Name { get; set; }
            public string ID { get; set; }
            public List<Permission.PermissionsOutput> PermissionsList { get; set; }
        }

        /// <summary>
        /// Get Role Detail From External Database.
        /// </summary>
        /// <returns>Role Detail</returns>
        /// <response code="200">Role Detail</response>
        /// <response code="204">Role not Found</response>
        // GET: api/Role/:id
        [ProducesResponseType(typeof(RoleOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id) {
            
            var Role = await _roleManager.FindByIdAsync(id);

            if (Role == null) {
                return NoContent();
            }

            var claims = await _roleManager.GetClaimsAsync(Role);
            var PermissionsClaimList = claims.Where(x => x.Type == CustomClaimTypes.Permission);

            List<string> permissionValueList = PermissionsClaimList.Select(x => x.Value).ToList();
            var PermissionsList = Permission.Get(permissionValueList);

            RoleOutput output = new RoleOutput {
                PermissionsList = PermissionsList,
                Name = Role.Name,
                ID = Role.Id
            };

            return Ok(output);

        }

        /// <summary>
        /// Register Role into External Database.
        /// </summary>
        /// <returns></returns>
        /// <response code="200"></response>
        // POST: api/Role
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] RoleDto value) {

            await _roleManager.CreateAsync(new IdentityRole(value.Name));
            var adminRole = await _roleManager.FindByNameAsync(value.Name);

            foreach (string Permission in value.Permissions) {
                await _roleManager.AddClaimAsync(adminRole, new Claim(CustomClaimTypes.Permission, Permission));
            }

            return Ok();
        }

        /// <summary>
        /// Edit Role From External Database.
        /// </summary>
        /// <returns></returns>
        /// <response code="200"></response>
        // PUT: api/Role/:id
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] RoleDto value) {

            var role = await _roleManager.FindByIdAsync(id);

            if (role.Name != value.Name) {
                await _roleManager.SetRoleNameAsync(role, value.Name);
                await _roleManager.UpdateAsync(role);
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            var Permissions = claims.Where(x => x.Type == CustomClaimTypes.Permission);

            foreach (var permission in Permissions) {
                if (!value.Permissions.Exists(x => x == permission.Value)) {
                    await _roleManager.RemoveClaimAsync(role, permission);
                }
            }

            var PermissionList = Permissions.ToList();
            foreach (var permission in value.Permissions) {
                if (!PermissionList.Exists(x => x.Value == permission)) {
                    await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));
                }
            }

            return Ok();
        }

    }
}
