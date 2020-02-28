using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        public class ClientSearchDetail : SearchDetail
        {
            public string CardCode { get; set; }
            public string CardFName { get; set; }
            public string CardName { get; set; }
        }

        public class ClientSearchResponse : SearchResponse<ClientSearchDetail>
        {
        }

        [HttpPost("clients/search")]
        public async Task<IActionResult> Get([FromBody] SearchRequest request)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();

            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(\"CardCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(\"CardFName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(\"CardName\") Like LOWER('%{request.columns[2].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY \"CardCode\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY \"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY \"CardName\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY \"CardCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select ""CardCode"", ""CardName"", ""CardFName""
                From OCRD Where ""CardType"" = 'C' AND ""CardCode"" LIKE '%-P'";

            if (where.Count != 0) {
                query += " AND " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"].ToObject<List<ClientSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                 From OCRD Where ""CardType"" = 'C' AND ""CardCode"" LIKE '%-P' ";

            if (where.Count != 0) {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0]["COUNT"].ToObject<int>();

            var respose = new ClientSearchResponse
            {
                Data = orders,
                Draw = request.Draw,
                RecordsFiltered = COUNT,
                RecordsTotal = COUNT,
            };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(respose);
        }

        [HttpGet("CRMClientToSell/{id}")]
        public async Task<IActionResult> GetCRMClientToSell(string id)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    contact.""ListNum"",
                    payment.""GroupNum"",
                    payment.""PymntGroup"",
                    ""SlpName"",
                    ""ListName""
                From OCRD contact
                JOIN OSLP seller ON contact.""SlpCode"" = seller.""SlpCode""
                JOIN OCTG payment ON payment.""GroupNum"" = contact.""GroupNum""
                JOIN OPLN priceList ON priceList.""ListNum"" = contact.""ListNum"" 
                Where ""CardCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            if (oRecSet.RecordCount == 0) {
                return NotFound("No Existe Contacto");
            }

            JToken contact = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(contact);
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [HttpGet("CRM/{id}")]
        public async Task<IActionResult> GetCRMID(string id)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (!context.oCompany.Connected)
            {
                int code = context.oCompany.Connect();
                if (code != 0)
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.BusinessPartners items = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);
            SAPbobsCOM.SalesPersons seller = (SAPbobsCOM.SalesPersons)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oSalesPersons);
            SAPbobsCOM.PaymentTermsTypes payment = (SAPbobsCOM.PaymentTermsTypes)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPaymentTermsTypes);
            SAPbobsCOM.UserTable sboTable = (SAPbobsCOM.UserTable)context.oCompany.UserTables.Item("SO1_01FORMAPAGO");

            JToken pagos = context.XMLTOJSON(sboTable.GetAsXML())["OCRD"];

            if (items.GetByKey(id))
            {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["OCRD"] = temp["OCRD"][0];

                if (seller.GetByKey(temp["OCRD"]["SlpCode"].ToObject<int>()))
                {
                    JToken temp2 = context.XMLTOJSON(seller.GetAsXML());
                    temp["OSLP"] = temp2["OSLP"][0];
                }
                if (payment.GetByKey(temp["OCRD"]["GroupNum"].ToObject<int>()))
                {
                    JToken temp3 = context.XMLTOJSON(payment.GetAsXML());
                    temp["OCTG"] = temp3["OCTG"][0];
                }

                temp["PAGO"] = pagos;
                return Ok(temp);
            }
            return NotFound("No Existe Contacto");
        }

        // GET: api/Contact/CRMList
        [HttpGet("CRMList")]
        public async Task<IActionResult> GetCRMList()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (!context.oCompany.Connected)
            {
                int code = context.oCompany.Connect();
                if (code != 0)
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"CardCode\", \"CardName\", \"CardFName\"  From OCRD Where \"CardType\" = 'C' AND \"CardCode\" LIKE '%-P'");
            oRecSet.MoveFirst();
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //return Ok(context.comp(contacts, 0).Result);
            return Ok(contacts);
        }

        // GET: api/Contact/CRMListProveedor
        [HttpGet("CRMListProveedor")]
        public async Task<IActionResult> GetCRMListProveedor()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (!context.oCompany.Connected)
            {
                int code = context.oCompany.Connect();
                if (code != 0)
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"CardCode\", \"CardName\", \"Currency\", \"CardFName\"  From OCRD Where \"CardType\" = 'S'");
            oRecSet.MoveFirst();
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(contacts);
        }

        // GET: api/Contact/APPCRM/200
        [HttpGet("APPCRM/{id}")]
        public async Task<IActionResult> GetAPPCRM(int id)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (!context.oCompany.Connected)
            {
                int code = context.oCompany.Connect();
                if (code != 0)
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    ""Address"",
                    ""ZipCode"",
                    ""Country"",
                    ""Block"",
                    ""GroupNum"",
                    ""ListNum""
                From OCRD Where ""CardType"" = 'C' AND ""SlpCode"" = " + id + @" AND ""CardCode"" LIKE '%-P'");
            oRecSet.MoveFirst();
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //return Ok(await context.comp(contacts, 3));
            return Ok(contacts);
        }

        // GET: api/Contact/APPCRM
        [HttpGet("APPCRM")]
        public async Task<IActionResult> GetAPPCRMs()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (!context.oCompany.Connected)
            {
                int code = context.oCompany.Connect();
                if (code != 0)
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    ""Address"",
                    ""ZipCode"",
                    ""Country"",
                    ""Block"",
                    ""GroupNum"",
                    ""ListNum""
                From OCRD Where ""CardType"" = 'C' AND ""CardCode"" LIKE '%-P'");
            oRecSet.MoveFirst();
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //return Ok(await context.comp(contacts, 3));
            return Ok(contacts);
        }

    }
}
