using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SAP_API.Entities;
using SAP_API.Models;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

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
        [HttpPost("TwilioAutorizacion")]
        public async Task<IActionResult> SendTwilioAsync()
        {
            TwilioClient.Init("AC325fc690a873079a3b6f77f3512cc872", "25f7b22387f439b5937c4874f1e87ee3");
            AutorizacionRequest request = _context.AutorizacionRequest.Where(x => x.id == 43).FirstOrDefault();
            double preciobase = double.Parse(request.PrecioBase) * double.Parse(request.CantidadBase);

            String Body = $@"El usuario {request.Usuario} de la sucursal {request.Sucursal} solicita autorización para vender un producto a precio diferente al autorizado.{Environment.NewLine}Cliente:{request.Cliente}.{Environment.NewLine}Producto:{request.Producto}.{Environment.NewLine}Cantidad:{request.Cantidad}.{Environment.NewLine}Precio base:{preciobase} {request.Currency}.{Environment.NewLine}Precio introducido:{request.PrecioSolicitado.Substring(4)} {request.PrecioSolicitado.Substring(0, 4)}.{Environment.NewLine} Si desea aprobarlo escriba:{request.id}";
            var message = MessageResource.Create(
                body: Body,
                from: new Twilio.Types.PhoneNumber("whatsapp:+14155238886"),
                to: new Twilio.Types.PhoneNumber("whatsapp:+5216864259059")
            );
            var messages = MessageResource.Read(to: new Twilio.Types.PhoneNumber("whatsapp:+5216864259059"));
            return Ok(messages);
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
            request = _context.AutorizacionRequest.Where(x => x.id == 1).FirstOrDefault();
            TwilioClient.Init("AC325fc690a873079a3b6f77f3512cc872", "25f7b22387f439b5937c4874f1e87ee3");
            double preciobase = double.Parse(request.PrecioBase) * double.Parse(request.CantidadBase);

            String Body = $@"El usuario {request.Usuario} de la sucursal {request.Sucursal} solicita autorización para vender un producto a precio diferente al autorizado.{Environment.NewLine}Cliente:{request.Cliente}.{Environment.NewLine}Producto:{request.Producto}.{Environment.NewLine}Cantidad:{request.Cantidad}.{Environment.NewLine}Precio base:{preciobase} {request.Currency}.{Environment.NewLine}Precio introducido:{request.PrecioSolicitado.Substring(4)} {request.PrecioSolicitado.Substring(0, 4)}.{Environment.NewLine} Si desea aprobarlo escriba:{request.id}";
            
            var message = MessageResource.Create(
                body: Body,
                from: new Twilio.Types.PhoneNumber("whatsapp:+14155238886"),
                to: new Twilio.Types.PhoneNumber("whatsapp:+5216864259059")
            );
            
            return Ok();
            /*
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
    <li>Precio base: <b>{preciobase} {request.Currency}</b></li>
    <li>Precio introducido: <b>{request.PrecioSolicitado.Substring(4)} {request.PrecioSolicitado.Substring(0, 4)}</b></li>
</ul>    
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
                Port = 587,
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

            }*/
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
                autorizacion.Autorizado = 1;
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                return BadRequest();
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
                    $"Precio base: {preciobase} {autorizacion.Currency}\n" +
                    $"Precio introducido: {autorizacion.PrecioSolicitado.Substring(4)} {autorizacion.PrecioSolicitado.Substring(0,4)}\n" +
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

            }
            DateTime FechaInicial = DateTime.Now.AddMinutes(-10);
            DateTime FechaFinal = FechaInicial.AddMinutes(60);
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
                    return Content(@"<script>window.close();</script>", "text/html");
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
            AutorizacionRequest autorizacion = _context.AutorizacionRequest.Where(x => x.id == 24).FirstOrDefault();

            DateTime FechaInicial = DateTime.Now;
            DateTime FechaFinal = FechaInicial.AddMinutes(60);
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
                    return Content(@"<script>window.close();</script>", "text/html");
    }
                else
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error
});
                }
            }
            catch (Exception ex)
{

    return BadRequest(ex);
}
        }
       
    }

}
