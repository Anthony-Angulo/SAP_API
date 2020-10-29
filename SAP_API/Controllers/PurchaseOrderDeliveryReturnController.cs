using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrderDeliveryReturnController : ControllerBase {

        //// GET: api/PurchaseOrderDeliveryReturn/5
        //[HttpGet("{id}")]
        //public string Get(int id) {
        //    return "value";
        //}

        // POST: api/PurchaseOrderDeliveryReturn
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] PurchaseOrderDeliveryReturn value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents returnDocument = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseReturns);
            SAPbobsCOM.Documents purchaseDelivery = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseDeliveryNotes);

            if (purchaseDelivery.GetByKey(value.order)) {

                returnDocument.CardCode = purchaseDelivery.CardCode;
                returnDocument.DocDate = DateTime.Now;
                returnDocument.DocDueDate = DateTime.Now;

                for (int i = 0; i < value.products.Count; i++) {

                    returnDocument.Lines.BaseEntry = purchaseDelivery.DocEntry;
                    returnDocument.Lines.BaseLine = value.products[i].Line;
                    returnDocument.Lines.BaseType = 20;
                    returnDocument.Lines.Quantity = value.products[i].Count;

                    for (int j = 0; j < value.products[i].batch.Count; j++) {
                        returnDocument.Lines.BatchNumbers.BaseLineNumber = returnDocument.Lines.LineNum;
                        returnDocument.Lines.BatchNumbers.BatchNumber = value.products[i].batch[j].name;
                        returnDocument.Lines.BatchNumbers.Quantity = value.products[i].batch[j].quantity;
                        returnDocument.Lines.BatchNumbers.Add();
                    }

                    returnDocument.Lines.Add();
                }

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
