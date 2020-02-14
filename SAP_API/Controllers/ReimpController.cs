using Microsoft.AspNetCore.Mvc;
using LPS;
using System.Text;

namespace SAP_API.Controllers
{

    public class PrintS
    {
        public string gti { set; get; }
        public string peso { set; get; }
        public string lote { set; get; }
        public string ItemName { set; get; }
        public string ItemCode { set; get; }
        public string Fecha { set; get; }

    }

    [Route("api/[controller]")]
    [ApiController]
    public class ReimpController : ControllerBase
    {

        // POST: api/Reimp
        [HttpPost]
        public void Post([FromBody] PrintS value)
        {
            etiquetaFixIndividual(value.gti, value.lote, value.peso, value.ItemCode, value.ItemName, value.Fecha);
        }

        public void etiquetaFixIndividual(string GTIN, string Lote, string Peso, string ItemCode, string ItemName, string Fecha)
        {
            //string sUrlRequest = "http://api.ccfn.com.mx/json/productos/" + txtproducto.Text;
            //var json = new WebClient().DownloadString(sUrlRequest);

            //var list = JsonConvert.DeserializeObject<List<Producto>>(json)
            string s = "^XA\n";
            s += "^FW\n";
            s += "^CF0,40\n";
            s += "^FX Primera Seccion Producto\n";
            s += "^FO30,30^FDComercial de Carnes Frias del Norte SA de CV.^FS\n";
            s += "^CF1,20\n";
            s += "^FO30,80 ^ FDAv.Brasil 2800.^FS\n";
            s += "^FO30,100^FDCol.Alamitos 21210 ^FS\n";
            s += "^FO30,120^FDMexicali, BC^FS\n";
            s += "^FO30,140^FDMexico ^FS\n";
            s += "^CF0,30\n";
            s += "^FO50,190 ^FD" + ItemCode + " - " + ItemName + "^FS\n";
            s += "^FO40,180 ^FR^GB730,40,40 ^FS\n";
            s += "^BY2,2.5,80\n";
            s += "^FO200,230\n";
            s += "^BCN,,N,N,N,B\n";
            s += "^FD >;> 801" + GTIN + "10" + Lote + " ^FS\n";
            s += "^FT210,335 ^A0N,25,25 ^FD(01)" + GTIN + "(10)" + Lote + "^FS\n";
            s += "^CF0,20\n";
            s += "^FO50,360 ^FDGTIN:" + GTIN + "^FS\n";
            s += "^FO50,400 ^FDLote:" + Lote + "^FS\n";
            s += "^FO500,360 ^FDPeso: " + Peso + "^FS\n";
            s += "^FO500,400 ^FDCaducidad:" + Fecha + "^FS\n";
            s += "^BY2,2.5,60\n";
            s += "^FO600,80\n";
            s += "^BCN,,N,N,N,B\n";
            s += "^FD>;>837" + Peso + " ^FS\n";
            s += "^FT650,165^A0N,25,25 ^FD(37)" + Peso + "^FS\n";
            s += "^BY2,2.5,80\n";
            s += "^FO50,480\n";
            s += "^BCN,,N,N,N,B\n";
            s += "^FD>;>801" + GTIN + "3100" + Peso + "11" + Fecha + "10" + Lote + "37" + Peso + "^FS\n";
            s += "^FT60,590 ^ A0N,25,25 ^ FD(01)" + GTIN + "(3100)" + Peso + "(11)" + Fecha + "(10)" + Lote + "(37)" + Peso + "^FS\n";
            s += "^XZ\n";

            //sourceTextBox.Text = s;

            var bytes = Encoding.ASCII.GetBytes(s);
            // Send a printer-specific to the printer.

            RawPrinterHelper.SendBytesToPrinter("\\\\192.168.0.10\\s01-recepcion-srv10", bytes, bytes.Length);
            //MessageBox.Show("Data sent to printer.");
            //}
        }

    }
}
