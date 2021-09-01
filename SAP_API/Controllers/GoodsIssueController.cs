using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GoodsIssueController : ControllerBase {

        // Note: Use GoodsReceiptSearchResponse because the answer is composed of the same attributes.
        // If these change, a new class would have to be used that adapts to the new data.
        /// <summary>
        /// Get GoodsIssue List to WMS web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>GoodsReceiptSearchResponse</returns>
        /// <response code="200">GoodsReceiptSearchResponse(SearchResponse)</response>
        // POST: api/GoodsIssue/Search
        [ProducesResponseType(typeof(GoodsReceiptSearchDetail), StatusCodes.Status200OK)]
        [HttpPost("Search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(document.\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(warehouse.\"WhsName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {

                List<string> whereOR = new List<string>();
                if ("Abierto".Contains(request.columns[2].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"document.""DocStatus"" = 'O' ");
                }
                if ("Cerrado".Contains(request.columns[2].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"document.""DocStatus"" = 'C' ");
                }
                if ("Cancelado".Contains(request.columns[2].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"document.""CANCELED"" = 'Y' ");
                }

                string whereORClause = "(" + String.Join(" OR ", whereOR) + ")";
                where.Add(whereORClause);
            }
            if (request.columns[3].search.value != String.Empty) {
                where.Add($"to_char(to_date(SUBSTRING(document.\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[3].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY document.\"DocNum\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY warehouse.\"WhsName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY document.\"DocStatus\" {request.order[0].dir}";
            } else if (request.order[0].column == 3) {
                orderby = $" ORDER BY document.\"DocDate\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY document.\"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    document.""DocEntry"",
                    document.""DocNum"",
                    to_char(to_date(SUBSTRING(document.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when document.""CANCELED"" = 'Y' then 'Cancelado'
                    when document.""DocStatus"" = 'O' then 'Abierto'
                    when document.""DocStatus"" = 'C' then 'Cerrado'
                    else document.""DocStatus"" end)  AS  ""DocStatus"",

                    warehouse.""WhsName""
                From OIGE document
                LEFT JOIN NNM1 serie ON document.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode"" ";

            if (where.Count != 0) {
                query += "Where " + whereClause;
            }

            query += orderby;

            if (request.length != -1) {
                query += " LIMIT " + request.length + " OFFSET " + request.start + "";
            }

            oRecSet.DoQuery(query);
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OIGE"].ToObject<List<GoodsReceiptSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From OIGE document
                LEFT JOIN NNM1 serie ON document.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode"" ";

            if (where.Count != 0) {
                queryCount += "Where " + whereClause;
            }

            oRecSet.DoQuery(queryCount);
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OIGE"][0]["COUNT"].ToObject<int>();

            GoodsReceiptSearchResponse respose = new GoodsReceiptSearchResponse {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            return Ok(respose);
        }

        // Class To Serialize GoodsIssue Query Result 
        class GoodsIssueDetailLine {
            public string ItemCode { set; get; }
            public string Dscription { set; get; }
            public double Quantity { set; get; }
            public string UomCode { set; get; }
            public string UomCode2 { set; get; }
            public double InvQty { set; get; }
        }

        // Class To Serialize GoodsIssue Query Result
        class GoodsIssueDetail {
            public uint DocEntry { get; set; }
            public uint DocNum { set; get; }
            public string DocDate { set; get; }
            public string DocStatus { set; get; }
            public string WhsName { set; get; }
            public List<GoodsIssueDetailLine> Lines { set; get; }
        };

        /// <summary>
        /// Get GoodsIssue Detail to WMS GoodsIssue Detail Page
        /// </summary>
        /// <param name="DocEntry">DocEntry. An Unsigned Integer that serve as Document identifier.</param>
        /// <returns>A GoodsIssue Detail</returns>
        /// <response code="200">Returns GoodsIssue Detail</response>
        /// <response code="204">No GoodsIssue Found</response>
        // GET: api/GoodsIssue/:DocEntry
        [ProducesResponseType(typeof(GoodsIssueDetail), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("{DocEntry}")]
        public async Task<IActionResult> Get(uint DocEntry) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery($@"
                SELECT
                    document.""DocEntry"",
                    document.""DocNum"",
                    to_char(to_date(SUBSTRING(document.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when document.""CANCELED"" = 'Y' then 'Cancelado'
                    when document.""DocStatus"" = 'O' then 'Abierto'
                    when document.""DocStatus"" = 'C' then 'Cerrado'
                    else document.""DocStatus"" end)  AS  ""DocStatus"",

                    warehouse.""WhsName""
                FROM OIGE document
                LEFT JOIN NNM1 series ON series.""Series"" = document.""Series""
                LEFT JOIN OWHS warehouse ON warehouse.""WhsCode"" = series.""SeriesName""
                WHERE document.""DocEntry"" = '{DocEntry}';");

            if (oRecSet.RecordCount == 0) {
                return NoContent();
            }

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["OIGE"][0];

            oRecSet.DoQuery($@"
                Select
                    ""ItemCode"",
                    ""Dscription"",
                    ""Quantity"",
                    ""UomCode"",
                    ""InvQty"",
                    ""UomCode2""
                From IGE1 Where ""DocEntry"" = '{DocEntry}';");
            temp["Lines"] = context.XMLTOJSON(oRecSet.GetAsXML())["IGE1"];

            GoodsIssueDetail output = temp.ToObject<GoodsIssueDetail>();

            //Force Garbage Collector. Recommendation by InterLatin Dude. SDK Problem with memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(output);
        }

    }
}
