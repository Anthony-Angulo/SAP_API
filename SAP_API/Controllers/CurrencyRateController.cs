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
            SAPbobsCOM.Recordset oRecSet = SBO.GetCurrencyRate("USD", DateTime.Today);
            oRecSet.MoveFirst();
            JToken set = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(set);
        }

        //// GET: api/CurrencyRate/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST: api/CurrencyRate
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT: api/CurrencyRate/5
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
