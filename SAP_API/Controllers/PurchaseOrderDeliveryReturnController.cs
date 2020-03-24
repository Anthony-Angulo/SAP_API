using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrderDeliveryReturnController : ControllerBase {
        // GET: api/PurchaseOrderDeliveryReturn/5
        [HttpGet("{id}")]
        public string Get(int id) {
            return "value";
        }

        // POST: api/PurchaseOrderDeliveryReturn
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PurchaseOrderDeliveryReturn value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents returnDocument = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseReturns);
            SAPbobsCOM.Documents purchaseDelivery = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseDeliveryNotes);

            //SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (purchaseDelivery.GetByKey(value.order)) {

                returnDocument.CardCode = purchaseDelivery.CardCode;
                returnDocument.DocDate = DateTime.Now;
                returnDocument.DocDueDate = DateTime.Now;

                //oRecSet.DoQuery(@"
                //Select
                //    serie1.""SeriesName"",
                //    serie1.""Series"",
                //    serie1.""ObjectCode"",
                //    serie2.""SeriesName""as s1,
                //    serie2.""Series"" as s2,
                //    serie2.""ObjectCode"" as s3
                //From NNM1 serie1
                //JOIN NNM1 serie2 ON serie1.""SeriesName"" = serie2.""SeriesName""
                //Where serie1.""ObjectCode"" = 15 AND serie2.""Series"" = '" + purchaseDelivery.Series + "'");
                //oRecSet.MoveFirst();
                //returnDocument.Series = context.XMLTOJSON(oRecSet.GetAsXML())["NNM1"][0]["Series"].ToObject<int>();

                for (int i = 0; i < value.products.Count; i++) {
                    //returnDocument.Lines.ItemCode = value.products[i].ItemCode;
                    //returnDocument.Lines.Quantity = value.products[i].Count;
                    //returnDocument.Lines.UoMEntry = value.products[i].UoMEntry;

                    //returnDocument.Lines.WarehouseCode = value.products[i].WarehouseCode;
                    returnDocument.Lines.BaseEntry = purchaseDelivery.DocEntry;
                    returnDocument.Lines.BaseLine = value.products[i].Line;
                    //returnDocument.Lines.BaseType = 17; SAPbobsCOM.enum
                    returnDocument.Lines.Quantity = value.products[i].Count;

                    for (int j = 0; j < value.products[i].batch.Count; j++) {
                        returnDocument.Lines.BatchNumbers.BaseLineNumber = returnDocument.Lines.LineNum;
                        returnDocument.Lines.BatchNumbers.BatchNumber = value.products[i].batch[j].name;
                        returnDocument.Lines.BatchNumbers.Quantity = value.products[i].batch[j].quantity;
                        returnDocument.Lines.BatchNumbers.Add();
                    }

                    returnDocument.Lines.Add();
                }

                //returnDocument.Comments = "Test";
                int result = returnDocument.Add();
                if (result == 0) {
                    return Ok(new { value });
                } else {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }

            }

            return BadRequest(new { error = "No Existe Documento" });
        }

    }
}
