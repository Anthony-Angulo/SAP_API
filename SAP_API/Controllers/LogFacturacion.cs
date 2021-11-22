using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SAP_API.Entities;
using SAP_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogFacturacionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public LogFacturacionController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

        }
        // GET: api/Order/CRMOrderDaily
        [HttpPost("AddLog")]
        public IActionResult LogFactPost([FromBody] LogFacturacion log)
        {
            try
            {
                log.fecha = DateTime.Now;
                _context.LogFacturacion.Add(log);
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException);
            }
        }
        [HttpGet("SendMail")]
        public IActionResult SendMail()
        {
            string to = "gustavo.carreno@superchivas.com.mx";//_configuration["cuentaenvio"];
            MailMessage message = new MailMessage(_configuration["cuentacorreo"], to);
            message.Subject = "Bitácora de precios de venta fuera del intervalo autorizado";
            message.Body = @"";
            var smtpClient = new SmtpClient(_configuration["smtpserver"])
            {
                Port = 587,
                Credentials = new NetworkCredential(_configuration["cuentacorreo"], _configuration["passcorreo"]),
                EnableSsl = true
            };
            var csv = new StringBuilder();
            DateTime FechaInicial = DateTime.Now;
            DateTime FechaFinal = DateTime.Now;
          FechaInicial= FechaInicial.AddDays(-1);
            csv.Append("sep=;"+Environment.NewLine);
            //List<LogFacturacion> logFacturacions = _context.LogFacturacion.Where(x=>x.fecha>=FechaInicial).OrderBy(x=>x.fecha).ToList();
            List<LogFacturacion> logFacturacions = _context.LogFacturacion.ToList();

            csv.Append(string.Format("Fecha;Usuario;Producto;Precio base;Moneda base;Tipo Cambio;Precio introducido;Moneda introducida;Serie;Almacen") + Environment.NewLine);

            foreach (var item in logFacturacions)
            {
                csv.Append(string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", 
                    item.fecha,item.user,
                    item.Productdsc,
                    (double.Parse(item.CantidadBase)*double.Parse(item.PrecioBase)).ToString(),
                    item.MonedaBase, 
                    item.TipoCambio,
                    item.PrecioIntroducido,
                    item.MonedaIntroducida,
                    item.serie,
                    item.warehouseextern)+ Environment.NewLine);
            }
            System.IO.MemoryStream theStream = new System.IO.MemoryStream();
            byte[] byteArr = Encoding.ASCII.GetBytes(csv.ToString());
            
            System.IO.MemoryStream stream1 = new System.IO.MemoryStream(byteArr, true);
            stream1.Write(byteArr, 0, byteArr.Length);
            stream1.Position = 0;
            message.Attachments.Add(new Attachment(stream1, $"precios_de_venta_fuera_de_intervalo_autorizado.csv"));
            // Credentials are necessary if the server requires the client
            // to authenticate before it will send email on the client's behalf.

            try
            {
                if (logFacturacions.Count != 0)
                {
                    smtpClient.Send(message);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());

            }
        }
    }
}
