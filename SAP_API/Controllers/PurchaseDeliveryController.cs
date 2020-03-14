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
    public class PurchaseDeliveryController : ControllerBase {
        // // GET: api/PurchaseDelivery
        // [HttpGet]
        // public async Task<IActionResult> Get()
        // {
        //     SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

        //     if (!context.oCompany.Connected) {
        //         int code = context.oCompany.Connect();
        //         if (code != 0) {
        //             string error = context.oCompany.GetLastErrorDescription();
        //             return BadRequest(new { error });
        //         }
        //     }

        //     SAPbobsCOM.Documents items = context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseDeliveryNotes);
        //     SAPbobsCOM.Recordset oRecSet = context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
        //     List<Object> list = new List<Object>();

        //     oRecSet.DoQuery("Select * From OPDN");
        //     items.Browser.Recordset = oRecSet;
        //     items.Browser.MoveFirst();

        //     while (items.Browser.EoF == false) {
        //         JToken temp = context.XMLTOJSON(items.GetAsXML());
        //         temp["OPDN"] = temp["OPDN"][0];
        //         list.Add(temp);
        //         items.Browser.MoveNext();
        //     }
        //     return Ok(list);
        // }

        // GET: api/PurchaseDelivery/list/20191022
        [HttpGet("list/{date}")]
        public async Task<IActionResult> GetList(string date) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

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

        // POST: api/PurchaseDelivery
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PurchaseOrderDelivery value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Documents purchaseOrder = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);
            SAPbobsCOM.Documents purchaseOrderdelivery = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseDeliveryNotes);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (purchaseOrder.GetByKey(value.order)) {
                
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

                    if (value.products[i].UoMEntry == 7) {
                        purchaseOrder.Lines.SetCurrentLine(value.products[i].Line);
                        purchaseOrderdelivery.Lines.UoMEntry = 6;
                        purchaseOrderdelivery.Lines.UnitPrice = purchaseOrder.Lines.UnitPrice * 2.20462;
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
