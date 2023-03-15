using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Entities;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PurchaseDeliveryController : ControllerBase {

        static private ApplicationDbContext _context;

        public PurchaseDeliveryController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpPost("search")]
        public async Task<IActionResult> GetSearch([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();

            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(purchaseDelivery.\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
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
                    whereOR.Add(@"purchaseDelivery.""DocStatus"" = 'O' ");
                }
                if ("Cerrado".Contains(request.columns[4].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"purchaseDelivery.""DocStatus"" = 'C' ");
                }
                if ("Cancelado".Contains(request.columns[4].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"purchaseDelivery.""CANCELED"" = 'Y' ");
                }

                string whereORClause = "(" + String.Join(" OR ", whereOR) + ")";
                where.Add(whereORClause);
            }
            if (request.columns[5].search.value != String.Empty) {
                where.Add($"to_char(to_date(SUBSTRING(purchaseDelivery.\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[5].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY purchaseDelivery.\"DocNum\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY contact.\"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY contact.\"CardName\" {request.order[0].dir}";
            } else if (request.order[0].column == 3) {
                orderby = $" ORDER BY warehouse.\"WhsName\" {request.order[0].dir}";
            } else if (request.order[0].column == 4) {
                orderby = $" ORDER BY purchaseDelivery.\"DocStatus\" {request.order[0].dir}";
            } else if (request.order[0].column == 5) {
                orderby = $" ORDER BY purchaseDelivery.\"DocDate\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY purchaseDelivery.\"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    purchaseDelivery.""DocEntry"",
                    purchaseDelivery.""DocNum"",

                    to_char(to_date(SUBSTRING(purchaseDelivery.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when purchaseDelivery.""CANCELED"" = 'Y' then 'Cancelado'
                    when purchaseDelivery.""DocStatus"" = 'O' then 'Abierto'
                    when purchaseDelivery.""DocStatus"" = 'C' then 'Cerrado'
                    else purchaseDelivery.""DocStatus"" end)  AS  ""DocStatus"",

                    purchaseDelivery.""CardName"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From OPDN purchaseDelivery
                LEFT JOIN NNM1 serie ON purchaseDelivery.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON purchaseDelivery.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                query += "Where " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            List<PurchaseOrderSearchDetail> orders = context.XMLTOJSON(oRecSet.GetAsXML())["OPDN"].ToObject<List<PurchaseOrderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From OPDN purchaseDelivery
                LEFT JOIN NNM1 serie ON purchaseDelivery.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON purchaseDelivery.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                queryCount += "Where " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OPDN"][0]["COUNT"].ToObject<int>();

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

        // GET: api/PurchaseDelivery/Detail/(DocEntry)
        [HttpGet("WMSDetail/{DocEntry}")]
        public async Task<IActionResult> GetWMSDetail(int DocEntry) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            PurchaseDeliveryDetail purchaseDeliveryDetail;
            JToken purchaseDelivery;
            string DocCur;

            oRecSet.DoQuery(@"
                Select
                    purchaseDelivery.""DocEntry"",
                    purchaseDelivery.""DocNum"",
                    purchaseDelivery.""DocCur"",
                    
                    (case when purchaseDelivery.""DocCur"" = 'USD' then purchaseDelivery.""DocTotalFC""
                    else purchaseDelivery.""DocTotal"" end)  AS  ""Total"",

                    to_char(to_date(SUBSTRING(purchaseDelivery.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(purchaseDelivery.""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",
                    to_char(to_date(SUBSTRING(purchaseDelivery.""CancelDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""CancelDate"",

                    (case when purchaseDelivery.""CANCELED"" = 'Y' then 'Cancelado'
                    when purchaseDelivery.""DocStatus"" = 'O' then 'Abierto'
                    when purchaseDelivery.""DocStatus"" = 'C' then 'Cerrado'
                    else purchaseDelivery.""DocStatus"" end)  AS  ""DocStatus"",

                    purchaseDelivery.""Comments"",
                    contact.""CardCode"",
                    contact.""CardName"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From OPDN purchaseDelivery
                LEFT JOIN NNM1 serie ON purchaseDelivery.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON purchaseDelivery.""CardCode"" = contact.""CardCode""
                WHERE purchaseDelivery.""DocEntry"" = '" + DocEntry + "'");
            if (oRecSet.RecordCount == 0) {
                return NotFound("No Existe Documento");
            }
            purchaseDelivery = context.XMLTOJSON(oRecSet.GetAsXML())["OPDN"][0];
            DocCur = purchaseDelivery["DocCur"].ToString();
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
                From PDN1
                WHERE ""DocEntry"" = '" + DocEntry + "'");
            oRecSet.MoveFirst();
            purchaseDelivery["PurchaseDeliveryRows"] = context.XMLTOJSON(oRecSet.GetAsXML())["PDN1"];

            purchaseDeliveryDetail = purchaseDelivery.ToObject<PurchaseDeliveryDetail>();

            purchaseDelivery = null;
            oRecSet = null;
            DocCur = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(purchaseDeliveryDetail);
        }

        // GET: api/PurchaseDelivery/Detail/(DocEntry)
        [HttpGet("Detail/{id}")]
        public async Task<IActionResult> GetDetail(int id) {

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
                From OPDN ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""
                WHERE ord.""DocEntry"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken purchaseOrder = context.XMLTOJSON(oRecSet.GetAsXML())["OPDN"][0];

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
                From PDN1
                WHERE ""DocEntry"" = '" + id + "'");
            oRecSet.MoveFirst();
            purchaseOrder["rows"] = context.XMLTOJSON(oRecSet.GetAsXML())["PDN1"];
            return Ok(purchaseOrder);
        }

        // GET: api/PurchaseDelivery/Return/(DocEntry)
        [HttpGet("Return/{DocEntry}")]
        //[Authorize] 
        public async Task<IActionResult> GetReturn(int DocEntry) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocStatus"",
                    ""CardName"",
                    ""CardCode""
                From OPDN WHERE ""DocEntry"" = " + DocEntry);

            if (oRecSet.RecordCount == 0) {
                return NotFound("No exite Documento");
            }

            JToken purchaseDelivery = context.XMLTOJSON(oRecSet.GetAsXML())["OPDN"][0];

            if (purchaseDelivery["DocStatus"].ToString() != "O") {
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
                From PDN1 WHERE ""DocEntry"" = " + DocEntry);

            purchaseDelivery["PDN1"] = context.XMLTOJSON(oRecSet.GetAsXML())["PDN1"];

            foreach (JToken product in purchaseDelivery["PDN1"]) {
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
                    From OITM Where ""ItemCode"" = '" + product["ItemCode"] + "'");
                oRecSet.MoveFirst();
                product["Detail"] = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
                //oRecSet.DoQuery(@"
                //    Select
                //        ""BcdEntry"",
                //        ""BcdCode"",
                //        ""BcdName"",
                //        ""ItemCode"",
                //        ""UomEntry""
                //    From OBCD Where ""ItemCode"" = '" + pro["ItemCode"] + "'");
                //oRecSet.MoveFirst();
                //pro["CodeBars"] = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(purchaseDelivery);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // GET: api/PurchaseDelivery/list/20191022
        [HttpGet("list/{date}")]
        public async Task<IActionResult> GetList(string date) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseDeliveryNotes);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From OPDN Where \"DocDate\" = '" + date + "'");
            int rc = oRecSet.RecordCount;
            if (rc == 0) {
                return NotFound();
            }
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false) {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["OPDN"] = temp["OPDN"][0];
                temp["PDN4"]?.Parent.Remove();
                temp["PDN12"]?.Parent.Remove();
                temp["BTNT"]?.Parent.Remove();
                list.Add(temp);
                items.Browser.MoveNext();
            }

            return Ok(list);
        }

        public class ItemPrint
        {
            public string ItemCode { get; set; }

            public double count { get; set; }

            public int UomCode { get; set; }
        }
            // POST: api/PurchaseDelivery
        [HttpPost]
        //[Authorize]
        public async Task<IActionResult> Post([FromBody] PurchaseOrderDelivery value) {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents purchaseOrder = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            SAPbobsCOM.Documents purchaseOrderdelivery = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseDeliveryNotes);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<ItemPrint> itemsToPrint = new List<ItemPrint>();
            if (purchaseOrder.GetByKey(value.order)) {

                if ((String)purchaseOrder.UserFields.Fields.Item("U_IL_Pedimento").Value == String.Empty) {
                    purchaseOrder.UserFields.Fields.Item("U_IL_Pedimento").Value = value.pedimento;
                    purchaseOrder.Update();
                }

                purchaseOrderdelivery.CardCode = purchaseOrder.CardCode;
                purchaseOrderdelivery.DocDate = DateTime.Now;
                purchaseOrderdelivery.DocDueDate = DateTime.Now;
                if (purchaseOrder.DocRate != 1) {
                    purchaseOrderdelivery.DocRate = purchaseOrder.DocRate;
                }

                oRecSet.DoQuery(@"
                    Select
                        count(*) as count
                    From PDN1
                    Where ""BaseEntry"" = " + purchaseOrder.DocEntry + "");
                oRecSet.MoveFirst();
                int count = context.XMLTOJSON(oRecSet.GetAsXML())["PDN1"][0]["COUNT"].ToObject<int>();

                if (count == 0) {
                    for (int i = 0; i < purchaseOrder.Expenses.Count; i++) {
                        purchaseOrder.Expenses.SetCurrentLine(i);
                        if (purchaseOrder.Expenses.LineTotal != 0) {

                            purchaseOrderdelivery.Expenses.ExpenseCode = purchaseOrder.Expenses.ExpenseCode;
                            if (purchaseOrder.DocCurrency == "MXN") {
                                purchaseOrderdelivery.Expenses.LineTotal = purchaseOrder.Expenses.LineTotal;
                            }
                            else {
                                purchaseOrderdelivery.Expenses.LineTotal = purchaseOrder.Expenses.LineTotalFC;
                            }

                            purchaseOrderdelivery.Expenses.TaxCode = purchaseOrder.Expenses.TaxCode;
                            purchaseOrderdelivery.Expenses.Remarks = purchaseOrder.Expenses.Remarks;
                            purchaseOrderdelivery.Expenses.VatGroup = purchaseOrder.Expenses.VatGroup;
                            purchaseOrderdelivery.Expenses.WTLiable = SAPbobsCOM.BoYesNoEnum.tNO;
                            purchaseOrderdelivery.Expenses.DistributionMethod = purchaseOrder.Expenses.DistributionMethod;
                            purchaseOrderdelivery.Expenses.Add();
                        }

                    }
                }

                for (int i = 0; i < value.products.Count; i++) {
                   
                    //purchaseOrderdelivery.Lines.ItemCode = value.products[i].ItemCode;
                    //purchaseOrderdelivery.Lines.Quantity = value.products[i].Count;
                    // purchaseOrderdelivery.Lines.UoMEntry = value.products[i].UoMEntry;
                    // purchaseOrderdelivery.Lines.WarehouseCode = value.products[i].WarehouseCode;

                    purchaseOrderdelivery.Lines.BaseEntry = purchaseOrder.DocEntry;
                    purchaseOrderdelivery.Lines.BaseLine = value.products[i].Line;
                    purchaseOrderdelivery.Lines.BaseType = 22;
                    
                    if (value.products[i].UoMEntry == 116 || value.products[i].UoMEntry == 196) {
                        
                        purchaseOrder.Lines.SetCurrentLine(value.products[i].Line);
                        if (value.products[i].Group == 43) {
                            oRecSet.DoQuery(@"
                                Select
                                    ""NumInBuy"",
                                    ""IUoMEntry""
                                From OITM Where ""ItemCode"" = '" + value.products[i].ItemCode + "'");
                            oRecSet.MoveFirst();
                            double price = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["NumInBuy"].ToObject<double>();
                            purchaseOrderdelivery.Lines.UoMEntry = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["IUoMEntry"].ToObject<int>();
                            purchaseOrderdelivery.Lines.UnitPrice = purchaseOrder.Lines.UnitPrice / price;
                        } else {
                            purchaseOrderdelivery.Lines.UoMEntry = 185;
                            purchaseOrderdelivery.Lines.UnitPrice = purchaseOrder.Lines.UnitPrice * 2.20462;
                        }
                            
                        //Console.WriteLine(value.products[i].ItemType);
                        //if (value.products[i].ItemType == "F")
                        //{

                        //    Console.WriteLine(purchaseOrder.Lines.Quantity);
                        //    Console.WriteLine(purchaseOrder.Lines.UnitPrice);
                        //    Console.WriteLine(purchaseOrder.Lines.LineTotal);
                        //    Console.WriteLine(purchaseOrder.Lines.RowTotalFC);

                        //    if (purchaseOrder.Lines.Currency == "USD") {
                        //        purchaseOrderdelivery.Lines.DiscountPercent = ((value.products[i].Count * Math.Round(purchaseOrder.Lines.UnitPrice * 2.20462, 2)) * 100 / purchaseOrder.Lines.RowTotalFC) - 100;
                        //    } else {
                        //        purchaseOrderdelivery.Lines.DiscountPercent = ((value.products[i].Count * Math.Round(purchaseOrder.Lines.UnitPrice * 2.20462, 2)) * 100 / purchaseOrder.Lines.LineTotal) - 100;
                        //    }
                            
                        //    //purchaseOrderdelivery.Lines.LineTotal = purchaseOrder.Lines.LineTotal;
                        //    //purchaseOrderdelivery.Lines.RowTotalFC = purchaseOrder.Lines.RowTotalFC;
                        //    //Console.WriteLine(purchaseOrderdelivery.Lines.LineTotal);
                        //}
                         
                    }

                    purchaseOrderdelivery.Lines.Quantity = value.products[i].Count;

                    for (int j = 0; j < value.products[i].batch.Count; j++) {
                        
                        purchaseOrderdelivery.Lines.BatchNumbers.BatchNumber = value.products[i].batch[j].name;
                        purchaseOrderdelivery.Lines.BatchNumbers.Quantity = value.products[i].batch[j].quantity;
                        
                        purchaseOrderdelivery.Lines.BatchNumbers.ManufacturerSerialNumber = value.products[i].batch[j].pedimento;
                        purchaseOrderdelivery.Lines.BatchNumbers.InternalSerialNumber = value.products[i].batch[j].attr1;
                        purchaseOrderdelivery.Lines.BatchNumbers.ExpiryDate = value.products[i].batch[j].expirationDate.Date;
                        purchaseOrderdelivery.Lines.BatchNumbers.UserFields.Fields.Item("U_IL_CodBar").Value = value.products[i].batch[j].code;
                        //purchaseOrderdelivery.Lines.BatchNumbers.UserFields.Fields.Item("U_CodBarCj").Value = value.products[i].batch[j].code;
                        purchaseOrderdelivery.Lines.BatchNumbers.Add();

                    }
                    purchaseOrderdelivery.Lines.Add();                    
                }
             
                int result = purchaseOrderdelivery.Add();
                if (result == 0) {
                 
                    return Ok(new { value });

                }
                else {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            return BadRequest("No Existe Documento");
        }

        // // PUT: api/PurchaseDelivery/5
        // [HttpPut("{id}")]
        // public void Put(int id, [FromBody] string value)
        // {
        // }

        // // DELETE: api/ApiWithActions/5
        // [HttpDelete("{id}")]
        // public void Delete(int id)
        // {
        // }
    }
}
