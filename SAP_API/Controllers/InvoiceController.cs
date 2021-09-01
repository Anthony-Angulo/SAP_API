using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;
using System.Threading.Tasks;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceController : ControllerBase
    {

        // GET api/<InvoiceController>/5
        [HttpGet("{DocEntry}")]
        public IActionResult GetInvoice(string DocEntry)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            JToken invoice;
            oRecSet.DoQuery(@"
                Select
                    Invoice.""DocEntry"",
                    Invoice.""DocNum"",
                    Invoice.""DocDate"",
                    Invoice.""CardCode"",
                    Client.""CardName"",
                    Client.""CardFName"",
                    Invoice.""DocCur"",
                    Invoice.""DocRate"",
                    Invoice.""DocNum""||Invoice.""CardCode"" || to_char(Invoice.""DocDate"", 'YYYYMMDD') as ""CodBar"",
                    Invoice.""DocTotalSy"",
                    Invoice.""Series""
                    From OINV Invoice
                    JOIN OCRD Client ON Client.""CardCode""= Invoice.""CardCode""
                    Where Invoice.""DocNum"" = '" + DocEntry + "'" +
                    @"and Invoice.""CANCELED""='N'");
            if (oRecSet.RecordCount == 0)
            {
                // Handle no Existing Invoice
                return NotFound(@"La factura con el numero " + DocEntry);
            }
            invoice = context.XMLTOJSON(oRecSet.GetAsXML())["OINV"][0];

            return Ok(invoice);
        }
        // GET api/<InvoiceController>/5
        [HttpGet("Code/{Codebar}")]
        public IActionResult GetInvoiceCodeBar(string Codebar)
        {
            int FinalDocNum = Codebar.IndexOf("C");
            string DocEntry = Codebar.Substring(0, FinalDocNum);
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            JToken invoice;
            oRecSet.DoQuery(@"
                Select
                    Invoice.""DocEntry"",
                    Invoice.""DocNum"",
                    Invoice.""DocDate"",
                    Invoice.""CardCode"",
                    Client.""CardName"",
                    Client.""CardFName"",
                    Invoice.""DocCur"",
                    Invoice.""DocRate"",
                    Invoice.""DocNum""|| Invoice.""CardCode"" || to_char(Invoice.""DocDate"", 'YYYYMMDD') as ""CodBar"",
                    Invoice.""DocTotalSy"",
                    Invoice.""Series""
                    From OINV Invoice
                    JOIN OCRD Client ON Client.""CardCode""= Invoice.""CardCode""
                    Where Invoice.""DocNum""|| Invoice.""CardCode"" || to_char(Invoice.""DocDate"", 'YYYYMMDD') = '" + Codebar + "'" +
                    @"and Invoice.""CANCELED""='N'");
            if (oRecSet.RecordCount == 0)
            {
                // Handle no Existing Invoice
                return NotFound(@"La factura con el numero " + DocEntry + " No existe");
            }
            invoice = context.XMLTOJSON(oRecSet.GetAsXML())["OINV"][0];
            return Ok(invoice);
        }
    }
}
