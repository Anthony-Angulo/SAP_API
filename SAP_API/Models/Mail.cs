using Microsoft.Extensions.Configuration;
using System.Net.Mail;

namespace SAP_API.Models
{
    public class Mail
    {
        private readonly IConfiguration _configuration;
        public Mail(     IConfiguration configuration) {
            _configuration = configuration;
        }

        public MailMessage GetAutorizacionMessage(AutorizacionRequest request )
        {
            MailMessage message= new MailMessage(_configuration["CuentaAutorizacion"], _configuration["cuentarecibeAutorizacion"]);
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
    <li>Precio introducido: <b>{double.Parse(request.PrecioSolicitado.Substring(4)):##.0000} {request.PrecioSolicitado.Substring(0, 4)}</b></li>
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
            return message;
        }
    }
}
