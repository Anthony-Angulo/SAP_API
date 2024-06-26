﻿using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SAP_API.Entities;
using SAP_API.Models;
using SAPbobsCOM;
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
    public class AutorizacionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AutorizacionController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

        }

        [HttpPost("RequestAutorizacion")]
        public async Task<IActionResult> SendMailAsync([FromBody] AutorizacionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("No esta bien formada la solicitud");
            }
            try
            {
                request.Fecha = DateTime.Now;
                _context.AutorizacionRequest.Add(request);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException);
            }
            try
            {
                string to = _configuration["cuentarecibeAutorizacion"];
                MailMessage message = new MailMessage(_configuration["CuentaAutorizacion"], to);
                string to1 = _configuration["cuentaRecibeAutorizacion2"];
                if (to1 != "")
                {
                    MailAddress bcc = new MailAddress(to1);
                    message.Bcc.Add(bcc);
                }
                message.Subject = "Solicitud de Autorizacion";
                message.IsBodyHtml = true;
                double preciobase = double.Parse(request.PrecioBase) * double.Parse(request.CantidadBase);

                message.Body = $@"
            <html>
            <body>
	        <p>El usuario <b>{request.Usuario}</b> de la sucursal <b>{request.Sucursal}</b> solicita autorización para vender un producto a precio diferente al autorizado.</p>
<ul>
    <li>Cliente: <b>{request.Cliente}</b></li>
    <li>Producto <b>{request.Producto}</b></li>
 <li>Cantidad: <b>{request.Cantidad}</b></li>
<li>Costo del artículo*: <b>{double.Parse(request.Costo):##.0000}</b> </li>
    <li>Precio base: <b>{preciobase:##.0000} {request.Currency}</b></li>
    <li>Precio introducido: <b>{double.Parse(request.PrecioSolicitado.Substring(0, 6)):##.0000} {request.PrecioSolicitado.Substring(7)}</b></li>
</ul>    
<p>*El costo del artículo es en base a la última entrada de mercancía registrada</p>
<a href=""{_configuration["DireccionAutorizacion"]}{request.id}"" target=""blank""><button
            style = ""background-color: green;color: white;
    border-radius: 12px;margin:10px;
        padding: 0.5rem; "">AUTORIZAR</button></a>
      
          <a><button style = ""background-color: red;color: white;
    border-radius: 12px;margin:10px;
        padding: 0.5rem;"">RECHAZAR</button>
     </a>
     
                    </body>
	        </html>";
                var smtpClient = new SmtpClient(_configuration["smtpserver"])
                {
                    Port = int.Parse(_configuration["port"]),
                    Credentials = new NetworkCredential(_configuration["CuentaAutorizacion"], _configuration["PassAutorizacion"]),
                    EnableSsl = true,
                };
                // Credentials are necessary if the server requires the client
                // to authenticate before it will send email on the client's behalf.

                smtpClient.Send(message);
                return Ok();
            }
            catch (Exception ex)
            {
                SAPbobsCOM.CompanyService ErrorCompanyService;
                MessagesService ErroroMessageService;
                SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
                ErrorCompanyService = context.oCompany.GetCompanyService();
                ErroroMessageService = (SAPbobsCOM.MessagesService)ErrorCompanyService.GetBusinessService(ServiceTypes.MessagesService);

                SAPbobsCOM.Message oMessage = null;
                RecipientCollection oRecipientCollection = null;

                // get the data interface for the new message
                oMessage = ((SAPbobsCOM.Message)(ErroroMessageService.GetDataInterface(MessagesServiceDataInterfaces.msdiMessage)));
                // fill subject
                oMessage.Subject = "Error a la hora de mandar solicitud de autorizaciones";
                oMessage.Text = $"{System.DateTime.Now}\n" +
                    $"Hubo un error: {ex}";
                oMessage.Priority = BoMsgPriorities.pr_High;
                // Add Recipient 
                oRecipientCollection = oMessage.RecipientCollection;

                // se agregan usuarios a los cuales les llegara el mensaje/alerta
                oRecipientCollection.Add();
                oRecipientCollection.Item(0).SendInternal = BoYesNoEnum.tYES; // send internal message
                oRecipientCollection.Item(0).UserCode = "SISTEMAS14"; // add existing user name
                oRecipientCollection.Item(0).UserType = BoMsgRcpTypes.rt_InternalUser;
                oRecipientCollection.Add();
                oRecipientCollection.Item(1).SendInternal = BoYesNoEnum.tYES; // send internal message
                oRecipientCollection.Item(1).UserCode = "SISTEMAS15";
                oRecipientCollection.Item(1).UserType = BoMsgRcpTypes.rt_InternalUser;
                oRecipientCollection.Add();
                oRecipientCollection.Item(2).SendInternal = BoYesNoEnum.tYES; // send internal message
                oRecipientCollection.Item(2).UserCode = "SISTEMAS13";
                oRecipientCollection.Item(2).UserType = BoMsgRcpTypes.rt_InternalUser;
                oRecipientCollection.Add();
                oRecipientCollection.Item(3).SendInternal = BoYesNoEnum.tYES; // send internal message
                oRecipientCollection.Item(3).UserCode = "SISTEMAS08";
                oRecipientCollection.Item(3).UserType = BoMsgRcpTypes.rt_InternalUser;
                oRecipientCollection.Add();
                oRecipientCollection.Item(4).SendInternal = BoYesNoEnum.tYES; // send internal message
                oRecipientCollection.Item(4).UserType = BoMsgRcpTypes.rt_InternalUser;
                oRecipientCollection.Item(4).UserCode = "SISTEMAS06";
                // send the message
                ErroroMessageService.SendMessage(oMessage);
                return BadRequest(ex.ToString());

            }
        }

        [HttpPost("RequestAutorizacionAlmacenes")]
        public async Task<IActionResult> SendMailAsyncAlmacenes([FromBody] AutorizacionAlmacenes request)
        {
            AutorizacionAlmacenes Vieja = _context.AutorizacionAlmacenes.Where(x => x.WhsDestino == request.WhsDestino
            && x.WhsOrigen == request.WhsOrigen && x.Autorizado != 2
            && x.Fecha.Date == DateTime.Now.Date
             ).FirstOrDefault();
            if (Vieja != null) return BadRequest("Ya existe una solicitud para este almacen, Favor de esperar respuesta");
            if (!ModelState.IsValid)
            {
                return BadRequest("No esta bien formada la solicitud");
            }
            try
            {
                request.Fecha = DateTime.Now;
                _context.AutorizacionAlmacenes.Add(request);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.InnerException);
            }


            string to = _configuration["cuentarecibeAutorizacion"];
            MailMessage message = new MailMessage(_configuration["CuentaAutorizacion"], to);

            string to1 = _configuration["cuentaRecibeAutorizacion2"];
            if (to1 != "")
            {
                MailAddress bcc = new MailAddress(to1);
                message.Bcc.Add(bcc);
            }
            message.Subject = "Solicitud de Autorizacion de transferencia entre almacenes";
            message.IsBodyHtml = true;
            message.Body = $@"
            <html>
            <body>
	        <p>La sucursal <b>{request.WhsOrigen}</b> solicita autorización para hacer transpasos al Almacen <b>{request.WhsDestino}</b>.</p>
<ul>
    <li>Almacen Origen <b>{request.WhsOrigen}</b></li>
    <li>Almacen Destino: <b>{request.WhsDestino}</b></li>
    <li>Comentario: <b>{request.Comentario}</b></li>

</ul>    
<p>*Al confirmar este autorizacion se le permitira al almacen origen hacer traspasos al almacen destino durante el dia en curso</p>
<a href=""{_configuration["DireccionAutorizacionAlmacen"]}{request.id}"" target=""blank""><button
            style = ""background-color: green;color: white;
    border-radius: 12px;margin:10px;
        padding: 0.5rem; "">AUTORIZAR</button></a>
      
          <a><button style = ""background-color: red;color: white;
    border-radius: 12px;margin:10px;
        padding: 0.5rem;"">RECHAZAR</button>
     </a>
     
                    </body>
	        </html>";
            var smtpClient = new SmtpClient(_configuration["smtpserver"])
            {
                Port = int.Parse(_configuration["port"]),
                Credentials = new NetworkCredential(_configuration["CuentaAutorizacion"], _configuration["PassAutorizacion"]),
                EnableSsl = true,
            };
            // Credentials are necessary if the server requires the client
            // to authenticate before it will send email on the client's behalf.

            try
            {
                smtpClient.Send(message);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());

            }
        }
        [HttpGet("Almacen/{id}")]
        public IActionResult AutorizarSolicitudAlmacen([FromRoute] int id)
        {
            AutorizacionAlmacenes autorizacion = _context.AutorizacionAlmacenes.Where(x => x.id == id).FirstOrDefault();

            try
            {
                if (autorizacion == null)
                {
                    return NotFound();
                }
                /*else if (autorizacion.Autorizado == 1)
                {
                    return Ok("Autorizacion ya aprobada");
                }*/
                autorizacion.Autorizado = 1;
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            try
            {
                SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
                SAPbobsCOM.UserTable oUserTable;
                oUserTable = context.oCompany.UserTables.Item("WH_AUT");
                oUserTable.Code = autorizacion.WhsDestino + autorizacion.WhsOrigen + DateTime.Now.ToString("yyyyMMddHHMMss");
                oUserTable.Name = autorizacion.WhsDestino + autorizacion.WhsOrigen + DateTime.Now.ToString("yyyyMMddHHMMss");
                oUserTable.UserFields.Fields.Item("U_FECHA").Value = DateTime.Now.Date;
                oUserTable.UserFields.Fields.Item("U_WH_DEST").Value = autorizacion.WhsDestino;
                oUserTable.UserFields.Fields.Item("U_WH_ORIG").Value = autorizacion.WhsOrigen;
                int result = oUserTable.Add();

                oUserTable = context.oCompany.UserTables.Item("WH_AUT");
                oUserTable.Code = autorizacion.WhsDestino + autorizacion.WhsOrigen + DateTime.Now.ToString("yyyyMMddHHMMss");
                oUserTable.Name = autorizacion.WhsDestino + autorizacion.WhsOrigen + DateTime.Now.ToString("yyyyMMddHHMMss");
                oUserTable.UserFields.Fields.Item("U_FECHA").Value = DateTime.Now.Date;
                oUserTable.UserFields.Fields.Item("U_WH_DEST").Value = "TSR" + autorizacion.WhsDestino;
                oUserTable.UserFields.Fields.Item("U_WH_ORIG").Value = autorizacion.WhsOrigen;
                result = oUserTable.Add();
                if (result == 0)
                {
                    return Content(@"<script>window.close();</script>", "text/html");
                }
                else
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new
                    {
                        error
                    });
                }
            }
            catch (Exception ex)
            {

                return BadRequest(ex);
            }




        }

        [HttpGet("{id}")]
        public IActionResult AutorizarSolicitud([FromRoute] int id)
        {
            AutorizacionRequest autorizacion = _context.AutorizacionRequest.Where(x => x.id == id).FirstOrDefault();
            try
            {
                if (autorizacion == null)
                {
                    return NotFound();
                }
                else if (autorizacion.Autorizado == 1)
                {
                    return Ok("Autorización ya fue aprobada anteriormente");
                }
                autorizacion.Autorizado = 1;
                autorizacion.FechaAutorizado = DateTime.Now;
                _context.SaveChanges();
               

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            try
            {
                bool resul = false;

                SAPbobsCOM.CompanyService oCmpSrv;
                MessagesService oMessageService;

                //get company service
                SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
                oCmpSrv = context.oCompany.GetCompanyService();
                oMessageService = (SAPbobsCOM.MessagesService)oCmpSrv.GetBusinessService(ServiceTypes.MessagesService);

                SAPbobsCOM.Message oMessage = null;
                RecipientCollection oRecipientCollection = null;

                // get the data interface for the new message
                oMessage = ((SAPbobsCOM.Message)(oMessageService.GetDataInterface(MessagesServiceDataInterfaces.msdiMessage)));
                double preciobase = double.Parse(autorizacion.PrecioBase) * double.Parse(autorizacion.CantidadBase);
                // fill subject
                oMessage.Subject = "Autorización aceptada";
                oMessage.Text = $"{System.DateTime.Now.ToString()}\n" +
                    $"Su solicitud correspondiente a los siguientes datos ha sido autorizada.\n" +
                    $"Cliente: {autorizacion.Cliente}\n" +
                    $"Producto: {autorizacion.Producto}\n" +
                    $"Precio base: {preciobase:##.0000} {autorizacion.Currency}\n" +
                    $"Precio introducido: {double.Parse(autorizacion.PrecioSolicitado.Substring(0, 6)):##.0000} {autorizacion.PrecioSolicitado.Substring(7)}\n" +
                    $"Tiene 60 minutos para registrar la factura antes de que la autorización expire.";

                // Add Recipient 
                oRecipientCollection = oMessage.RecipientCollection;

                // se agregan usuarios a los cuales les llegara el mensaje/alerta
                oRecipientCollection.Add();
                oRecipientCollection.Item(0).SendInternal = BoYesNoEnum.tYES; // send internal message
                oRecipientCollection.Item(0).UserCode = autorizacion.USER_CODE; // add existing user name


                // send the message
                oMessageService.SendMessage(oMessage);
                resul = true;

                GC.Collect();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            DateTime FechaInicial = DateTime.Now;
            DateTime FechaFinal = FechaInicial.AddDays(1);
            try
            {
                SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
                SAPbobsCOM.UserTable oUserTable;
                oUserTable = context.oCompany.UserTables.Item("CCFN_AUTORIZACIONES");
                oUserTable.Code = autorizacion.CardCode + autorizacion.ProductCode + FechaInicial.ToString("yyyyMMddHHMMss");
                oUserTable.Name = autorizacion.CardCode + autorizacion.ProductCode + FechaInicial.ToString("yyyyMMddHHMMss");
                oUserTable.UserFields.Fields.Item("U_CardCode").Value = autorizacion.CardCode;
                oUserTable.UserFields.Fields.Item("U_ItemCode").Value = autorizacion.ProductCode;
                oUserTable.UserFields.Fields.Item("U_DateIn").Value = FechaInicial;
                oUserTable.UserFields.Fields.Item("U_HourIn").Value = FechaInicial;
                oUserTable.UserFields.Fields.Item("U_DateOut").Value = FechaFinal;
                oUserTable.UserFields.Fields.Item("U_HourOut").Value = FechaFinal;
                oUserTable.UserFields.Fields.Item("U_Status").Value = "0";
                oUserTable.UserFields.Fields.Item("U_U_NAME").Value = autorizacion.USER_CODE;
                oUserTable.UserFields.Fields.Item("U_DateReg").Value = FechaInicial;
                oUserTable.UserFields.Fields.Item("U_HourReg").Value = FechaInicial;
                oUserTable.UserFields.Fields.Item("U_VatSum").Value = autorizacion.PrecioSolicitado;

                int result = oUserTable.Add();
                if (result == 0)
                {
                    //return Content(@"<script>window.close();</script>", "text/html");
                    return Ok("Autorización fue Realizada");

                }
                else
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            catch (Exception ex)
            {

                return BadRequest(ex);
            }

            
        }
        [HttpGet("InsertarSAP")]
        public IActionResult InsertarSAP()
        {
            AutorizacionAlmacenes autorizacion = _context.AutorizacionAlmacenes.Where(x => x.id == 1).FirstOrDefault();

            try
            {
                SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
                SAPbobsCOM.UserTable oUserTable;
                oUserTable = context.oCompany.UserTables.Item("WH_AUT");
                oUserTable.Code = autorizacion.WhsDestino + autorizacion.WhsOrigen + DateTime.Now.ToString("yyyyMMddHHMMss");
                oUserTable.Name = autorizacion.WhsDestino + autorizacion.WhsOrigen + DateTime.Now.ToString("yyyyMMddHHMMss");
                oUserTable.UserFields.Fields.Item("U_FECHA").Value = DateTime.Now.Date;
                oUserTable.UserFields.Fields.Item("U_WH_DEST").Value = autorizacion.WhsDestino;
                oUserTable.UserFields.Fields.Item("U_WH_ORIG").Value = autorizacion.WhsOrigen;
                int result = oUserTable.Add();
                if (result == 0)
                {
                    return Content(@"<script>window.close();</script>", "text/html");
                }
                else
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new
                    {
                        error
                    });
                }
            }
            catch (Exception ex)
            {

                return BadRequest(ex);
            }
        }

        [HttpPost("GetData")]
        public IActionResult GetData([FromBody] Request request)
        {

            return Ok(
                _context.AutorizacionRequest.Where(x => x.Fecha >= request.FechaInicial && x.Fecha <= request.FechaFinal && x.Autorizado == request.Autorizado).ToList()
                );
        }

        [HttpGet("SendMail")]
        public IActionResult SendMail()
        {
            //string to = "gustavo.carreno@superchivas.com.mx";//_configuration["cuentaenvio"];
            MailMessage message = new MailMessage(_configuration["cuentacorreo"], _configuration["cuentaenvio"]);
            message.Subject = "Autorizaciones hechas por el Sr. Cervantes en Sistema SAP B1";
            message.Body = @"";
            var smtpClient = new SmtpClient(_configuration["smtpserver"])
            {
                Port = int.Parse(_configuration["port"]),
                Credentials = new NetworkCredential(_configuration["cuentacorreo"], _configuration["passcorreo"]),
                EnableSsl = true
            };
            var csv = new StringBuilder();
            DateTime FechaInicial = DateTime.Now.Date;
            DateTime FechaFinal = DateTime.Now;
            FechaInicial = FechaInicial.AddDays(-7);
            FechaInicial = new DateTime(FechaInicial.Year, FechaInicial.Month, FechaInicial.Day);
            csv.Append("sep=;" + Environment.NewLine);
            List<AutorizacionRequest> logFacturacions = _context.AutorizacionRequest.Where(x => x.Fecha >= FechaInicial).OrderByDescending(x => x.Fecha).ToList();
            //List<LogFacturacion> logFacturacions = _context.LogFacturacion.ToList();
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Inserting Tables");
            var people = from p in logFacturacions

                         select new
                         {
                             Fecha = p.Fecha,
                             Usuario = p.Usuario,
                             Sucursal = p.Sucursal,
                             Cliente = p.Cliente,
                             CodigoCliente = p.CardCode,
                             Producto = p.Producto,
                             CodigoProducto = p.ProductCode,
                             PrecioBase = (double.Parse(p.CantidadBase) * double.Parse(p.PrecioBase)).ToString(),
                             MonedaBase = p.Currency,
                             CantidadBase = p.CantidadBase,
                             PrecioSolicitado = p.PrecioSolicitado,
                             Autorizado = p.Autorizado == 1 ? "Autorizado" : "No Autorizado",
                             Costo = p.Costo,
                             NumeroDeDocumentoCosto = p.DocNumCosto,
                             FechaAutorizado = p.FechaAutorizado
                         };

            var tableWithPeople = ws.Cell(1, 1).InsertTable(people.AsEnumerable());

            var memoryStream = new System.IO.MemoryStream();
            wb.SaveAs(memoryStream);
            byte[] contentAsBytes = Encoding.UTF8.GetBytes("C:\\test.xlsx");
            memoryStream.Write(contentAsBytes, 0, contentAsBytes.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new System.Net.Mail.Attachment(memoryStream,
                                                    "autorizaciones_SAP_B1.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            message.Attachments.Add(attachment);
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

        public class Request
        {
            public DateTime FechaInicial { get; set; }
            public DateTime FechaFinal { get; set; }
            public int Autorizado { get; set; }
        }
    }

}
