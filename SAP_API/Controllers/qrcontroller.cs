using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Entities;
using SAP_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class qrcontroller : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        //Constructor
        public qrcontroller(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        [HttpPost("ChecaQR")]
        public IActionResult checkQr(VerifyQR qr)
        {
            var QR = _context.QR_ALMACENES.Where(x => x.data == qr.data && x.Almacen == qr.Warehouse && x.Activo == true).FirstOrDefault();
            if (QR == null)
            {
                return BadRequest("No existe");
            }
            return Ok(QR);
        }
        [HttpPost("CreateQR")]
        public IActionResult CreateQr(CreateQR qrpost)
        {
            List<QR_ALMACENES> qR_s = _context.QR_ALMACENES.Where(x => x.Almacen == qrpost.Almacen).ToList();
            foreach (var item in qR_s)
            {
                item.Activo = false;
            }
            _context.SaveChanges();
            QR_ALMACENES qr = _mapper.Map<QR_ALMACENES>(qrpost);

            _context.QR_ALMACENES.Add(qr);
            try
            {
                _context.SaveChanges();
            }
            catch (System.Exception Ex)
            {
                return BadRequest(Ex.Message);
                throw;
            }
            return Ok(qr);
        }
        public class VerifyQR
        {
            public string data { get; set; }

            public int Warehouse { get; set; }
        }
        public class CreateQR
        {
            public string data { get; set; }

            public int Almacen { get; set; }
            public bool Activo = true;

            public string IdUsuario { get; set; }

            public decimal Latitud { get; set; }

            public decimal Longitud { get; set; }

            public DateTime FechaCreacion = DateTime.Now;
        }
    }
}
