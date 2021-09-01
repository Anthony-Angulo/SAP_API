using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using SAP_API.Entities;
using SAP_API.Models;
using static SAP_API.Controllers.UserController;

namespace SAP_API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {

        // Atributtes
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;

        // Constructor
        public AccountController(UserManager<User> userManager,
                                SignInManager<User> signInManager,
                                IConfiguration configuration,
                                ApplicationDbContext context,
                                RoleManager<IdentityRole> roleManager
                                )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// Get User Info. This route use the identification Token to know the user and redirect To /api/User/:UserID
        /// </summary>
        /// <returns>A Route Redirect</returns>
        // GET: api/Account
        [HttpGet("User")]
        public async Task<IActionResult> UserLog()
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            string authHeader = Request.Headers[HeaderNames.Authorization];
            authHeader = authHeader.Replace("Bearer ", "");
            JwtSecurityToken token = handler.ReadToken(authHeader) as JwtSecurityToken;
            Claim ClaimUserID = token.Claims.First(claim => claim.Type == "UserID");
            string UserID = ClaimUserID.Value;
            return Redirect($"/api/User/{UserID}");
        }

        class Token
        {
            public string token { get; set; }
        }
        /// <summary>
        /// Try to Login. Generate a Token.
        /// </summary>
        /// <param name="loginData">Login Data (Credentials)</param>
        /// <returns>A Identification Token</returns>
        /// <response code="200">Returns Identification Token</response>
        /// <response code="400">Login Failed</response>
        [ProducesResponseType(typeof(Token), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<object> Login([FromBody] LoginDto loginData)
        {

            var result = await _signInManager.PasswordSignInAsync(loginData.Email, loginData.Password, false, false);
            
            if (result.Succeeded)
            {
                var appUser = _userManager.Users.SingleOrDefault(r => r.Email == loginData.Email);
                var AppLogin = new AppUserLogin
                {
                    active = appUser.Active,
                    Email = appUser.Email,
                    id = appUser.Id,
                    SAPID = appUser.SAPID,
                    Name = appUser.Name,
                    User = appUser.UserName,
                    Active_Burn = appUser.Active_Burn,
                    Serie = appUser.Serie

                };
                var token = await GenerateJwtToken(loginData.Email, appUser);
                appUser.Warehouse = _context.Warehouses.Find(appUser.WarehouseID);
                //UserDetailOutput Roles = Redirect($"/api/User/{appUser.Id}");
                UserController userController = new UserController(_context, _userManager, _roleManager);
                 var Roles =  (ObjectResult) userController.Get(appUser.Id).Result;
                UserDetailOutput userDetail = (UserDetailOutput)Roles.Value;
                //Task<IActionResult> RolList = new RoleController(_context,_roleManager).Get(Roles.RoleId);
                return Ok(new { token, AppLogin, warehouseCode= appUser.Warehouse.WhsCode,userDetail.RolePermissions});
            }  
            return BadRequest("Error al Intentar Iniciar Sesion");
        }        
        public class AppUserLogin
        {
            public Boolean active { get; set; }
            public string Email { get; set; }
            public string id { get; set; }
            public string Name { get; set; }
            public string User { get; set; }
            public int SAPID { get; set; }
            public string Active_Burn { get; set; }
            public int Serie { get; set; }
        }
        /// <summary>
        /// Register User.
        /// </summary>
        /// <param name="register">Register Data</param>
        /// <returns></returns>
        /// <response code="200"></response>
        /// <response code="400">Register Failed</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto register)
        {

            Warehouse warehouse = _context.Warehouses.Find(register.Warehouse);
            if (warehouse == null)
            {
                return BadRequest("No Warehouse");
            }
            Department department = _context.Departments.Find(register.Department);
            if (department == null)
            {
                return BadRequest("No Departamento");
            }

            IdentityRole Role = await _roleManager.FindByIdAsync(register.Role);
            if (Role == null)
            {
                return BadRequest("NO ROL");
            }

            User user = new User
            {
                UserName = register.Email,
                Email = register.Email,
                Name = register.Name,
                LastName = register.LastName,
                SAPID = register.SAPID,
                Active = register.Active,
                Warehouse = warehouse,
                Department = department,
            };
            
                var result = await _userManager.CreateAsync(user, register.Password);

                if (result.Succeeded)
                {

                    await _userManager.AddToRoleAsync(user, Role.Name);

                    foreach (string Permission in register.PermissionsExtra)
                    {
                        await _userManager.AddClaimAsync(user, new Claim(CustomClaimTypes.Permission, Permission));
                    }

                    return Ok();
                }
                StringBuilder stringBuilder = new StringBuilder();
                foreach (IdentityError m in result.Errors.ToList())
                {
                    stringBuilder.AppendFormat("Codigo: {0} Descripcion: {1}\n", m.Code, m.Description);
                }
                return BadRequest(stringBuilder.ToString());
            }

        // Generate Identification Token
        private async Task<object> GenerateJwtToken(string email, User user)
        {

            var role = await _userManager.GetRolesAsync(user);
            IdentityOptions options = new IdentityOptions();

            List<Claim> claims = new List<Claim> {
                //new Claim(JwtRegisteredClaimNames.Sub, email),
                //new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                //new Claim(ClaimTypes.NameIdentifier, user.Id)
                new Claim("UserID", user.Id.ToString()),
             };

            string roles = role.FirstOrDefault();
            if (roles != null)
            {
                claims.Add(new Claim(options.ClaimsIdentity.RoleClaimType, role.FirstOrDefault()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtExpireDays"]));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                SigningCredentials = creds
            };

            //var token = new JwtSecurityToken(
            //    //_configuration["JwtIssuer"],
            //    //_configuration["JwtIssuer"],
            //    //claims,
            //    expires: expires,
            //    signingCredentials: creds
            //);

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(securityToken);
        }

        public class LoginDto
        {
            [Required]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }

        }

        public class RegisterDto
        {
            [Required]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "PASSWORD_MIN_LENGTH", MinimumLength = 6)]
            public string Password { get; set; }

            public List<string> PermissionsExtra { get; set; }
            public bool Active { get; set; }
            public int Department { get; set; }
            public string LastName { get; set; }
            public string Name { get; set; }
            public string Role { get; set; }
            public int Warehouse { get; set; }
            public int SAPID { get; set; }
        }

        public class EditDto
        {
            [Required]
            public string Email { get; set; }

            [StringLength(100, ErrorMessage = "PASSWORD_MIN_LENGTH", MinimumLength = 6)]
            public string Password { get; set; }

            public List<string> PermissionsExtra { get; set; }
            public bool Active { get; set; }
            public int Department { get; set; }
            public string LastName { get; set; }
            public string Name { get; set; }
            public string Role { get; set; }
            public int Warehouse { get; set; }
            public int SAPID { get; set; }
        }

    }
}
