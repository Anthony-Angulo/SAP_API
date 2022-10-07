using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BatchController : ControllerBase
    {

        // Note: Documentation No Complete.
        // TODO: Class To Serialize Result.
        // This Route need to be refactorize, there no standard output and existence validation.
        /// <summary>
        /// Search Items Batch.
        /// </summary>
        /// <param name="WhsCode">Warehouse Code. An String that serve as Warehouse identifier.</param>
        /// <param name="ItemCode">ItemCode. An String that serve as Item identifier.</param>
        /// <returns></returns>
        /// <response code="200"></response>
        /// <response code="204">Item Not Found</response>
        // GET: api/Batch/:WhsCode/:ItemCode
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("{WhsCode}/{ItemCode}")]
        public async Task<IActionResult> Get(string WhsCode, string ItemCode)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
                Select ""ManBtchNum""
                From OITM
                Where ""ItemCode"" = '{ItemCode}';");

            if (oRecSet.RecordCount == 0)
            {
                return NoContent();
            }

            string ManBtnNum = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["ManBtchNum"].ToString();

            if (ManBtnNum == "N")
            {

                oRecSet.DoQuery($@"
                    Select ""OnHand""
                    From OITW
                    Where ""WhsCode"" = '{WhsCode}'
                    AND ""ItemCode"" = '{ItemCode}';");

                double stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"][0]["OnHand"].ToObject<double>();
                return Ok(new { value = "No Maneja Lote", stock });
            }

            oRecSet.DoQuery($@"
                Select
                    ""ItemCode"",
                    ""BatchNum"",
                    ""Quantity"",
                    ""U_IL_CodBar"",
                    ""CreateDate""
                From OIBT
                Where ""Quantity"" != 0 AND ""ItemCode"" = '{ItemCode}' AND ""WhsCode"" = '{WhsCode}';");
            oRecSet.MoveFirst();

            JToken batchList = context.XMLTOJSON(oRecSet.GetAsXML())["OIBT"];

            //Force Garbage Collector. Recommendation by InterLatin Dude. SDK Problem with memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(batchList);
        }
    }
}
