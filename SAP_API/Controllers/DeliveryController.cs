using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class DeliveryController : ControllerBase {

        // Note: Use OrderSearchResponse because the answer is composed of the same attributes.
        // If these change, a new class would have to be used that adapts to the new data.
        /// <summary>
        /// Get Delivery List to WMS web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>OrderSearchResponse</returns>
        /// <response code="200">OrderSearchResponse(SearchResponse)</response>
        // POST: api/Delivery/Search
        [ProducesResponseType(typeof(OrderSearchResponse), StatusCodes.Status200OK)]
        [HttpPost("Search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request) {

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
                From ODLN ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                query += "Where " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["ODLN"].ToObject<List<OrderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From ODLN ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                queryCount += "Where " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["ODLN"][0]["COUNT"].ToObject<int>();

            OrderSearchResponse respose = new OrderSearchResponse {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            return Ok(respose);
        }

        // TODO: Class To Serialize Result.
        /// <summary>
        /// Get Delivery Detail to WMS Delivery Detail Page
        /// </summary>
        /// <param name="DocEntry">DocEntry. An Unsigned Integer that serve as Document identifier.</param>
        /// <returns>A Delivery Detail</returns>
        /// <response code="200">Returns Delivery Detail</response>
        /// <response code="204">No Delivery Found</response>
        // GET: api/Delivery/:DocEntry
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("Detail/{DocEntry}")]
        public async Task<IActionResult> GetDetail(int DocEntry) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery($@"
                SELECT
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    to_char(to_date(SUBSTRING(ord.""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",
                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(ord.""CancelDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""CancelDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",

                    ord.""DocTime"",
                    ord.""Address"",
                    ord.""Address2"",
                    ord.""DocCur"",
                    ord.""Comments"",
                    ord.""DocTotal"",
                    ord.""DocTotalFC"",
                    ord.""DocRate"",
                    payment.""PymntGroup"",
                    contact.""CardName"",
                    contact.""CardCode"",
                    contact.""CardFName"",
                    contact.""ListNum"",
                    employee.""SlpCode"",
                    employee.""SlpName"",
                    warehouse.""WhsCode"",
                    warehouse.""WhsName""
                FROM ODLN ord
                LEFT JOIN NNM1 series ON series.""Series"" = ord.""Series""
                LEFT JOIN OWHS warehouse ON warehouse.""WhsCode"" = series.""SeriesName""
                LEFT JOIN OSLP employee ON employee.""SlpCode"" = ord.""SlpCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OCRD contact ON contact.""CardCode"" = ord.""CardCode""
                WHERE ord.""DocEntry"" = '{DocEntry}';");

            if (oRecSet.RecordCount == 0) {
                return NoContent();
            }

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["ODLN"][0];

            oRecSet.DoQuery($@"
                Select
                    ""LineNum"",
                    ""ItemCode"",
                    ""Dscription"",
                    ""Price"",
                    ""Currency"",
                    ""Quantity"",
                    ""UomCode"",
                    ""InvQty"",
                    ""OpenQty"",
                    ""UomEntry"",
                    ""UomCode2"",
                    ""LineTotal"",
                    ""U_CjsPsVr"",
                    ""TotalFrgn"",
                    ""Rate""
                From DLN1 Where ""DocEntry"" = '{DocEntry}';");

            temp["DLN1"] = context.XMLTOJSON(oRecSet.GetAsXML())["DLN1"];

            return Ok(temp);
        }

        /// <summary>
        /// Add a Delivery Document Linked to a Order Document.
        /// </summary>
        /// <param name="value">A Delivery Parameters</param>
        /// <returns>Message</returns>
        /// <response code="200">Delivery Added</response>
        /// <response code="400">Error</response>
        /// <response code="204">Document not Found</response>
        // POST: api/Delivery/SAP
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpPost("SAP")]
        //[Authorize]
        public async Task<IActionResult> PostDelivery([FromBody] Delivery value)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            int DeliveryDocumentCount = value.DeliveryRows.Select(row => row.DeliveryRowDetailList).Select(Detail => Detail.Count).Max();
            SAPbobsCOM.Documents[] deliveryList = new SAPbobsCOM.Documents[DeliveryDocumentCount];

            for (int i = 0; i < deliveryList.Length; i++)
            {
                deliveryList[i] = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDeliveryNotes);
            }

            if (!order.GetByKey(value.DocEntry))
            {
                return NoContent();
            }

            oRecSet.DoQuery($@"
                Select
                    serie1.""SeriesName"",
                    serie1.""Series"",
                    serie1.""ObjectCode"",
                    serie2.""SeriesName""as s1,
                    serie2.""Series"" as s2,
                    serie2.""ObjectCode"" as s3
                From NNM1 serie1
                JOIN NNM1 serie2 ON serie1.""SeriesName"" = serie2.""SeriesName""
                Where serie1.""ObjectCode"" = 15 AND serie2.""Series"" = '{order.Series}';");



            if (oRecSet.RecordCount == 0)
            {
                return BadRequest("Error Con la Sucursal");
            }



            int Serie = context.XMLTOJSON(oRecSet.GetAsXML())["NNM1"][0]["Series"].ToObject<int>();

            oRecSet.DoQuery($@"
                Select
                    s.""U_D1""
                From ""@IL_CECOS"" s
                Where s.""Code"" = '{value.whsCode}';");

            oRecSet.MoveFirst();

            if (oRecSet.RecordCount == 0)
            {
                return BadRequest("Cuenta de Centros de Costo No Encontrada Para Ese Almacen. " +  order.Series  +"");
            }


            string cuenta = (string)oRecSet.Fields.Item("U_D1").Value;

            for (int i = 0; i < deliveryList.Length; i++)
            {
                deliveryList[i].CardCode = order.CardCode;
                deliveryList[i].DocDate = DateTime.Now;
                deliveryList[i].DocDueDate = DateTime.Now;
                deliveryList[i].Series = Serie;
            }

            for (int i = 0; i < value.DeliveryRows.Count; i++)
            {

                for (int j = 0; j < value.DeliveryRows[i].DeliveryRowDetailList.Count; j++)
                {

                    deliveryList[j].Lines.BaseEntry = order.DocEntry;
                    deliveryList[j].Lines.BaseLine = value.DeliveryRows[i].LineNum;
                    deliveryList[j].Lines.UoMEntry = value.DeliveryRows[i].DeliveryRowDetailList[j].UomEntry;
                    deliveryList[j].Lines.CostingCode = cuenta;
                    deliveryList[j].Lines.UserFields.Fields.Item("U_IL_CeCo").Value = cuenta;
                    deliveryList[j].Lines.BaseType = (int)SAPbobsCOM.BoAPARDocumentTypes.bodt_Order;
                    deliveryList[j].Lines.Quantity = value.DeliveryRows[i].DeliveryRowDetailList[j].Count;

                    oRecSet.DoQuery(@"
                                Select
                                    ""NumInBuy"",
                                    ""IUoMEntry"",
                                    ""QryGroup51"",
                                    ""ManBtchNum"",
                                    ""U_IL_TipPes""
                                From OITM Where ""ItemCode"" = '" + value.DeliveryRows[i].DeliveryRowDetailList[j].ItemCode + "'");
                    oRecSet.MoveFirst();
                    //double price = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["NumInBuy"].ToObject<double>();
                    string ba = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["ManBtchNum"].ToObject<string>();
                    string pe = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["U_IL_TipPes"].ToObject<string>();

                    if (ba.Equals("Y"))
                    {
                        for (int k = 0; k < value.DeliveryRows[i].DeliveryRowDetailList[j].BatchList.Count; k++)
                        {

                            deliveryList[j].Lines.BatchNumbers.BaseLineNumber = deliveryList[j].Lines.LineNum;
                            deliveryList[j].Lines.BatchNumbers.BatchNumber = value.DeliveryRows[i].DeliveryRowDetailList[j].BatchList[k].Code;
                            deliveryList[j].Lines.BatchNumbers.Quantity = value.DeliveryRows[i].DeliveryRowDetailList[j].BatchList[k].Quantity;
                            deliveryList[j].Lines.BatchNumbers.Add();
                        }
                    }

                    deliveryList[j].Lines.Add();
                }
            }

            StringBuilder Errors = new StringBuilder();
            for (int i = 0; i < deliveryList.Length; i++)
            {
                if (deliveryList[i].Add() != 0)
                {
                    Errors.AppendLine($"Documento Numero: {i}");
                    Errors.AppendLine(context.oCompany.GetLastErrorDescription());
                }
            }

            if (Errors.Length != 0)
            {
                string error = Errors.ToString();
                return BadRequest(error);
            }

            //Force Garbage Collector. Recommendation by InterLatin Dude. SDK Problem with memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok();
        }


        // POST: api/Delivery
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DeliveryOld value)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Documents delivery = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDeliveryNotes);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (order.GetByKey(value.order))
            {
                delivery.CardCode = order.CardCode;
                //delivery.DocDate = DateTime.Now;
                delivery.DocDueDate = DateTime.Now;

                oRecSet.DoQuery(@"
                Select
                    serie1.""SeriesName"",
                    serie1.""Series"",
                    serie1.""ObjectCode"",
                    serie2.""SeriesName""as s1,
                    serie2.""Series"" as s2,
                    serie2.""ObjectCode"" as s3
                From NNM1 serie1
                JOIN NNM1 serie2 ON serie1.""SeriesName"" = serie2.""SeriesName""
                Where serie1.""ObjectCode"" = 15 AND serie2.""Series"" = '" + order.Series + "'");
                oRecSet.MoveFirst();
                delivery.Series = context.XMLTOJSON(oRecSet.GetAsXML())["NNM1"][0]["Series"].ToObject<int>();

                for (int i = 0; i < value.products.Count; i++)
                {
                    //delivery.Lines.ItemCode = value.products[i].ItemCode;
                    //delivery.Lines.Quantity = value.products[i].Count;
                    //delivery.Lines.UoMEntry = value.products[i].UoMEntry;

                    //delivery.Lines.WarehouseCode = value.products[i].WarehouseCode;
                    delivery.Lines.BaseEntry = order.DocEntry;
                    delivery.Lines.BaseLine = value.products[i].Line;
                    delivery.Lines.BaseType = 17;
                    delivery.Lines.Quantity = value.products[i].Count;

                    for (int j = 0; j < value.products[i].batch.Count; j++)
                    {
                        delivery.Lines.BatchNumbers.BaseLineNumber = delivery.Lines.LineNum;
                        delivery.Lines.BatchNumbers.BatchNumber = value.products[i].batch[j].name;
                        delivery.Lines.BatchNumbers.Quantity = value.products[i].batch[j].quantity;
                        delivery.Lines.BatchNumbers.Add();
                    }

                    delivery.Lines.Add();
                }

                //delivery.Comments = "Test";
                int result = delivery.Add();
                if (result == 0)
                {
                    return Ok(new { value });
                }
                else
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }

            }

            return BadRequest(new { error = "No Existe Documento" });
        }



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // GET: api/Delivery/list/20191022
        [HttpGet("list/{date}")]
        public async Task<IActionResult> GetList(string date) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDeliveryNotes);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From ODLN Where \"DocDate\" = '" + date + "'");
            int rc = oRecSet.RecordCount;
            if (rc == 0) {
                return NotFound();
            }
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false) {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["ODLN"] = temp["ODLN"][0];
                // temp["PDN4"]?.Parent.Remove();
                // temp["PDN12"]?.Parent.Remove();
                // temp["BTNT"]?.Parent.Remove();
                list.Add(temp);
                items.Browser.MoveNext();
            }

            return Ok(list);
        }

        
        // POST: api/Delivery/MASS
        [HttpPost("MASS")]
        public async Task<IActionResult> PostMASS([FromBody] DeliveryModelMASS value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents delivery = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryGenExit);

            delivery.DocDate = value.date;
            delivery.TaxDate = value.date;
            delivery.Comments = value.comments;

            for (int i = 0; i < value.product.Count; i++) {
                delivery.Lines.ItemCode = value.product[i].ItemCode;
                //delivery.Lines.UoMEntry = 6;
                delivery.Lines.Quantity = value.product[i].Count;
                delivery.Lines.AccountCode = value.product[i].AccCode;
                delivery.Lines.WarehouseCode = value.sucursal;
                //Console.WriteLine(value.product[i].batch.Count);
                for (int j = 0; j < value.product[i].batch.Count; j++) {
                    delivery.Lines.BatchNumbers.BaseLineNumber = delivery.Lines.LineNum;
                    delivery.Lines.BatchNumbers.BatchNumber = value.product[i].batch[j].BatchNum;
                    delivery.Lines.BatchNumbers.Quantity = value.product[i].batch[j].Quantity;
                    delivery.Lines.BatchNumbers.Add();
                }

                delivery.Lines.Add();
            }

            int result = delivery.Add();
            if (result == 0) {
                return Ok(new { value });
            } else {
                string error = context.oCompany.GetLastErrorDescription();
                return BadRequest(new { error });
            }

        }
    }
}
