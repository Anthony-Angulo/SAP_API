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
    public class CurrencyRateController : ControllerBase
    {
        // GET: api/CurrencyRate
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

            SAPbobsCOM.SBObob SBO = (SAPbobsCOM.SBObob)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoBridge);
            try {
                SAPbobsCOM.Recordset oRecSet = SBO.GetCurrencyRate("USD", DateTime.Today);
                oRecSet.MoveFirst();
                JToken set = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return Ok(set);
            } catch(Exception ex) {
                return NotFound(ex.Message);
            }
            
        }

    }
}
