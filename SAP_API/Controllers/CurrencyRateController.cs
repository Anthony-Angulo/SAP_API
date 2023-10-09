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
    public class CurrencyRateController : ControllerBase
    {

        // Class To Serialize CurrencyRate Query Result 
        public class CurrencyRateDetail
        {
            public double CurrencyRate;
        }

        /// <summary>
        /// Get CurrencyRate From the Current Date.
        /// </summary>
        /// <returns>Currenct CurrencyRate</returns>
        /// <response code="200">Returns CurrencyRate</response>
        /// <response code="409">Error</response>
        // GET: api/CurrencyRate
        [ProducesResponseType(typeof(CurrencyRateDetail), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status204NoContent)]
        [HttpGet]
        public async Task<IActionResult> Get()
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.SBObob SBO = (SAPbobsCOM.SBObob)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoBridge);
            try
            {
                SAPbobsCOM.Recordset oRecSet = SBO.GetCurrencyRate("USD", DateTime.Today);
                JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
                CurrencyRateDetail CurrencyOutput = temp.ToObject<CurrencyRateDetail>();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return Ok(CurrencyOutput);
            }
            catch (Exception ex)
            {
                return Conflict(ex.Message);
            }
        }

    }
}
