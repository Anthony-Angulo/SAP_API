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
    public class DeliveryController : ControllerBase {
        // GET: api/Delivery
        [HttpGet]
        public async Task<IActionResult> Get() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDeliveryNotes);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From ODLN");
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false) {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                //temp["ODLN"] = temp["ODLN"][0];
                list.Add(temp);
                items.Browser.MoveNext();
            }

            return Ok(list);
        }

        // GET: api/Delivery/list/20191022
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

        // POST: api/Delivery
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Delivery value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Documents delivery = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDeliveryNotes);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (order.GetByKey(value.order)) {
                delivery.CardCode = order.CardCode;
                delivery.DocDate = DateTime.Now;
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

                for (int i = 0; i < value.products.Count; i++) {
                    //delivery.Lines.ItemCode = value.products[i].ItemCode;
                    //delivery.Lines.Quantity = value.products[i].Count;
                    //delivery.Lines.UoMEntry = value.products[i].UoMEntry;

                    //delivery.Lines.WarehouseCode = value.products[i].WarehouseCode;
                    delivery.Lines.BaseEntry = order.DocEntry;
                    delivery.Lines.BaseLine = value.products[i].Line;
                    delivery.Lines.BaseType = 17;
                    delivery.Lines.Quantity = value.products[i].Count;
                     
                    for (int j = 0; j < value.products[i].batch.Count; j++) {
                        delivery.Lines.BatchNumbers.BaseLineNumber = delivery.Lines.LineNum;
                        delivery.Lines.BatchNumbers.BatchNumber = value.products[i].batch[j].name;
                        delivery.Lines.BatchNumbers.Quantity = value.products[i].batch[j].quantity;
                        delivery.Lines.BatchNumbers.Add();
                    }
                    
                    delivery.Lines.Add();
                }

                //delivery.Comments = "Test";
                int result = delivery.Add();
                if (result == 0) {
                    return Ok(new { value });
                } else {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }

            }

            return BadRequest(new { error = "No Existe Documento" });
        }

        // POST: api/Delivery/MASS
        [HttpPost("MASS")]
        public async Task<IActionResult> PostMASS([FromBody] DeliveryModelMASS value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

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
