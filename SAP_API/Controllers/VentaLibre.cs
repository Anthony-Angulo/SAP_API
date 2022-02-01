using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Entities;
using SAP_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class VentaLibre : ControllerBase
    {
        private readonly ApplicationDbContext _context;
                
        public VentaLibre(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public IActionResult getVentaLibre(string id)
        {
            return Ok(_context.VentaLibre.Where(x=>x.idUsuario==id).FirstOrDefault());
        }
        [HttpPost("")]
        public IActionResult PostVentaLibre(VentaLibreModel ventaLibre)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            try
            {
                VentaLibreModel VentaLibre = _context.VentaLibre.Where(x => x.idUsuario == ventaLibre.idUsuario).FirstOrDefault();
                if(ventaLibre != null)
                {
                    return BadRequest("Este usuario ya tiene configuracion");
                }
                _context.VentaLibre.Add(ventaLibre);
                _context.SaveChanges();
                return Ok("Agregado correctamente");
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
