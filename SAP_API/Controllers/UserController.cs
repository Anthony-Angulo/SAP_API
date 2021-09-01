using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Entities;
using SAP_API.Models;
using static SAP_API.Controllers.AccountController;
using static SAP_API.Models.Permission;

namespace SAP_API.Controllers {

    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase {

        // Atributtes
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // Constructor
        public UserController(ApplicationDbContext context,
                              UserManager<User> userManager,
                              RoleManager<IdentityRole> roleManager) {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        class UserOutput {
            public string Id { get; set; }
            public string LastName { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Department { get; set; }
            public string Warehouse { get; set; }
        }
        /// <summary>
        /// Get User List From External Database.
        /// </summary>
        /// <returns>User List</returns>
        /// <response code="200">User List </response>
        // GET: api/User
        [ProducesResponseType(typeof(UserOutput[]), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> Get() {
            List<UserOutput> output = _context.Users.Select(user =>
                new UserOutput {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    LastName = user.LastName,
                    Department = user.Department.Name,
                    Warehouse = user.Warehouse.WhsName,
                }
            ).ToList();
            return Ok(output);
        }

       public class UserDetailOutput {
            public string Id { get; set; }
            public string Email { get; set; }
            public string Name { get; set; }
            public string LastName { get; set; }
            public bool Active { get; set; }
            public int SAPID { get; set; }
            public string Department { get; set; }
            public string Warehouse { get; set; }
            public string Role { get; set; }
            public List<PermissionsOutput> RolePermissions { get; set; }
            public List<PermissionsOutput> PermissionsExtra { get; set; }
        }
        // TODO: Change Department and Warehouse "Get"
        /// <summary>
        /// Get User Detail From External Database.
        /// </summary>
        /// <returns>User Detail</returns>
        /// <response code="200">User Detail</response>
        /// <response code="204">User not Found</response>
        // GET: api/User/:id
        [ProducesResponseType(typeof(UserDetailOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id) {

            // TODO: Change This.
            var user1 = _context.Users.Where(User => User.Id == id).Select(User => (new { Department = User.Department.Name, Warehouse = User.Warehouse.WhsName }));


            User user = await _userManager.FindByIdAsync(id);

            if (user == null) {
                return NoContent();
            }

            IList<string> userRoleNames = await _userManager.GetRolesAsync(user);

            List<PermissionsOutput> RolePermissions = new List<PermissionsOutput>();

            if (userRoleNames.Count != 0) {
                IQueryable<IdentityRole> userRoles = _roleManager.Roles.Where(x => userRoleNames.Contains(x.Name));

                IList<Claim> roleClaims = await _roleManager.GetClaimsAsync(userRoles.FirstOrDefault());

                IEnumerable<string> PermissionsClaims = roleClaims.Where(x => x.Type == CustomClaimTypes.Permission).Select(x => x.Value);

                RolePermissions = Permission.Get(PermissionsClaims.ToList());
            }

            IList<Claim> userClaims = await _userManager.GetClaimsAsync(user);
            List<PermissionsOutput> PermissionsExtra = new List<PermissionsOutput>();
            if (userClaims.Count != 0) {
                IEnumerable<string> PermissionsExtraClaims = userClaims.Where(x => x.Type == CustomClaimTypes.Permission).Select(x => x.Value);
                PermissionsExtra = Permission.Get(PermissionsExtraClaims.ToList());
            }

            UserDetailOutput result = new UserDetailOutput {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                LastName = user.LastName,
                Active = user.Active,
                SAPID = user.SAPID,
                Department = user1.FirstOrDefault().Department,
                Warehouse = user1.FirstOrDefault().Warehouse,
                Role = userRoleNames.FirstOrDefault(),
                RolePermissions = RolePermissions,
                PermissionsExtra = PermissionsExtra,
            };

            return Ok(result);

        }

        /// <summary>
        /// Edit User From External Database.
        /// </summary>
        /// <returns></returns>
        /// <response code="200">User Updated</response>
        /// <response code="204">User No Found</response>
        /// <response code="400">Error Message</response>
        // PUT: api/User/:id
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] EditDto model) {

            User user = await _userManager.FindByIdAsync(id);

            if (user == null) {
                return NoContent();
            }

            Warehouse warehouse = _context.Warehouses.Find(model.Warehouse);
            if (warehouse == null) {
                return BadRequest("No Warehouse");
            }
            Department department = _context.Departments.Find(model.Department);
            if (department == null) {
                return BadRequest("No Departamento");
            }

            IdentityRole Role = await _roleManager.FindByIdAsync(model.Role);
            if (Role == null) {
                return BadRequest("NO ROL");
            }

            user.UserName = model.Email;
            user.Email = model.Email;
            user.Name = model.Name;
            user.LastName = model.LastName;
            user.SAPID = model.SAPID;
            user.Active = model.Active;
            user.Warehouse = warehouse;
            user.Department = department;

            var result = await _userManager.UpdateAsync(user);

            if (model.Password != null) {
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, model.Password);
            }

            if (result.Succeeded) {

                IList<string> userRoleNames = await _userManager.GetRolesAsync(user);
                string ActualRole = userRoleNames.FirstOrDefault();
                if (Role.Name != ActualRole) {
                    if (ActualRole != null) {
                        await _userManager.RemoveFromRoleAsync(user, ActualRole);
                    }
                    await _userManager.AddToRoleAsync(user, Role.Name);
                }

                var UserClaims = await _userManager.GetClaimsAsync(user);
                var Permissions = UserClaims.Where(x => x.Type == CustomClaimTypes.Permission);

                foreach (var permission in Permissions) {
                    if (!model.PermissionsExtra.Exists(x => x == permission.Value)) {
                        await _userManager.RemoveClaimAsync(user, permission);
                    }
                }

                var PermissionList = Permissions.ToList();
                foreach (var permission in model.PermissionsExtra) {
                    if (!PermissionList.Exists(x => x.Value == permission)) {
                        await _userManager.AddClaimAsync(user, new Claim(CustomClaimTypes.Permission, permission));
                    }
                }

                return Ok();
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (IdentityError m in result.Errors.ToList()) {
                stringBuilder.AppendFormat("Codigo: {0} Descripcion: {1}\n", m.Code, m.Description);
            }
            return BadRequest(stringBuilder.ToString());

        }
    }
}
