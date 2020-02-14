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
    public class PeyMethodController : ControllerBase
    {
        // GET: api/PeyMethod
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
            SAPbobsCOM.WizardPaymentMethods payment = (SAPbobsCOM.WizardPaymentMethods)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oWizardPaymentMethods);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From OPYM");
            payment.Browser.Recordset = oRecSet;
            payment.Browser.MoveFirst();

            while (payment.Browser.EoF == false)
            {
                JToken temp = context.XMLTOJSON(payment.GetAsXML());
                //temp["ORDR"] = temp["ORDR"][0];
                list.Add(temp);
                payment.Browser.MoveNext();
            }
            return Ok(list);
        }

        //// GET: api/PeyMethod/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST: api/PeyMethod
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT: api/PeyMethod/5
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
