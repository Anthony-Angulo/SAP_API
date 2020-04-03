using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ImpresionController : ControllerBase {

        // GET: api/Impresion/
        [HttpGet("Impresoras")]
        public async Task<IActionResult> GetImpresoras() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select * From \"@IL_IMPRESORAS\"");
            JToken impresoras = context.XMLTOJSON(oRecSet.GetAsXML())["IL_IMPRESORAS"];
            return Ok(impresoras);
        }

        public class TarimaPrint {
            public string WHS { set; get; }
            public string Pallet { set; get; }
            public string Request { set; get; }
            public string Transfer { set; get; }
            public string RequestCopy { set; get; }
            public string IDPrinter { set; get; }
        }

        // POST: api/TarimaImp
        [HttpPost("Tarima")]
        public void Post([FromBody] TarimaPrint value) {
            etiquetaproduccion(value.IDPrinter, value.WHS, value.Pallet, value.Request, value.Transfer, value.RequestCopy, DateTime.Now.ToString());
        }

        public void etiquetaproduccion(string IDPrinter, string WHS, string NumeroTarima, string SolicitudTraslado, string Transferencia, string Recepcion, string Fecha) {

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
            RawPrinterHelper.SendBytesToPrinter("\\\\192.168.0.10\\" + IDPrinter, bytes, bytes.Length);
        }

        // PUT: api/Impresion/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
