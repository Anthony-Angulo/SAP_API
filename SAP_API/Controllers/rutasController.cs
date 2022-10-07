using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Entities;
using SAP_API.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class rutasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        //Constructor
        public rutasController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        [HttpGet]
        public IActionResult GetRutas()
        {
            return Ok(_context.rutas.Where(x => x.Activo == true));
        }

        [HttpGet("{id}")]
        public IActionResult GetRutasById(int id)
        {
            var rutas = (from rut in _context.rutas
                         join Usuarios in _context.Users on rut.IdUsuarioAsignado equals Usuarios.Id into User
                         from U in User.DefaultIfEmpty()
                         where rut.id == id
                         select new
                         {
                             rut.id,
                             rut.Nombre,
                             U.Name,
                             tiendas = (from tiendas in _context.tiendas_ruta
                                        join Almacenes in _context.Warehouses on tiendas.Id equals Almacenes.ID
                                        where tiendas.IdRuta == rut.id && tiendas.Activo == true
                                        select new
                                        {
                                            Almacenes.ID,
                                            Almacenes.WhsName,

                                        }).ToList()

                         }).First();
            return Ok(rutas);
        }

        [HttpGet("ByUsuario/{id}")]
        public IActionResult GetRutasByUsuarioId(string Id)
        {
            var Rutas = (from rut in _context.rutas
                         where rut.IdUsuarioAsignado == Id
                         select new
                         {
                             rut.id,
                             rut.Nombre,
                             tiendas = (from tiendas in _context.tiendas_ruta
                                        join Almacenes in _context.Warehouses on tiendas.Id equals Almacenes.ID
                                        where tiendas.IdRuta == rut.id && tiendas.Activo == true
                                        select new
                                        {
                                            Almacenes.ID,
                                            Almacenes.WhsName,

                                        }).ToList()

                         }).FirstOrDefault();
            return Ok(Rutas);
        }

        [HttpPost]
        public IActionResult AddRuta(AddRutaDto addRuta)
        {
            var ruta = _mapper.Map<rutas>(addRuta);
            _context.rutas.Add(ruta);

            _context.SaveChanges();
            foreach (var item in addRuta.Almacenes)
            {
                _context.tiendas_ruta.Add(new tiendas_ruta
                {
                    Activo = true,
                    IdTienda = item,
                    FechaAsignada = addRuta.FechaCreacion,
                    IdRuta = ruta.id
                });
            }
            _context.SaveChanges();
            return Ok(ruta);
        }
        /*
                [HttpPost("AddAlmacenesToRoute")]
                public IActionResult AddTiendasToRuta(tiendasRutaPost tiendasRuta)
                {
                    List<tiendas_ruta> tiendasActuales = _context.tiendas_ruta.Where(x => x.IdRuta == tiendasRuta.IdRuta && x.FechaAsignada.Date == tiendasRuta.FechaAsignada.Date && x.Activo == true).ToList();

                    foreach (var item in tiendasActuales)
                    {
                        item.Activo = false;

                    }
                    _context.SaveChanges();
                    foreach (var item in tiendasRuta.AlmacenesId)
                    {
                        _context.tiendas_ruta.Add(new tiendas_ruta
                        {
                            Activo = true,
                            IdTienda = item,
                            FechaAsignada = tiendasRuta.FechaAsignada,
                            IdRuta = tiendasRuta.IdRuta
                        });
                    }
                    _context.SaveChanges();
                    return Ok();
                }*/
        [HttpPut]
        public IActionResult UpdateRuta(UpdateRutaDto addRuta)
        {
            try
            {
                var cliente = _context.rutas.Where(x => x.id == addRuta.id).FirstOrDefault();

                _mapper.Map(addRuta, cliente);
                List<tiendas_ruta> tiendasActuales = _context.tiendas_ruta.Where(x => x.IdRuta == addRuta.id && x.FechaAsignada.Date == addRuta.FechaActualizacion.Date && x.Activo == true).ToList();

                foreach (var item in tiendasActuales)
                {
                    item.Activo = false;

                }
                _context.SaveChanges();
                foreach (var item in addRuta.Almacenes)
                {
                    _context.tiendas_ruta.Add(new tiendas_ruta
                    {
                        Activo = true,
                        IdTienda = item,
                        FechaAsignada = addRuta.FechaActualizacion,
                        IdRuta = addRuta.id
                    });
                }


                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }

            return NoContent();
        }
        public class UpdateRutaDto
        {
            public int id { get; set; }
            public string Nombre { get; set; }

            public string IdUsuarioAsignado { get; set; }
            public bool Activo { get; set; }

            [IgnoreMap]
            public DateTime FechaActualizacion { get; set; }
            [IgnoreMap]

            public List<int> Almacenes { get; set; }
        }

        public class AddRutaDto
        {
            public string Nombre { get; set; }

            public DateTime FechaCreacion = DateTime.Now;

            public bool Activo = true;
            [IgnoreMap]

            public List<int> Almacenes { get; set; }
        }
        public class tiendasRutaPost
        {
            public int IdRuta { get; set; }

            public List<int> AlmacenesId { get; set; }

            public DateTime FechaAsignada { get; set; }
        }
    }
}
