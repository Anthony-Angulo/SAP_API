using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;
using System.Threading.Tasks;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SAP_API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    [AllowAnonymous]
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
        [HttpGet("")]
        public IActionResult GetInvoices()
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            JToken invoice;
            oRecSet.DoQuery(@"
                Select top 100
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
                    Where Invoice.""CANCELED""='N'");
            if (oRecSet.RecordCount == 0)
            {
                // Handle no Existing Invoice
                return NotFound();
            }
            invoice = context.XMLTOJSON(oRecSet.GetAsXML())["OINV"];

            return Ok(invoice);
        }

        [HttpGet("zones")]
        public IActionResult GetZones()
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery($@"
                Select * from ""@IL_ADD_PAYMENT_ZONE""");
            if (oRecSet.RecordCount == 0)
            {
                // Handle no Existing Invoice
                return NotFound();
            }
           var invoice = context.FixedXMLTOJSON(oRecSet.GetFixedXML(SAPbobsCOM.RecordsetXMLModeEnum.rxmData));

            return Ok(invoice);
        }

        [HttpGet("GetInvoicesByZone/{Zone}")]
        public IActionResult GetInvoicesByZone(string Zone)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            JToken invoice;
            oRecSet.DoQuery($@"
                SELECT
T0.""DocNum"",
T0.""DocDate"",
T0.""CardCode"",
T0.""DocCur"",
T0.""DocRate"",
T0.""CardName"",
/*
T1.""ItemCode"",
T1.""Dscription"",
T1.""Quantity"",
T1.""WhsCode"",
*/
T0.""DocTotal"",
T0.""DocTotalFC"",
T2.""U_IL_Zone""
FROM OINV T0
--INNER JOIN INV1 T1 ON T0.""DocEntry"" = T1.""DocEntry""
INNER JOIN OCRD T2 ON T0.""CardCode"" = T2.""CardCode""
WHERE T2.""U_IL_Zone""='{Zone}' AND T0.""DocStatus"" ='O'");
            if (oRecSet.RecordCount == 0)
            {
                // Handle no Existing Invoice
                return NotFound();
            }
            invoice = context.XMLTOJSON(oRecSet.GetAsXML())["OINV"];

            return Ok(invoice);
        }
       
        [HttpGet]
        public IActionResult GetInvoicesByZoneAndDate([FromQuery]string Zone, [FromQuery] string InitialDate, [FromQuery] string FinalDate)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            JToken invoice;
            oRecSet.DoQuery($@"
                SELECT
T0.""DocNum"",
T0.""DocDate"",
T0.""CardCode"",
T0.""DocCur"",
T0.""DocRate"",
T0.""CardName"",
/*
T1.""ItemCode"",
T1.""Dscription"",
T1.""Quantity"",
T1.""WhsCode"",
*/
T0.""DocTotal"",
T0.""DocTotalFC"",
T2.""U_IL_Zone""
FROM OINV T0
--INNER JOIN INV1 T1 ON T0.""DocEntry"" = T1.""DocEntry""
INNER JOIN OCRD T2 ON T0.""CardCode"" = T2.""CardCode""
WHERE T2.""U_IL_Zone""='{Zone}' AND T0.""DocStatus"" ='O'
AND ""DocDate"">='{InitialDate}'
AND ""DocDate""<='{FinalDate}'
");
            //2023/08/10
            if (oRecSet.RecordCount == 0)
            {
                // Handle no Existing Invoice
                return NotFound();
            }
            invoice = context.XMLTOJSON(oRecSet.GetAsXML())["OINV"];

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
