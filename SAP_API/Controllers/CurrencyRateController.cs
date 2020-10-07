using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class CurrencyRateController : ControllerBase {

        // Class To Serialize CurrencyRate Query Result 
        public class CurrencyRateDetail {
            public double CurrencyRate;
        }

        //  Summary:
        //    Get CurrencyRate From the Current Date.
        //
        //  Parameters:
        //      None.
        //
        // GET: api/CurrencyRate
        [HttpGet]
        public async Task<IActionResult> Get() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.SBObob SBO = (SAPbobsCOM.SBObob)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoBridge);
            try {
                SAPbobsCOM.Recordset oRecSet = SBO.GetCurrencyRate("USD", DateTime.Today);
                JToken currency = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
                CurrencyRateDetail currencyRateDetail = currency.ToObject<CurrencyRateDetail>();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return Ok(currencyRateDetail);
            } catch(Exception ex) {
                return Conflict(ex.Message);
            }
        }

    }
}
