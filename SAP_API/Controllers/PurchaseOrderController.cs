using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0) {
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~Remove 2nd DB

            //1 DB Config
            //SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~1 DB Config

            List<string> where = new List<string>();

            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(purchaseOrder.\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
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
                    whereOR.Add(@"purchaseOrder.""DocStatus"" = 'O' ");
                }
                if ("Cerrado".Contains(request.columns[4].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"purchaseOrder.""DocStatus"" = 'C' ");
                }
                if ("Cancelado".Contains(request.columns[4].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"purchaseOrder.""CANCELED"" = 'Y' ");
                }

                string whereORClause = "(" + String.Join(" OR ", whereOR) + ")";
                where.Add(whereORClause);
            }
            if (request.columns[5].search.value != String.Empty) {
                where.Add($"to_char(to_date(SUBSTRING(purchaseOrder.\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[5].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY purchaseOrder.\"DocNum\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY contact.\"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY contact.\"CardName\" {request.order[0].dir}";
            } else if (request.order[0].column == 3) {
                orderby = $" ORDER BY warehouse.\"WhsName\" {request.order[0].dir}";
            } else if (request.order[0].column == 4) {
                orderby = $" ORDER BY purchaseOrder.\"DocStatus\" {request.order[0].dir}";
            } else if (request.order[0].column == 5) {
                orderby = $" ORDER BY purchaseOrder.\"DocDate\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY purchaseOrder.\"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    purchaseOrder.""DocEntry"",
                    purchaseOrder.""DocNum"",

                    to_char(to_date(SUBSTRING(purchaseOrder.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when purchaseOrder.""CANCELED"" = 'Y' then 'Cancelado'
                    when purchaseOrder.""DocStatus"" = 'O' then 'Abierto'
                    when purchaseOrder.""DocStatus"" = 'C' then 'Cerrado'
                    else purchaseOrder.""DocStatus"" end)  AS  ""DocStatus"",

                    purchaseOrder.""CardName"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From OPOR purchaseOrder
                LEFT JOIN NNM1 serie ON purchaseOrder.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON purchaseOrder.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                query += "Where " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            List<PurchaseOrderSearchDetail> orders = context.XMLTOJSON(oRecSet.GetAsXML())["OPOR"].ToObject<List<PurchaseOrderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From OPOR purchaseOrder
                LEFT JOIN NNM1 serie ON purchaseOrder.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON purchaseOrder.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                queryCount += "Where " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OPOR"][0]["COUNT"].ToObject<int>();

            PurchaseOrderSearchResponse respose = new PurchaseOrderSearchResponse {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(respose);
        }

        // GET: api/PurchaseOrder/WMSDetail/(DocEntry)
        [HttpGet("WMSDetail/{DocEntry}")]
        public async Task<IActionResult> GetWMSDetail(int DocEntry) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            PurchaseOrderDetail purchaseOrderDetail;
            JToken purchaseOrder;
            string DocCur;

            oRecSet.DoQuery(@"
                Select
                    purchaseOrder.""DocEntry"",
                    purchaseOrder.""DocNum"",
                    purchaseOrder.""DocCur"",

                    (case when purchaseOrder.""DocCur"" = 'USD' then purchaseOrder.""DocTotalFC""
                    else purchaseOrder.""DocTotal"" end)  AS  ""Total"",
                    
                    to_char(to_date(SUBSTRING(purchaseOrder.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(purchaseOrder.""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",
                    to_char(to_date(SUBSTRING(purchaseOrder.""CancelDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""CancelDate"",

                    (case when purchaseOrder.""CANCELED"" = 'Y' then 'Cancelado'
                    when purchaseOrder.""DocStatus"" = 'O' then 'Abierto'
                    when purchaseOrder.""DocStatus"" = 'C' then 'Cerrado'
                    else purchaseOrder.""DocStatus"" end)  AS  ""DocStatus"",

                    purchaseOrder.""Comments"",
                    contact.""CardCode"",
                    contact.""CardName"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From OPOR purchaseOrder
                LEFT JOIN NNM1 serie ON purchaseOrder.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON purchaseOrder.""CardCode"" = contact.""CardCode""
                WHERE purchaseOrder.""DocEntry"" = '" + DocEntry + "'");
            if (oRecSet.RecordCount == 0) {
                return NotFound("No Existe Documento");
            }
            purchaseOrder = context.XMLTOJSON(oRecSet.GetAsXML())["OPOR"][0];
            DocCur = purchaseOrder["DocCur"].ToString();
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

                    (case when '" + DocCur + @"' = 'USD' then ""TotalFrgn""
                    else ""LineTotal"" end)  AS  ""Total""

                From POR1
                WHERE ""DocEntry"" = '" + DocEntry + "'");
            oRecSet.MoveFirst();
            purchaseOrder["PurchaseOrderRows"] = context.XMLTOJSON(oRecSet.GetAsXML())["POR1"];

            purchaseOrderDetail = purchaseOrder.ToObject<PurchaseOrderDetail>();

            purchaseOrder = null;
            oRecSet = null;
            DocCur = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(purchaseOrderDetail);
        }

        // GET: api/PurchaseOrder/
        [HttpGet("CRMDetail/{id}")]
        public async Task<IActionResult> GetCRMDetail(int id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0) {
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~Remove 2nd DB

            //1 DB Config
            //SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~1 DB Config

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

        // Cerrado. 1 Si valida, 0 si no valida
        // GET: api/PurchaseOrder/Reception/5/{Cerrado:1:0}
        //[HttpGet("Reception/{id}/{Cerrado}")]
        [HttpGet("Reception/{id}")]
        public async Task<IActionResult> GetReception(int id) {
            //public async Task<IActionResult> GetReception(int id, int Cerrado)
            //{
                SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocStatus"",
                    ""CardName"",
                    ""CardCode"",
                    ""U_IL_Pedimento""
                From OPOR WHERE ""DocNum"" = " + id);

            if (oRecSet.RecordCount == 0) {
                return NotFound();
            }

            JToken POrder = context.XMLTOJSON(oRecSet.GetAsXML());
            POrder["AdmInfo"]?.Parent.Remove();
            POrder["OPOR"] = POrder["OPOR"][0];

            //if (POrder["OPOR"]["DocStatus"].ToString() != "O" && Cerrado == 1) {
            //    return BadRequest("Documento Cerrado");
            //}

            if (POrder["OPOR"]["DocStatus"].ToString() != "O")
            {
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

        [HttpGet("Receptions/{id}")]
        public async Task<IActionResult> GetDetail(int id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                SELECT
                    ""DocEntry"",
                    ""DocNum"",
                    ""CardName"",
                    ""CardCode"",
                    ""U_IL_Pedimento"",
                    ""CANCELED"",
                    ""DocStatus"",
                    ""DocDate"",
                    ""Filler"",
                    ""ToWhsCode""
                From OPOR WHERE ""DocNum"" = " + id);

            if (oRecSet.RecordCount == 0) {
                return NotFound("No Existe Documento");
            }

            JToken purchaseOrder = context.XMLTOJSON(oRecSet.GetAsXML())["OPOR"][0];
            int docentry = purchaseOrder["DocEntry"].ToObject<int>();

            oRecSet.DoQuery(@"
                SELECT
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocDate"",
                    ""DocDueDate"",
                    ""ToWhsCode"",
                    ""Filler""
                FROM OPDN 
                WHERE ""DocEntry"" in (SELECT ""DocEntry"" FROM PDN1 WHERE ""BaseEntry"" = " + docentry + ")");

            if (oRecSet.RecordCount != 0) {
                purchaseOrder["PurchaseDelivery"] = context.XMLTOJSON(oRecSet.GetAsXML())["OPDN"];
            } else {
                purchaseOrder["PurchaseDelivery"] = new JArray();
            }

            return Ok(purchaseOrder);
        }




        // POST: api/PurchaseOrder
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PurchaseOrder value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0) {
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            //~Remove 2nd DB

            //1 DB Config
            //SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            //~1 DB Config



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
                return Ok(new { value = context.oCompany2.GetNewObjectKey() });
            } else {
                string error = context.oCompany2.GetLastErrorDescription();
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

        // GET: api/PurchaseOrder/list
        [HttpGet("listsolo/{date}")]
        public async Task<IActionResult> GetListSolo(string date) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<Object> list = new List<Object>();

            oRecSet.DoQuery(@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""CardName"",
                    
                    to_char(to_date(SUBSTRING(""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when ""CANCELED"" = 'Y' then 'Cancelado'
                    when ""DocStatus"" = 'O' then 'Abierto'
                    when ""DocStatus"" = 'C' then 'Cerrado'
                    else ""DocStatus"" end)  AS  ""DocStatus""

                From OPOR Where ""DocDate"" = '" + date + @"'");
            int rc = oRecSet.RecordCount;
            if (rc == 0) {
                return NotFound();
            }
            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["OPOR"];
            return Ok(temp);
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


        // GET: api/PurchaseOrder/5
        [HttpGet("DeliveriesItemCode/{DocEntry}/{itemcode}")]
        public async Task<IActionResult> GetDeliveriesItemCode(int DocEntry, string itemcode) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select ""DocEntry""
                From PDN1
                Where ""BaseEntry"" = " + DocEntry + @"
                AND ""ItemCode"" = '" + itemcode + "'");

            if (oRecSet.RecordCount == 0) {
                return Ok(new List<int>());
            }

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["PDN1"];
            return Ok(temp);
        }

        // POST: api/PurchaseOrder
        [HttpPost("Copy")]
        public async Task<IActionResult> PostCopy([FromBody] PurchaseOrderCopy  value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0) {
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Documents purchaseOrderOriginal = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            
            SAPbobsCOM.Recordset oRecSet1 = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            SAPbobsCOM.Documents purchaseOrderCopy = (SAPbobsCOM.Documents)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            
            SAPbobsCOM.Recordset oRecSet2 = (SAPbobsCOM.Recordset)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            string DocEntryString;
            int DocEntry;
            //~Remove 2nd DB
            if (context.oCompany2.InTransaction)
            {
                context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
            }
            //1 DB Config
            //SAPbobsCOM.Documents purchaseOrder = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            //~1 DB Config


            try
            {
                context.oCompany2.StartTransaction();
                if (!purchaseOrderOriginal.GetByKey(value.DocNumBase)) {
                    if (context.oCompany2.InTransaction)
                    {
                        context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    }
                    return BadRequest("No Existe Documento");
                }

                purchaseOrderCopy.CardCode = purchaseOrderOriginal.CardCode;
                purchaseOrderCopy.Series = purchaseOrderOriginal.Series;
                purchaseOrderCopy.DocDate = purchaseOrderOriginal.DocDate;
                purchaseOrderCopy.DocDueDate = purchaseOrderOriginal.DocDueDate;
                purchaseOrderCopy.UserFields.Fields.Item("U_IL_Pedimento").Value = purchaseOrderOriginal.UserFields.Fields.Item("U_IL_Pedimento").Value;
                purchaseOrderCopy.UserFields.Fields.Item("U_SO1_02FOLIOOPER").Value = purchaseOrderOriginal.DocNum.ToString();

                if (purchaseOrderOriginal.DocRate != 0) {
                    purchaseOrderCopy.DocRate = purchaseOrderOriginal.DocRate;
                }
                purchaseOrderCopy.Comments = purchaseOrderOriginal.Comments + " BASE: " + purchaseOrderOriginal.DocNum;
                purchaseOrderCopy.NumAtCard = purchaseOrderOriginal.NumAtCard;

                for (int i = 0; i < purchaseOrderOriginal.Lines.Count; i++) {
                    purchaseOrderOriginal.Lines.SetCurrentLine(i);
                    purchaseOrderCopy.Lines.ItemCode = purchaseOrderOriginal.Lines.ItemCode;
                    purchaseOrderCopy.Lines.UnitPrice = purchaseOrderOriginal.Lines.Price;
                    purchaseOrderCopy.Lines.Quantity = purchaseOrderOriginal.Lines.Quantity;
                    purchaseOrderCopy.Lines.UoMEntry = purchaseOrderOriginal.Lines.UoMEntry;
                    purchaseOrderCopy.Lines.WarehouseCode = purchaseOrderOriginal.Lines.WarehouseCode;
                    purchaseOrderCopy.Lines.Add();
                }
                for (int j = 0; j < purchaseOrderOriginal.Expenses.Count; j++) {
                    purchaseOrderOriginal.Expenses.SetCurrentLine(j);
                    if (purchaseOrderOriginal.Expenses.LineTotal != 0) {
                        purchaseOrderCopy.Expenses.ExpenseCode = purchaseOrderOriginal.Expenses.ExpenseCode;
                        if (purchaseOrderOriginal.DocCurrency == "MXN") {
                            purchaseOrderCopy.Expenses.LineTotal = purchaseOrderOriginal.Expenses.LineTotal;
                        }
                        else {
                            purchaseOrderCopy.Expenses.LineTotal = purchaseOrderOriginal.Expenses.LineTotalFC;
                        }
                        purchaseOrderCopy.Expenses.TaxCode = purchaseOrderOriginal.Expenses.TaxCode;
                        purchaseOrderCopy.Expenses.Remarks = purchaseOrderOriginal.Expenses.Remarks;
                        purchaseOrderCopy.Expenses.VatGroup = purchaseOrderOriginal.Expenses.VatGroup;
                        purchaseOrderCopy.Expenses.WTLiable = SAPbobsCOM.BoYesNoEnum.tNO;
                        purchaseOrderCopy.Expenses.DistributionMethod = purchaseOrderOriginal.Expenses.DistributionMethod;
                        purchaseOrderCopy.Expenses.Add();
                    }

                }

                int result = purchaseOrderCopy.Add();

                if (result != 0) {
                    if (context.oCompany2.InTransaction)
                    {
                        context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    }
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error, Location = "Creacion de Orden de Compra" });
                }

                DocEntryString = context.oCompany2.GetNewObjectKey();
                DocEntry = Int32.Parse(DocEntryString);
                bool r = purchaseOrderCopy.GetByKey(DocEntry);

                if (!r) {
                    if (context.oCompany2.InTransaction)
                    {
                        context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    }
                    return BadRequest("No se creo correctamente, volver a intentarlo");
                }

                oRecSet1.DoQuery(@"
                Select Distinct ""DocEntry""
                From PDN1
                Where ""BaseEntry"" = " + purchaseOrderOriginal.DocEntry + "");

                if (oRecSet1.RecordCount == 0) {
                    context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                    return Ok("Creado sin Entregas, No existentes");
                }

                JArray purchaseDeliveries = (JArray)context.XMLTOJSON(oRecSet1.GetAsXML())["PDN1"];

                List<int> DocEntries = new List<int>();
                foreach (JToken purchaseDelivery in purchaseDeliveries){
                    DocEntries.Add(purchaseDelivery["DocEntry"].ToObject<int>());
                }

                oRecSet1.DoQuery(@"
                    Select Distinct ""DocEntry""
                    From OPDN
                    Where ""DocEntry"" in (" + String.Join(", ", DocEntries) + @") AND ""CANCELED"" = 'N' ");

                if (oRecSet1.RecordCount == 0) {
                    context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                    return Ok("Creado sin Entregas, No existentes");
                }

                purchaseDeliveries = (JArray)context.XMLTOJSON(oRecSet1.GetAsXML())["OPDN"];

                DocEntries = new List<int>();
                foreach (JToken purchaseDelivery in purchaseDeliveries)
                {
                    DocEntries.Add(purchaseDelivery["DocEntry"].ToObject<int>());
                }

                for (int i = 0; i < DocEntries.Count; i++) {

                    SAPbobsCOM.Documents purchaseOrderDeliveryOriginal = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseDeliveryNotes);
                    SAPbobsCOM.Documents purchaseOrderDeliveryCopy = (SAPbobsCOM.Documents)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseDeliveryNotes);

                    Console.WriteLine(DocEntries[i]);
                    if (!purchaseOrderDeliveryOriginal.GetByKey(DocEntries[i])) {
                        if (context.oCompany2.InTransaction)
                        {
                            context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                        }
                        return BadRequest("No se trae correactamente la entrega: " + DocEntries[i]);
                    }

                    purchaseOrderDeliveryCopy.CardCode = purchaseOrderCopy.CardCode;
                    purchaseOrderDeliveryCopy.DocDate = purchaseOrderDeliveryOriginal.DocDate;
                    purchaseOrderDeliveryCopy.DocDueDate = purchaseOrderDeliveryOriginal.DocDueDate;
                    purchaseOrderDeliveryCopy.Comments = purchaseOrderDeliveryOriginal.Comments + "BASE: " + purchaseOrderDeliveryOriginal.DocNum;
                    if (purchaseOrderCopy.DocRate != 1) {
                        purchaseOrderDeliveryCopy.DocRate = purchaseOrderCopy.DocRate;
                    }

                    if (i == 0) {
                        for (int j = 0; j < purchaseOrderCopy.Expenses.Count; j++) {
                            purchaseOrderCopy.Expenses.SetCurrentLine(j);
                            if (purchaseOrderCopy.Expenses.LineTotal != 0) {
                                purchaseOrderDeliveryCopy.Expenses.ExpenseCode = purchaseOrderCopy.Expenses.ExpenseCode;
                                if (purchaseOrderCopy.DocCurrency == "MXN") {
                                    purchaseOrderDeliveryCopy.Expenses.LineTotal = purchaseOrderCopy.Expenses.LineTotal;
                                }
                                else {
                                    purchaseOrderDeliveryCopy.Expenses.LineTotal = purchaseOrderCopy.Expenses.LineTotalFC;
                                }
                                purchaseOrderDeliveryCopy.Expenses.TaxCode = purchaseOrderCopy.Expenses.TaxCode;
                                purchaseOrderDeliveryCopy.Expenses.Remarks = purchaseOrderCopy.Expenses.Remarks;
                                purchaseOrderDeliveryCopy.Expenses.VatGroup = purchaseOrderCopy.Expenses.VatGroup;
                                purchaseOrderDeliveryCopy.Expenses.WTLiable = SAPbobsCOM.BoYesNoEnum.tNO;
                                purchaseOrderDeliveryCopy.Expenses.DistributionMethod = purchaseOrderCopy.Expenses.DistributionMethod;
                                purchaseOrderDeliveryCopy.Expenses.Add();
                            }
                        }
                    }

                    for (int j = 0; j < purchaseOrderDeliveryOriginal.Lines.Count; j++) {

                        purchaseOrderDeliveryOriginal.Lines.SetCurrentLine(j);
                        purchaseOrderDeliveryCopy.Lines.BaseEntry = DocEntry;
                        purchaseOrderDeliveryCopy.Lines.BaseLine = purchaseOrderDeliveryOriginal.Lines.LineNum;
                        purchaseOrderDeliveryCopy.Lines.BaseType = 22;
                        purchaseOrderDeliveryCopy.Lines.UoMEntry = purchaseOrderDeliveryOriginal.Lines.UoMEntry;
                        purchaseOrderDeliveryCopy.Lines.UnitPrice = purchaseOrderDeliveryOriginal.Lines.UnitPrice;
                        purchaseOrderDeliveryCopy.Lines.Quantity = purchaseOrderDeliveryOriginal.Lines.Quantity;

                        for (int k = 0; k < purchaseOrderDeliveryOriginal.Lines.BatchNumbers.Count; k++) {

                            purchaseOrderDeliveryOriginal.Lines.BatchNumbers.SetCurrentLine(k);
                            purchaseOrderDeliveryCopy.Lines.BatchNumbers.BatchNumber = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.BatchNumber;
                            purchaseOrderDeliveryCopy.Lines.BatchNumbers.Quantity = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.Quantity;
                            purchaseOrderDeliveryCopy.Lines.BatchNumbers.ManufacturerSerialNumber = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.ManufacturerSerialNumber;
                            purchaseOrderDeliveryCopy.Lines.BatchNumbers.InternalSerialNumber = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.InternalSerialNumber;
                            purchaseOrderDeliveryCopy.Lines.BatchNumbers.ExpiryDate = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.ExpiryDate;
                            purchaseOrderDeliveryCopy.Lines.BatchNumbers.UserFields.Fields.Item("U_IL_CodBar").Value = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.UserFields.Fields.Item("U_IL_CodBar").Value;
                            purchaseOrderDeliveryCopy.Lines.BatchNumbers.Add();
                        }
                        purchaseOrderDeliveryCopy.Lines.Add();
                        
                    }

                    result = purchaseOrderDeliveryCopy.Add();
                    Console.WriteLine(11111111111111111111);
                    if (result != 0) {
                        string error = context.oCompany2.GetLastErrorDescription();
                        throw new Exception(error + ", Location: " + DocEntries[i]);
                    }

                }        } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                if (context.oCompany2.InTransaction) {
                    context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }
                return BadRequest( new { error = ex.Message, Location = ex.StackTrace
});
            }


            context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
            return Ok(DocEntry);

        }



        // POST: api/PurchaseOrder
        [HttpPost("Copy2")]
        public async Task<IActionResult> PostCopy2([FromBody] PurchaseOrderCopy value)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            string error;
            //Remove 2nd DB
            if (!context.oCompany2.Connected)
            {
                int code = context.oCompany2.Connect();
                if (code != 0)
                {
                    error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Documents purchaseOrderOriginal = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);

            SAPbobsCOM.Recordset oRecSet1 = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            SAPbobsCOM.Documents purchaseOrderCopy = (SAPbobsCOM.Documents)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);

            SAPbobsCOM.Recordset oRecSet2 = (SAPbobsCOM.Recordset)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            string DocEntryString;
            int DocEntry;
            //~Remove 2nd DB
            if (context.oCompany2.InTransaction)
            {
                context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
            }
            //1 DB Config
            //SAPbobsCOM.Documents purchaseOrder = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            //~1 DB Config


            context.oCompany2.StartTransaction();
            if (!purchaseOrderOriginal.GetByKey(value.DocNumBase))
            {
                if (context.oCompany2.InTransaction)
                {
                    context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }
                return BadRequest("No Existe Documento");
            }

            purchaseOrderCopy.CardCode = purchaseOrderOriginal.CardCode;
            purchaseOrderCopy.Series = purchaseOrderOriginal.Series;
            purchaseOrderCopy.DocDate = purchaseOrderOriginal.DocDate;
            purchaseOrderCopy.DocDueDate = purchaseOrderOriginal.DocDueDate;
            purchaseOrderCopy.UserFields.Fields.Item("U_IL_Pedimento").Value = purchaseOrderOriginal.UserFields.Fields.Item("U_IL_Pedimento").Value;
            purchaseOrderCopy.UserFields.Fields.Item("U_SO1_02FOLIOOPER").Value = purchaseOrderOriginal.DocNum.ToString();

            if (purchaseOrderOriginal.DocRate != 0)
            {
                purchaseOrderCopy.DocRate = purchaseOrderOriginal.DocRate;
            }
            purchaseOrderCopy.Comments = purchaseOrderOriginal.Comments + " BASE: " + purchaseOrderOriginal.DocNum;
            purchaseOrderCopy.NumAtCard = purchaseOrderOriginal.NumAtCard;

            for (int i = 0; i < purchaseOrderOriginal.Lines.Count; i++)
            {
                purchaseOrderOriginal.Lines.SetCurrentLine(i);
                purchaseOrderCopy.Lines.ItemCode = purchaseOrderOriginal.Lines.ItemCode;
                purchaseOrderCopy.Lines.UnitPrice = purchaseOrderOriginal.Lines.Price;
                purchaseOrderCopy.Lines.Quantity = purchaseOrderOriginal.Lines.Quantity;
                purchaseOrderCopy.Lines.UoMEntry = purchaseOrderOriginal.Lines.UoMEntry;
                purchaseOrderCopy.Lines.WarehouseCode = purchaseOrderOriginal.Lines.WarehouseCode;
                purchaseOrderCopy.Lines.Add();
            }
            for (int j = 0; j < purchaseOrderOriginal.Expenses.Count; j++)
            {
                purchaseOrderOriginal.Expenses.SetCurrentLine(j);
                if (purchaseOrderOriginal.Expenses.LineTotal != 0)
                {
                    purchaseOrderCopy.Expenses.ExpenseCode = purchaseOrderOriginal.Expenses.ExpenseCode;
                    if (purchaseOrderOriginal.DocCurrency == "MXN")
                    {
                        purchaseOrderCopy.Expenses.LineTotal = purchaseOrderOriginal.Expenses.LineTotal;
                    }
                    else
                    {
                        purchaseOrderCopy.Expenses.LineTotal = purchaseOrderOriginal.Expenses.LineTotalFC;
                    }
                    purchaseOrderCopy.Expenses.TaxCode = purchaseOrderOriginal.Expenses.TaxCode;
                    purchaseOrderCopy.Expenses.Remarks = purchaseOrderOriginal.Expenses.Remarks;
                    purchaseOrderCopy.Expenses.VatGroup = purchaseOrderOriginal.Expenses.VatGroup;
                    purchaseOrderCopy.Expenses.WTLiable = SAPbobsCOM.BoYesNoEnum.tNO;
                    purchaseOrderCopy.Expenses.DistributionMethod = purchaseOrderOriginal.Expenses.DistributionMethod;
                    purchaseOrderCopy.Expenses.Add();
                }

            }

            int result = purchaseOrderCopy.Add();

            if (result != 0)
            {
                if (context.oCompany2.InTransaction)
                {
                    context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }
                error = context.oCompany2.GetLastErrorDescription();
                return BadRequest(new { error, Location = "Creacion de Orden de Compra" });
            }

            DocEntryString = context.oCompany2.GetNewObjectKey();
            DocEntry = Int32.Parse(DocEntryString);
            bool r = purchaseOrderCopy.GetByKey(DocEntry);

            if (!r)
            {
                if (context.oCompany2.InTransaction)
                {
                    context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }
                return BadRequest("No se creo correctamente, volver a intentarlo");
            }

            oRecSet1.DoQuery(@"
            Select Distinct ""DocEntry""
            From PDN1
            Where ""BaseEntry"" = " + purchaseOrderOriginal.DocEntry + "");

            if (oRecSet1.RecordCount == 0)
            {
                context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                return Ok("Creado sin Entregas, No existentes");
            }

            JArray purchaseDeliveries = (JArray)context.XMLTOJSON(oRecSet1.GetAsXML())["PDN1"];

            List<int> DocEntries = new List<int>();
            foreach (JToken purchaseDelivery in purchaseDeliveries)
            {
                DocEntries.Add(purchaseDelivery["DocEntry"].ToObject<int>());
            }

            oRecSet1.DoQuery(@"
                Select Distinct ""DocEntry""
                From OPDN
                Where ""DocEntry"" in (" + String.Join(", ", DocEntries) + @") AND ""CANCELED"" = 'N' ");

            if (oRecSet1.RecordCount == 0)
            {
                context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                return Ok("Creado sin Entregas, No existentes");
            }

            purchaseDeliveries = (JArray)context.XMLTOJSON(oRecSet1.GetAsXML())["OPDN"];

            DocEntries = new List<int>();
            foreach (JToken purchaseDelivery in purchaseDeliveries)
            {
                DocEntries.Add(purchaseDelivery["DocEntry"].ToObject<int>());
            }

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Task<ResultC> t;
            //ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();

            List<Task<ResultC>> tasks = new List<Task<ResultC>>();

            for (int i = 0; i < DocEntries.Count; i++)
            {
                t = Task.Run(() => { return RegisterDelivery(i, token, context, DocEntries[i], purchaseOrderCopy, DocEntry); });
                tasks.Add(t);
            }

            error = String.Empty;

            try
            {
                //await Task.WhenAll(tasks.ToArray());
                while (tasks.Count > 0)
                {
                    // Identify the first task that completes.
                    Task<ResultC> firstFinishedTask = await Task.WhenAny(tasks);

                    if (firstFinishedTask.Result.Error != 0)
                    {
                        error = firstFinishedTask.Result.Message;
                        tokenSource.Cancel();
                        break;
                    }

                    // ***Remove the selected task from the list so that you don't
                    // process it more than once.
                    tasks.Remove(firstFinishedTask);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"\n{nameof(OperationCanceledException)} thrown\n");
            }
            catch (Exception ex)
            {
                tokenSource.Cancel();
                error = ex.Message + " ; " + ex.StackTrace;
            }
            finally
            {
                tokenSource.Dispose();
            }

            // Display status of all tasks.
            //foreach (var task in tasks)
            //    Console.WriteLine("Task {0} status is now {1}", task.Id, task.Status);

            //Console.WriteLine(ex.Message);
            //Console.WriteLine(ex.StackTrace);
            //if (context.oCompany2.InTransaction)
            //{
            //    context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
            //}
            //return BadRequest(new
            //{
            //    error = ex.Message,
            //    Location = ex.StackTrace
            //});
            if (error != String.Empty)
            {
                Console.WriteLine(error);
                if (context.oCompany2.InTransaction)
                {
                    context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }
                return BadRequest(new { error });
            }

            context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
            return Ok(DocEntry);
        }

        class ResultC
        {
            public string Message { set; get; }
            public int Error { set; get; }
        }

        static async Task<ResultC> RegisterDelivery(int taskNum, CancellationToken ct, SAPContext context, int DocEntry, SAPbobsCOM.Documents purchaseOrderCopy, int DocEntryBase)
        {
            if (ct.IsCancellationRequested)
            {
                Console.WriteLine("Task {0} was cancelled before it got started.", taskNum);
                ct.ThrowIfCancellationRequested();
            }

            SAPbobsCOM.Documents purchaseOrderDeliveryOriginal = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseDeliveryNotes);
            SAPbobsCOM.Documents purchaseOrderDeliveryCopy = (SAPbobsCOM.Documents)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseDeliveryNotes);
            int result;
            ResultC re = new ResultC();
            
            Console.WriteLine(DocEntry);
            if (!purchaseOrderDeliveryOriginal.GetByKey(DocEntry))
            {
                if (context.oCompany2.InTransaction)
                {
                    context.oCompany2.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }
                re.Error = 1;
                re.Message = "No se trae correactamente la entrega: " + DocEntry;
                return re;
            }

            purchaseOrderDeliveryCopy.CardCode = purchaseOrderCopy.CardCode;
            purchaseOrderDeliveryCopy.DocDate = purchaseOrderDeliveryOriginal.DocDate;
            purchaseOrderDeliveryCopy.DocDueDate = purchaseOrderDeliveryOriginal.DocDueDate;
            purchaseOrderDeliveryCopy.Comments = purchaseOrderDeliveryOriginal.Comments + "BASE: " + purchaseOrderDeliveryOriginal.DocNum;
            if (purchaseOrderCopy.DocRate != 1)
            {
                purchaseOrderDeliveryCopy.DocRate = purchaseOrderCopy.DocRate;
            }

            if (taskNum == 0)
            {
                for (int j = 0; j < purchaseOrderCopy.Expenses.Count; j++)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Task {0} cancelled", taskNum);
                        ct.ThrowIfCancellationRequested();
                    }

                    purchaseOrderCopy.Expenses.SetCurrentLine(j);
                    if (purchaseOrderCopy.Expenses.LineTotal != 0)
                    {
                        purchaseOrderDeliveryCopy.Expenses.ExpenseCode = purchaseOrderCopy.Expenses.ExpenseCode;
                        if (purchaseOrderCopy.DocCurrency == "MXN")
                        {
                            purchaseOrderDeliveryCopy.Expenses.LineTotal = purchaseOrderCopy.Expenses.LineTotal;
                        }
                        else
                        {
                            purchaseOrderDeliveryCopy.Expenses.LineTotal = purchaseOrderCopy.Expenses.LineTotalFC;
                        }
                        purchaseOrderDeliveryCopy.Expenses.TaxCode = purchaseOrderCopy.Expenses.TaxCode;
                        purchaseOrderDeliveryCopy.Expenses.Remarks = purchaseOrderCopy.Expenses.Remarks;
                        purchaseOrderDeliveryCopy.Expenses.VatGroup = purchaseOrderCopy.Expenses.VatGroup;
                        purchaseOrderDeliveryCopy.Expenses.WTLiable = SAPbobsCOM.BoYesNoEnum.tNO;
                        purchaseOrderDeliveryCopy.Expenses.DistributionMethod = purchaseOrderCopy.Expenses.DistributionMethod;
                        purchaseOrderDeliveryCopy.Expenses.Add();
                    }
                }
            }

            for (int j = 0; j < purchaseOrderDeliveryOriginal.Lines.Count; j++) {
                if (ct.IsCancellationRequested)
                {
                    Console.WriteLine("Task {0} cancelled", taskNum);
                    ct.ThrowIfCancellationRequested();
                }

                purchaseOrderDeliveryOriginal.Lines.SetCurrentLine(j);
                purchaseOrderDeliveryCopy.Lines.BaseEntry = DocEntryBase;
                purchaseOrderDeliveryCopy.Lines.BaseLine = purchaseOrderDeliveryOriginal.Lines.LineNum;
                purchaseOrderDeliveryCopy.Lines.BaseType = 22;
                purchaseOrderDeliveryCopy.Lines.UoMEntry = purchaseOrderDeliveryOriginal.Lines.UoMEntry;
                purchaseOrderDeliveryCopy.Lines.UnitPrice = purchaseOrderDeliveryOriginal.Lines.UnitPrice;
                purchaseOrderDeliveryCopy.Lines.Quantity = purchaseOrderDeliveryOriginal.Lines.Quantity;

                for (int k = 0; k < purchaseOrderDeliveryOriginal.Lines.BatchNumbers.Count; k++)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Task {0} cancelled", taskNum);
                        ct.ThrowIfCancellationRequested();
                    }

                    purchaseOrderDeliveryOriginal.Lines.BatchNumbers.SetCurrentLine(k);
                    purchaseOrderDeliveryCopy.Lines.BatchNumbers.BatchNumber = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.BatchNumber;
                    purchaseOrderDeliveryCopy.Lines.BatchNumbers.Quantity = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.Quantity;
                    purchaseOrderDeliveryCopy.Lines.BatchNumbers.ManufacturerSerialNumber = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.ManufacturerSerialNumber;
                    purchaseOrderDeliveryCopy.Lines.BatchNumbers.InternalSerialNumber = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.InternalSerialNumber;
                    purchaseOrderDeliveryCopy.Lines.BatchNumbers.ExpiryDate = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.ExpiryDate;
                    purchaseOrderDeliveryCopy.Lines.BatchNumbers.UserFields.Fields.Item("U_IL_CodBar").Value = purchaseOrderDeliveryOriginal.Lines.BatchNumbers.UserFields.Fields.Item("U_IL_CodBar").Value;
                    purchaseOrderDeliveryCopy.Lines.BatchNumbers.Add();
                }
                purchaseOrderDeliveryCopy.Lines.Add();

            }

            result = purchaseOrderDeliveryCopy.Add();
            if (result != 0) {
                string error = context.oCompany2.GetLastErrorDescription();
                re.Error = 2;
                re.Message = error + ", Location: " + DocEntry;
                return re;
            }
            re.Error = 0;
            return re;
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
