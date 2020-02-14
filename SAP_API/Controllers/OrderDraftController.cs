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
    public class OrderDraftController : ControllerBase
    {
        // GET: api/OrderDraft
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (!context.oCompany.Connected)
            {
                int code = context.oCompany.Connect();
                if (code != 0)
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"Select * FROM OWDD aut JOIN ODRF draft ON aut.""DraftEntry"" = draft.""DocEntry"" WHERE aut.""ObjType"" = 17 AND aut.""OwnerID"" = '10' AND aut.""IsDraft"" = 'Y'"); //AND ""Status"" = 'Y' 
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["OWDD"];
            return Ok(orders);

        }

        // GET: api/OrderDraft/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (!context.oCompany.Connected)
            {
                int code = context.oCompany.Connect();
                if (code != 0)
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDrafts);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (items.GetByKey(id))
            {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["ODRF"] = temp["ODRF"][0];

                oRecSet.DoQuery(@"
                Select
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""Series"" = '" + temp["ODRF"]["Series"] + "'");

                oRecSet.MoveFirst();
                JToken series = context.XMLTOJSON(oRecSet.GetAsXML());
                temp["WHS"] = series["OWHS"][0];

                oRecSet.DoQuery("Select * From OSLP Where \"SlpCode\" = '" + temp["ODRF"]["SlpCode"] + "'");
                oRecSet.MoveFirst();
                JToken vendedor = context.XMLTOJSON(oRecSet.GetAsXML());
                temp["SELLER"] = vendedor["OSLP"][0];


                oRecSet.DoQuery("Select \"CardCode\", \"CardName\", \"Currency\", \"CardFName\"  From OCRD Where \"CardCode\" = '" + temp["ODRF"]["CardCode"] + "'");
                oRecSet.MoveFirst();
                temp["contact"] = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0];

                return Ok(temp);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return NotFound("No Existe Documento");
        }

        // POST: api/OrderDraft
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (!context.oCompany.Connected)
            {
                int code = context.oCompany.Connect();
                if (code != 0)
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDrafts);
            int result;
            List<string> errors = new List<string>();

            oRecSet.DoQuery(@"Select * FROM OWDD WHERE ""ObjType"" = 17 AND ""Status"" = 'Y' AND ""OwnerID"" = '10' AND ""IsDraft"" = 'Y'");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML());
            foreach (JToken draft in orders["OWDD"])
            {
                items.GetByKey(draft["DraftEntry"].ToObject<int>());
                result = items.SaveDraftToDocument();
                if (result != 0)
                {
                    errors.Add(context.oCompany.GetLastErrorDescription());
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (errors.Count > 0)
            {
                return BadRequest(errors);
            }
            return Ok();
        }

        //// PUT: api/OrderDraft/5
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
