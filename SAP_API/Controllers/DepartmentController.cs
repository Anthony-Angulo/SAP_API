using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DepartmentController : ControllerBase {

        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context) {
            _context = context;
        }

        // GET: api/Department
        [Authorize]
        [HttpGet]
        public IEnumerable<Department> Get() {
            return _context.Departments;
        }

        // GET: api/Department/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id) {
            Department department = _context.Departments.Find(id);
            if (department != null) {
                return Ok(department);
            }
            return NotFound();
        }

        // POST: api/Department
        [Authorize]
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

        // PUT: api/Department/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] DepartmentDto value) {
            Department department = _context.Departments.Find(id);
            if (department == null) {
                return BadRequest("No Exite Esa Sucursal");
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
