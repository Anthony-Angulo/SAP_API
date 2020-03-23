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
    public class PurchaseOrderController : ControllerBase {

        [HttpPost("search")]
        public async Task<IActionResult> GetSearch([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(ord.\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(contact.\"CardFName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(contact.\"CardName\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty) {
                where.Add($"LOWER(warehouse.\"WhsName\") Like LOWER('%{request.columns[3].search.value}%')");
            }
            if (request.columns[4].search.value != String.Empty) {
                List<string> whereOR = new List<string>();
                if ("Abierto".Contains(request.columns[4].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"ord.""DocStatus"" = 'O' ");
                }
                if ("Cerrado".Contains(request.columns[4].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"ord.""DocStatus"" = 'C' ");
                }
                if ("Cancelado".Contains(request.columns[4].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"ord.""CANCELED"" = 'Y' ");
                }

                string whereORClause = "(" + String.Join(" OR ", whereOR) + ")";
                where.Add(whereORClause);
            }
            if (request.columns[5].search.value != String.Empty) {
                where.Add($"to_char(to_date(SUBSTRING(ord.\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[5].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY ord.\"DocNum\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY contact.\"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY contact.\"CardName\" {request.order[0].dir}";
            } else if (request.order[0].column == 3) {
                orderby = $" ORDER BY warehouse.\"WhsName\" {request.order[0].dir}";
            } else if (request.order[0].column == 4) {
                orderby = $" ORDER BY ord.\"DocStatus\" {request.order[0].dir}";
            } else if (request.order[0].column == 5) {
                orderby = $" ORDER BY ord.\"DocDate\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY ord.\"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",

                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",

                    ord.""CardName"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From OPOR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                query += "Where " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OPOR"].ToObject<List<OrderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From OPOR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                queryCount += "Where " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OPOR"][0]["COUNT"].ToObject<int>();

            var respose = new OrderSearchResponse {
                Data = orders,
                Draw = request.Draw,
                RecordsFiltered = COUNT,
                RecordsTotal = COUNT,
            };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(respose);
        }

        // GET: api/PurchaseOrder/
        [HttpGet("CRMDetail/{id}")]
        public async Task<IActionResult> GetCRMDetail(int id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocCur"",
                    
                    ord.""DocTotal"",
                    ord.""DocTotalFC"",
                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(ord.""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",
                    to_char(to_date(SUBSTRING(ord.""CancelDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""CancelDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",

                    ord.""Comments"",
                    contact.""CardCode"",
                    contact.""CardName"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From OPOR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""
                WHERE ord.""DocEntry"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken purchaseOrder = context.XMLTOJSON(oRecSet.GetAsXML())["OPOR"][0];

            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""Dscription"",
                    ""Price"",
                    ""Currency"",
                    ""Quantity"",
                    ""UomCode"",
                    ""InvQty"",
                    ""UomCode2"",
                    ""LineTotal"",
                    ""TotalFrgn""
                From POR1
                WHERE ""DocEntry"" = '" + id + "'");
            oRecSet.MoveFirst();
            purchaseOrder["rows"] = context.XMLTOJSON(oRecSet.GetAsXML())["POR1"];
            return Ok(purchaseOrder);
        }

        // GET: api/PurchaseOrder/Reception/5
        [HttpGet("Reception/{id}")]
        public async Task<IActionResult> GetReception(int id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //oRecSet.DoQuery(@"
            //    Select
            //        ""DocEntry"",
            //        ""DocNum"",
            //        ""DocStatus"",
            //        ""CardName"",
            //        ""CardCode"",
            //        ""U_IL_Pedimento""
            //    From OPOR WHERE ""DocNum"" = " + id);

            oRecSet.DoQuery(@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocStatus"",
                    ""CardName"",
                    ""CardCode""
                From OPOR WHERE ""DocNum"" = " + id);

            if (oRecSet.RecordCount == 0) {
                return NotFound();
            }

            JToken POrder = context.XMLTOJSON(oRecSet.GetAsXML());
            POrder["AdmInfo"]?.Parent.Remove();
            POrder["OPOR"] = POrder["OPOR"][0];

            if (POrder["OPOR"]["DocStatus"].ToString() != "O") {
                return BadRequest("Documento Cerrado");
            }

            oRecSet.DoQuery(@"
                Select
                    ""LineStatus"",
                    ""LineNum"",
                    ""ItemCode"",
                    ""Dscription"",
                    ""UomEntry"",
                    ""WhsCode"",
                    ""UomCode"",
                    ""OpenInvQty"",
                    ""OpenQty""
                From POR1 WHERE ""DocEntry"" = " + POrder["OPOR"]["DocEntry"]);

            POrder["POR1"] = context.XMLTOJSON(oRecSet.GetAsXML())["POR1"];

            foreach (JToken pro in POrder["POR1"]) {
                oRecSet.DoQuery(@"
                    Select
                        ""ItemCode"",
                        ""ItemName"",
                        ""QryGroup2"",
                        ""QryGroup7"",
                        ""QryGroup41"",
                        ""QryGroup43"",
                        ""QryGroup44"",
                        ""QryGroup45"",
                        ""ManBtchNum"",
                        ""U_IL_PesMax"",
                        ""U_IL_PesMin"",
                        ""U_IL_PesProm"",
                        ""U_IL_TipPes"",
                        ""NumInSale"",
                        ""NumInBuy""
                    From OITM Where ""ItemCode"" = '" + pro["ItemCode"] + "'");
                oRecSet.MoveFirst();
                pro["Detail"] = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
                oRecSet.DoQuery(@"
                    Select
                        ""BcdEntry"",
                        ""BcdCode"",
                        ""BcdName"",
                        ""ItemCode"",
                        ""UomEntry""
                    From OBCD Where ""ItemCode"" = '" + pro["ItemCode"] + "'");
                oRecSet.MoveFirst();
                pro["CodeBars"] = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];
            }
            return Ok(POrder);
        }

        // POST: api/PurchaseOrder
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PurchaseOrder value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);

            order.CardCode = value.cardcode;
            order.Series = value.series;
            
            if (value.currencyrate != 0) {
                order.DocRate = value.currencyrate;
            }

            order.NumAtCard = value.numatcard;

            for (int i = 0; i < value.rows.Count; i++) {
                order.Lines.ItemCode = value.rows[i].code;
                order.Lines.UnitPrice = value.rows[i].price;
                order.Lines.Quantity = value.rows[i].quantity;
                order.Lines.Add();
            }

            order.Comments = value.comments;

            int result = order.Add();
            if (result == 0) {
                return Ok(new { value = context.oCompany.GetNewObjectKey() });
            } else {
                string error = context.oCompany.GetLastErrorDescription();
                return BadRequest(new { error });
            }
        }



        /// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //// GET: api/PurchaseOrder
        //[HttpGet]
        //public async Task<IActionResult> Get()
        //{
        //    SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

        //    if (!context.oCompany.Connected) {
        //        int code = context.oCompany.Connect();
        //        if (code != 0) {
        //            string error = context.oCompany.GetLastErrorDescription();
        //            return BadRequest(new { error });
        //        }
        //    }

        //    SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
        //    SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

        //    List<Object> list = new List<Object>();

        //    oRecSet.DoQuery("Select * From OPOR");
        //    items.Browser.Recordset = oRecSet;
        //    items.Browser.MoveFirst();

        //    while (items.Browser.EoF == false) {
        //        JToken temp = context.XMLTOJSON(items.GetAsXML());
        //        temp["OPOR"] = temp["OPOR"][0];
        //        list.Add(temp);
        //        items.Browser.MoveNext();
        //    }

        //    return Ok(list);
        //}

        // GET: api/PurchaseOrder/CRMList
        // Todas las Ordernes - Encabezado para lista CRM
        [HttpGet("CRMList")]
        public async Task<IActionResult> GetCRMList() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""CardName"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From OPOR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["OPOR"];
            return Ok(orders);
        }

        // GET: api/PurchaseOrder/CRMList
        // Todas las Ordernes - Encabezado para lista CRM
        [HttpGet("CRMList/Sucursal/{id}")]
        public async Task<IActionResult> GetCRMSucursalList(string id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""CardName"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From OPOR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""
                Where warehouse.""WhsCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["OPOR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }

        // GET: api/PurchaseOrder/list
        [HttpGet("list/{date}")]
        public async Task<IActionResult> GetList(string date) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From OPOR Where \"DocDate\" = '" + date + "'");
            int rc = oRecSet.RecordCount;
            if (rc == 0) {
                return NotFound();
            }
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false) {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["OPOR"] = temp["OPOR"][0];
                temp["POR4"]?.Parent.Remove();
                temp["POR12"]?.Parent.Remove();
                list.Add(temp);
                items.Browser.MoveNext();
            }

            return Ok(list);
        }

        // GET: api/PurchaseOrder/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (items.GetByKey(id)) {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["OPOR"] = temp["OPOR"][0];

                oRecSet.DoQuery(@"
                Select
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""Series"" = '" + temp["OPOR"]["Series"] + "'");

                oRecSet.MoveFirst();
                JToken series = context.XMLTOJSON(oRecSet.GetAsXML());
                temp["WHS"] = series["OWHS"][0];

                oRecSet.DoQuery("Select \"CardCode\", \"CardName\", \"Currency\", \"CardFName\"  From OCRD Where \"CardCode\" = '" + temp["OPOR"]["CardCode"] + "'");
                oRecSet.MoveFirst();
                temp["contact"] = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0];

                return Ok(temp);
            }
            return NotFound("No Existe Documento");
        }

        //// PUT: api/PurchaseOrder/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE: api/ApiWithActions/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
