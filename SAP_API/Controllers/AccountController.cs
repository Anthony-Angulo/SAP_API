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

namespace SAP_API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase {

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
                                ) {
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
        [Authorize]
        [HttpGet("User")]
        public async Task<IActionResult> UserLog() {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            string authHeader = Request.Headers[HeaderNames.Authorization];
            authHeader = authHeader.Replace("Bearer ", "");
            JwtSecurityToken token = handler.ReadToken(authHeader) as JwtSecurityToken;
            Claim ClaimUserID = token.Claims.First(claim => claim.Type == "UserID");
            string UserID = ClaimUserID.Value;
            return Redirect($"/api/User/{UserID}");
        }

        class Token {
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
        [HttpPost("Login")]
        public async Task<object> Login([FromBody] LoginDto loginData) {
            
            var result = await _signInManager.PasswordSignInAsync(loginData.Email, loginData.Password, false, false);

            if (result.Succeeded) {
                var appUser = _userManager.Users.SingleOrDefault(r => r.Email == loginData.Email);
                var token = await GenerateJwtToken(loginData.Email, appUser);
                return Ok(new { token });
            }
            return BadRequest("Error al Intentar Iniciar Sesion");
        }

        [Authorize]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model) {

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

            User user = new User {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                LastName = model.LastName,
                SAPID = model.SAPID,
                Active = model.Active,
                Warehouse = warehouse,
                Department = department,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded) {

                await _userManager.AddToRoleAsync(user, Role.Name);

                foreach (string Permission in model.PermissionsExtra) {
                    await _userManager.AddClaimAsync(user, new Claim(CustomClaimTypes.Permission, Permission));
                }

                return Ok();
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (IdentityError m in result.Errors.ToList()) {
                stringBuilder.AppendFormat("Codigo: {0} Descripcion: {1}\n", m.Code, m.Description);
            }
            return BadRequest(stringBuilder.ToString());
        }

        private async Task<object> GenerateJwtToken(string email, User user) {

            var role = await _userManager.GetRolesAsync(user);
            IdentityOptions options = new IdentityOptions();

            List<Claim> claims = new List<Claim> {
                //new Claim(JwtRegisteredClaimNames.Sub, email),
                //new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                //new Claim(ClaimTypes.NameIdentifier, user.Id)
                new Claim("UserID", user.Id.ToString()),
             };

            string roles = role.FirstOrDefault();
            if (roles != null) {
                claims.Add(new Claim(options.ClaimsIdentity.RoleClaimType, role.FirstOrDefault()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtExpireDays"]));

            var tokenDescriptor = new SecurityTokenDescriptor {
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

        public class LoginDto {
            [Required]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }

        }

        public class RegisterDto {
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

        public class EditDto {
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
