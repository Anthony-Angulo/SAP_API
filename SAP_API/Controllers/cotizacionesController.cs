using Microsoft.AspNetCore.Mvc;
using Remotion.Linq.Clauses;
using SAP_API.Entities;
using SAP_API.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class cotizacionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public cotizacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/<cotizacionesController>
        [HttpGet("")]
        public IActionResult Get()
        {
            try
            {
                return Ok(_context.cotizaciones.Include(x=>x.rows)) ;

            }
            catch (System.Exception ex)
            {
                return Ok(ex);
                throw;
            }
        }

        // GET api/<cotizacionesController>/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            try
            {
                var cot = (from p in _context.cotizaciones
                          where p.Id==id
                          select new cotizaciones
                          {
                              Id = p.Id,
                              CardCode = p.CardCode,
                              CardFName = p.CardFName,
                              CardName = p.CardName,
                              Comments = p.Comments,
                              Currency = p.Currency,
                              CurrencyRate = p.CurrencyRate,
                              Date = p.Date,
                              Payment = p.Payment,
                              PriceList = p.PriceList,
                              rows = p.rows,
                              Series = p.Series
                          }).First();
                return Ok(cot);
            }
            catch (System.Exception ex)
            {
                return Ok(ex);
                throw;
            }
        }

        // POST api/<cotizacionesController>
        [HttpPost]
        public IActionResult Post([FromBody] cotizaciones value)
        {
            try
            {
                value.Date = DateTime.Now;

                _context.cotizaciones.Add(value);
                _context.SaveChanges();
                return Ok(value.Id);

            }
            catch (System.Exception Ex)
            {
                return BadRequest(Ex);
            }}

        // PUT api/<cotizacionesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<cotizacionesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
