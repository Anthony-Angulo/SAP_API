using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ventasxubicacion : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ventasxubicacion(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Info")]
        public IActionResult Info()
        {
            return Ok("Controlador de ventas");
        }
        [HttpPost("Venta")]
        public async Task<IActionResult> SaveVentaAsync([FromBody] VentaInfo venta)
        {
            if (!ModelState.IsValid) return BadRequest("Json mal formado");

            try
            {
                venta.FechaVenta = DateTime.Now;
                Console.WriteLine(venta);
                _context.VentaInfo.Add(venta);
               await _context.SaveChangesAsync();
                return Ok("Informacion Guardada");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException);

            }

        }
        [HttpGet("GetVentas/{id}")]
        public async Task<IActionResult> GetVentaAsync([FromRoute] string id)
        {
            if (!ModelState.IsValid) return BadRequest("Json mal formado");

            try
            {
                
                return Ok(_context.VentaInfo.Where(x => x.IdVendedor == id).ToList());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException);

            }

        }
    }
    public class VentaInfo
    {
        [Key]
        public int id { get; set; }

        [Required]
        public Decimal longitud { get; set; }
        [Required]

        public Decimal latitud { get; set; }
        [Required]
        public string CardCode { get; set; }
        [Required]
        public string Cliente { get; set; }
        [Required]
        
        public string ClienteFantasia { get; set; }
        [Required]
        public string IdVendedor { get; set; }
        [Required]

        public DateTime FechaVenta { get; set; }
    }
}
