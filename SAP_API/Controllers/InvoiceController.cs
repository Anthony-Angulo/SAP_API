using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        // GET: api/Invoice
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Invoice/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Invoice
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // POST: api/Invoice
        [HttpPost("Test/{DocEntry}")]
        public async Task<IActionResult> PostTEST(string DocEntry)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents Doc = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                SELECT
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    contact.""CardName"",
                    contact.""CardCode"",
                    contact.""CardFName"",
                    warehouse.""WhsCode"",
                    warehouse.""WhsName""
                FROM ORDR ord
                LEFT JOIN NNM1 series ON series.""Series"" = ord.""Series""
                LEFT JOIN OWHS warehouse ON warehouse.""WhsCode"" = series.""SeriesName""
                LEFT JOIN OSLP employee ON employee.""SlpCode"" = ord.""SlpCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OCRD contact ON contact.""CardCode"" = ord.""CardCode""
                WHERE ord.""DocEntry"" = '" + DocEntry + "' ");

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0];

            oRecSet.DoQuery(@"
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
                From RDR1 Where ""DocEntry"" = '" + DocEntry + "'");
            temp["RDR1"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];


            oRecSet.DoQuery(@"
                SELECT
                    ord.""DocEntry"",
                    ord.""DocNum"",

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
                WHERE ord.""DocEntry"" in (Select Distinct ""DocEntry"" From DLN1 Where ""BaseEntry"" = '" + DocEntry + "' )");

            temp["ODLN"] = context.XMLTOJSON(oRecSet.GetAsXML())["ODLN"];

            oRecSet.DoQuery(@"
                Select
                    ""LineNum"",
                    ""DocEntry"",
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
                From DLN1 Where ""BaseEntry"" = '" + DocEntry + "'");
            temp["DLN1"] = context.XMLTOJSON(oRecSet.GetAsXML())["DLN1"];


            Doc.CardCode = temp["CardCode"].ToString();
            //delivery.DocDate = DateTime.Now;
            //delivery.DocDueDate = DateTime.Now;

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
            //Where serie1.""ObjectCode"" = 15 AND serie2.""Series"" = '" + order.Series + "'");
            //oRecSet.MoveFirst();
            //delivery.Series = context.XMLTOJSON(oRecSet.GetAsXML())["NNM1"][0]["Series"].ToObject<int>();
            foreach (JToken t in temp["DLN1"])
            {
                //delivery.Lines.ItemCode = value.products[i].ItemCode;
                //delivery.Lines.Quantity = value.products[i].Count;
                //delivery.Lines.UoMEntry = value.products[i].UoMEntry;

                //delivery.Lines.WarehouseCode = value.products[i].WarehouseCode;
                Doc.Lines.BaseEntry = t["DocEntry"].ToObject<int>();
                Doc.Lines.BaseLine = t["LineNum"].ToObject<int>();
                Doc.Lines.BaseType = 15;
                //delivery.Lines.Quantity = value.products[i].Count;

                //for (int j = 0; j < value.products[i].batch.Count; j++)
                //{
                //    delivery.Lines.BatchNumbers.BaseLineNumber = delivery.Lines.LineNum;
                //    delivery.Lines.BatchNumbers.BatchNumber = value.products[i].batch[j].name;
                //    delivery.Lines.BatchNumbers.Quantity = value.products[i].batch[j].quantity;
                //    delivery.Lines.BatchNumbers.Add();
                //}

                Doc.Lines.Add();
            }

            //delivery.Comments = "Test";
            int result = Doc.Add();
            if (result == 0)
            {
                return Ok();
            }
            else
            {
                string error = context.oCompany.GetLastErrorDescription();
                return BadRequest(new { error });
            }

            return Ok(temp);
            return BadRequest(new { error = "No Existe Documento" });
        }





        // PUT: api/Invoice/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
