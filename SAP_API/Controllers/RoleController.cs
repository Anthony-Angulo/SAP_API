using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Entities;
using SAP_API.Models;

namespace SAP_API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase {

        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleController(ApplicationDbContext context, RoleManager<IdentityRole> roleManager) {
            _roleManager = roleManager;
            _context = context;
        }

        // GET: api/Role
        [Authorize]
        [HttpGet]
        public IEnumerable<IdentityRole> Get() {
            return _context.Roles;
        }

        // GET: api/Role/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id) {
            var Role = await _roleManager.FindByIdAsync(id);
            if (Role != null) {
                var claims = await _roleManager.GetClaimsAsync(Role);
                var PermissionsClaimList = claims.Where(x => x.Type == CustomClaimTypes.Permission);

                List<string> permissionValueList = PermissionsClaimList.Select(x => x.Value).ToList();
                var PermissionsList = Permission.Get(permissionValueList);

                Object result = new {
                    PermissionsList,
                    Name = Role.Name,
                    ID = Role.Id
                };
                return Ok(result);
            }
            return NotFound();
        }

        // POST: api/Role
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] RoleDto value) {

            await _roleManager.CreateAsync(new IdentityRole(value.Name));
            var adminRole = await _roleManager.FindByNameAsync(value.Name);

            foreach (string Permission in value.Permissions) {
                await _roleManager.AddClaimAsync(adminRole, new Claim(CustomClaimTypes.Permission, Permission));
            }

            return Ok();
        }

        // PUT: api/Warehouse/5
        [Authorize]
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
