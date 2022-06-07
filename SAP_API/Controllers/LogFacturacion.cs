using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SAP_API.Entities;
using SAP_API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
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
            string to = _configuration["cuentaenvio"];
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
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("preciosFueraDeIntervalo");
            var people = from p in logFacturacions

                         select new
                         {
                             Fecha= p.fecha,
                             Usuario= p.user,
                             Producto=p.Productdsc,
                             Preciobase=(double.Parse(p.CantidadBase) * double.Parse(p.PrecioBase)).ToString(),
                             MonedaBase=p.MonedaBase,
                             TipoCambio=p.TipoCambio,
                             PrecioIntroducido = p.PrecioIntroducido,
                             MonedaIntroducida = p.MonedaIntroducida,
                             serie = p.serie,
                             Almacen=p.warehouseextern
                         };
            var tableWithPeople = ws.Cell(1, 1).InsertTable(people.AsEnumerable());

            var memoryStream = new System.IO.MemoryStream();
            wb.SaveAs(memoryStream);
            byte[] contentAsBytes = Encoding.UTF8.GetBytes("C:\\test.xlsx");

            memoryStream.Write(contentAsBytes, 0, contentAsBytes.Length);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new System.Net.Mail.Attachment(memoryStream,
                                                    "precios_de_venta_fuera_de_intervalo_autorizado.xlsx", MediaTypeNames.Application.Octet);
            message.Attachments.Add(attachment);
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
