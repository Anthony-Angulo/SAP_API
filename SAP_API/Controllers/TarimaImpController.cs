using System;
using System.Text;
using LPS;
using Microsoft.AspNetCore.Mvc;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TarimaImpController : ControllerBase {

        public class TarimaPrintTransfer {
            public string WHS { set; get; }
            public string Pallet { set; get; }
            public string Request { set; get; }
            public string Transfer { set; get; }
            public string RequestCopy { set; get; }
        } 

        // POST: api/TarimaImp
        [HttpPost]
        public void Post([FromBody] TarimaPrintTransfer value) {
            etiquetaproduccion(value.WHS, value.Pallet, value.Request, value.Transfer, value.RequestCopy, DateTime.Now.ToString());
        }

        private void etiquetaproduccion(string WHS, string NumeroTarima, string SolicitudTraslado, string Transferencia, string Recepcion, string Fecha) {
            //string sUrlRequest = "http://api.ccfn.com.mx/json/productos/" + txtproducto.Text;
            //var json = new WebClient().DownloadString(sUrlRequest);

            //var list = JsonConvert.DeserializeObject<List<Producto>>(json);

            string s = "^XA\n";
            s += "^FW\n";
            s += "^CFA,40\n";
            s += "^FO50,30^FDSucursal: " + WHS + "^FS\n";
            s += "^CFA,40\n";
            s += "^FO550,150^FDTarima ^FS\n";
            s += "^FO590,220^FD" + NumeroTarima + " ^FS\n";
            s += "^CFA,35\n";
            s += "^FO40,100^FDSolicitud de Traslado^FS\n";
            s += "^BY3,2,70\n";
            s += "^FO40,150^BC^FD" + SolicitudTraslado + "^FS\n";
            //'---------------- Tranferencias generadas para la Tarima 1
            s += "^ CFA,40\n";
            s += "^ FO40,350^FDTransferencia^FS\n";
            s += "^ FO40,380^FD" + Transferencia + "^FS\n";

            //'-------------------
            s += "^ CFA,35\n";
            s += "^FO350,450^FDRecepcion^FS\n";
            s += "^BY3,2,70";
            s += "^FO350,500^BC^FD" + Recepcion + "^FS\n";
            s += "^CFA,20\n";
            s += "^FO40,570^FDFecha: " + Fecha + "^FS\n";
            s += "^XZ\n";


            var bytes = Encoding.ASCII.GetBytes(s);
            // Send a printer-specific to the printer.
            RawPrinterHelper.SendBytesToPrinter("\\\\192.168.0.10\\s01-recepcion-srv10", bytes, bytes.Length);
            //MessageBox.Show("Data sent to printer.");
            //}
        }

    }
}
