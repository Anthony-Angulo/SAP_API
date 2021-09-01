using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAP_API.Entities;
using SAP_API.Models;

namespace SAP_API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepartmentController : ControllerBase {

        // Atributtes
        private readonly ApplicationDbContext _context;

        // Constructor
        public DepartmentController(ApplicationDbContext context) {
            _context = context;
        }

        /// <summary>
        /// Get Department List From External Database.
        /// </summary>
        /// <returns>Department List</returns>
        /// <response code="200">Department List </response>
        // GET: api/Department
        [ProducesResponseType(typeof(Department[]), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> GetDepartments() {
            return Ok(_context.Departments);
        }

        /// <summary>
        /// Get Department Detail From External Database.
        /// </summary>
        /// <returns>Department Detail</returns>
        /// <response code="200">Department Detail</response>
        /// <response code="204">Department not Found</response>
        // GET: api/Department/:id
        [ProducesResponseType(typeof(Department), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartment(int id) {
            Department department = _context.Departments.Find(id);
            if (department == null) {
                return NoContent();
            }
            return Ok(department);
        }

        /// <summary>
        /// Register Department into External Database.
        /// </summary>
        /// <returns></returns>
        /// <response code="200"></response>
        /// <response code="400">Error Message</response>
        // POST: api/Department
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DepartmentDto value) {

            Department department = new Department {
                Name = value.Name
            };

            var result = _context.Departments.Add(department);

            if (result.State != EntityState.Added) {
                string Errors = "";
                return BadRequest(Errors);
            }

            try {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException) {
                return BadRequest("A database command did not affect the expected number of rows. This usually indicates an optimistic concurrency violation; that is, a row has been changed in the database since it was queried.");
            }
            catch (DbUpdateException) {
                return BadRequest("An error occurred sending updates to the database.");
            }
            //catch (DbEntityValidationException) {
            //    return BadRequest("The save was aborted because validation of entity property values failed.");
            //}
            catch (NotSupportedException) {
                return BadRequest("An attempt was made to use unsupported behavior such as executing multiple asynchronous commands concurrently on the same context instance.");
            }
            catch (ObjectDisposedException) {
                return BadRequest("The context or connection have been disposed.");
            }
            catch (InvalidOperationException) {
                return BadRequest("Some error occurred attempting to process entities in the context either before or after sending commands to the database.");
            }

            return Ok();
        }

        /// <summary>
        /// Edit Department From External Database.
        /// </summary>
        /// <returns></returns>
        /// <response code="200"></response>
        /// <response code="204">Department No Found</response>
        /// <response code="400">Error Message</response>
        // PUT: api/Department/:id
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] DepartmentDto value) {
            
            Department department = _context.Departments.Find(id);

            if (department == null) {
                return NoContent();
            }

            if (department.Name == value.Name) {
                string Errors = "No Hay Cambios que Realizar";
                return BadRequest(Errors);
            }

            department.Name = value.Name;

            var result = _context.Departments.Update(department);

            if (result.State == EntityState.Detached) {
                string Errors = "No Exite Esta Sucursal";
                return BadRequest(Errors);
            }

            if (result.State == EntityState.Unchanged) {
                string Errors = "No Hay Cambios que Realizar";
                return BadRequest(Errors);
            }

            if (result.State != EntityState.Modified) {
                string Errors = "";
                return BadRequest(Errors);
            }

            try {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException) {
                return BadRequest("A database command did not affect the expected number of rows. This usually indicates an optimistic concurrency violation; that is, a row has been changed in the database since it was queried.");
            }
            catch (DbUpdateException) {
                return BadRequest("An error occurred sending updates to the database.");
            }
            //catch (DbEntityValidationException) {
            //    return BadRequest("The save was aborted because validation of entity property values failed.");
            //}
            catch (NotSupportedException) {
                return BadRequest("An attempt was made to use unsupported behavior such as executing multiple asynchronous commands concurrently on the same context instance.");
            }
            catch (ObjectDisposedException) {
                return BadRequest("The context or connection have been disposed.");
            }
            catch (InvalidOperationException) {
                return BadRequest("Some error occurred attempting to process entities in the context either before or after sending commands to the database.");
            }

            return Ok();
        }
    }
}
