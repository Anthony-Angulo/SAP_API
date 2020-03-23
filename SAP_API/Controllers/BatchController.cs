using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BatchController : ControllerBase {

        // GET: api/Batch/(warehouse:S01)/(itemcode:A0305869)
        [HttpGet("{warehouse}/{itemcode}")]
        public async Task<IActionResult> Get(string warehouse, string itemcode) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select ""ManBtchNum""
                From OITM
                Where ""ItemCode"" = '" + itemcode + "'");
            oRecSet.MoveFirst();
            string ManBtnNum = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["ManBtchNum"].ToString();

            if (ManBtnNum == "N") {
                oRecSet.DoQuery(@"
                    Select ""OnHand""
                    From OITW
                    Where ""WhsCode"" = '" + warehouse + @"'
                    AND ""ItemCode"" = '" + itemcode + "'");
                oRecSet.MoveFirst();
                double stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"][0]["OnHand"].ToObject<double>();
                return Ok(new { value = "No Maneja Lote" , stock });
            }

            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""BatchNum"",
                    ""Quantity"",
                    ""U_IL_CodBar"",
                    ""CreateDate""
                From OIBT
                Where ""Quantity"" != 0 AND ""ItemCode"" = '" + itemcode + @"' AND ""WhsCode"" = '" + warehouse +"'");
            oRecSet.MoveFirst();
            JToken batchList = context.XMLTOJSON(oRecSet.GetAsXML())["OIBT"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(batchList);
        }
    }
}
