using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase {

        /// <summary>
        /// Get Client List to CRM web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>ClientSearchResponse</returns>
        /// <response code="200">ClientSearchResponse(SearchResponse)</response>
        // POST: api/Contact/Clients/Search
        [ProducesResponseType(typeof(ClientSearchResponse), StatusCodes.Status200OK)]
        [HttpPost("Clients/Search")]
        public async Task<IActionResult> GetClients([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
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
                From OCRD Where ""CardType"" = 'C' AND ""CardCode"" NOT LIKE '%-D'";

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
                 From OCRD Where ""CardType"" = 'C' AND ""CardCode"" NOT LIKE '%-D' ";

            if (where.Count != 0) {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0]["COUNT"].ToObject<int>();

            ClientSearchResponse respose = new ClientSearchResponse {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };

            return Ok(respose);
        }

        /// <summary>
        /// Get Provider List to CRM web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>ProviderSearchResponse</returns>
        /// <response code="200">ProviderSearchResponse(SearchResponse)</response>
        // POST: api/Contact/Providers/Search
        [ProducesResponseType(typeof(ProviderSearchResponse), StatusCodes.Status200OK)]
        [HttpPost("Providers/Search")]
        public async Task<IActionResult> GetProviders([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();

            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(\"CardCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(\"CardName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(\"CardFName\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty) {
                where.Add($"LOWER(\"Currency\") Like LOWER('%{request.columns[3].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY \"CardCode\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY \"CardName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY \"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 3) {
                orderby = $" ORDER BY \"Currency\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY \"CardCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select ""CardCode"", ""CardName"", ""CardFName"", ""Currency""
                From OCRD Where ""CardType"" = 'S' ";

            if (where.Count != 0) {
                query += " AND " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"].ToObject<List<ProviderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                 From OCRD Where ""CardType"" = 'S' ";

            if (where.Count != 0) {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0]["COUNT"].ToObject<int>();

            ProviderSearchResponse respose = new ProviderSearchResponse {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };

            return Ok(respose);
        }

        /// <summary>
        /// Get Client Info to CRM 
        /// </summary>
        /// <param name="CardCode">CardCode. A String that serve as Client identifier.</param>
        /// <returns>A Client Basic Info</returns>
        /// <response code="200">Returns Client Info</response>
        /// <response code="204">No Client Found</response>
        // GET: api/Contact/CRMClientToSell/:CardCode
        [ProducesResponseType(typeof(ContactToSell), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("CRMClientToSell/{CardCode}")]
        public async Task<IActionResult> GetCRMClientToSell(string CardCode) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    contact.""ListNum"",
                    paymentTerm.""GroupNum"",
                    paymentTerm.""PymntGroup"",
                    paymentMethod.""PayMethCod"",
                    paymentMethod.""Descript"",
                    ""SlpName"",
                    ""Balance"",
                    ""ListName""
                From OCRD contact
                JOIN OSLP seller ON contact.""SlpCode"" = seller.""SlpCode""
                JOIN OCTG paymentTerm ON paymentTerm.""GroupNum"" = contact.""GroupNum""
                JOIN OPLN priceList ON priceList.""ListNum"" = contact.""ListNum""
                LEFT JOIN OPYM paymentMethod ON paymentMethod.""PayMethCod"" = contact.""PymCode""
                Where ""CardCode"" = '" + CardCode + "'");

            if (oRecSet.RecordCount == 0) {
                return NoContent();
            }

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0];

            oRecSet.DoQuery(@"
                Select
                    paymentMethodCardCode.""PymCode"",
                    paymentMethod.""Descript""
                From CRD2 paymentMethodCardCode
                JOIN OPYM paymentMethod ON paymentMethod.""PayMethCod"" = paymentMethodCardCode.""PymCode""
                Where ""CardCode"" = '" + CardCode  + "'");

            temp["PaymentMethods"] = context.XMLTOJSON(oRecSet.GetAsXML())["CRD2"];

            ContactToSell ContactOutput = temp.ToObject<ContactToSell>();

            //Force Garbage Collector. Recommendation by InterLatin Dude. SDK Problem with memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(ContactOutput);
        }

        // TODO: Class To Serialize Result.
        /// <summary>
        /// Get Provider Info to CRM 
        /// </summary>
        /// <param name="CardCode">CardCode. A String that serve as Provider identifier.</param>
        /// <returns>A Provider Basic Info</returns>
        /// <response code="200">Returns Provider Info</response>
        /// <response code="204">No Provider Found</response>
        // GET: api/Contact/CRMProviderToBuy/:CardCode
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("CRMProviderToBuy/{CardCode}")]
        public async Task<IActionResult> GetCRMProviderToBuy(string CardCode) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    ""Currency""
                From OCRD
                Where ""CardCode"" = '" + CardCode + "'");

            if (oRecSet.RecordCount == 0) {
                return NoContent();
            }

            JToken contact = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0];

            //Force Garbage Collector. Recommendation by InterLatin Dude. SDK Problem with memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(contact);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [HttpGet("CRM/{id}")]
        public async Task<IActionResult> GetCRMID(string id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.BusinessPartners items = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);
            SAPbobsCOM.SalesPersons seller = (SAPbobsCOM.SalesPersons)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oSalesPersons);
            SAPbobsCOM.PaymentTermsTypes payment = (SAPbobsCOM.PaymentTermsTypes)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPaymentTermsTypes);
            SAPbobsCOM.UserTable sboTable = (SAPbobsCOM.UserTable)context.oCompany.UserTables.Item("SO1_01FORMAPAGO");

            JToken pagos = context.XMLTOJSON(sboTable.GetAsXML())["OCRD"];

            if (items.GetByKey(id)) {

                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["OCRD"] = temp["OCRD"][0];

                if (seller.GetByKey(temp["OCRD"]["SlpCode"].ToObject<int>())) {
                    JToken temp2 = context.XMLTOJSON(seller.GetAsXML());
                    temp["OSLP"] = temp2["OSLP"][0];
                }
                if (payment.GetByKey(temp["OCRD"]["GroupNum"].ToObject<int>())) {
                    JToken temp3 = context.XMLTOJSON(payment.GetAsXML());
                    temp["OCTG"] = temp3["OCTG"][0];
                }

                temp["PAGO"] = pagos;
                return Ok(temp);
            }
            return NotFound("No Existe Contacto");
        }

        // GET: api/Contact/APPCRM/200
        [HttpGet("APPCRM/{id}")]
        public async Task<IActionResult> GetAPPCRM(int id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery($@"
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
                From OCRD employeeSales
                JOIN OHEM employee ON ""SlpCode"" = ""salesPrson""
                Where ""CardType"" = 'C' AND ""empID"" = {id} AND ""CardCode"" NOT LIKE '%-D'");
            
            if (oRecSet.RecordCount == 0) {
                return Ok(new List<string>());
            }

            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            return Ok(contacts);
        }

        // GET: api/Contact/APPCRM
        [HttpGet("APPCRM")]
        public async Task<IActionResult> GetAPPCRMs() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
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
                From OCRD Where ""CardType"" = 'C' AND ""CardCode"" NOT LIKE '%-D'");
            
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(contacts);
        }

        [HttpGet("{CardCode}")]
        public async Task<IActionResult> Get(string CardCode) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.BusinessPartners Bp = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);

            if (!Bp.GetByKey(CardCode)) {
                return NoContent();
            }
            JToken output = context.XMLTOJSON(Bp.GetAsXML());
            return Ok(output);

        }

    }
}
