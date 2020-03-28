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
    public class PaymentMethodsController : ControllerBase
    {
        // GET: api/PaymentMethods
        [HttpGet]
        public async Task<IActionResult> Get() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.WizardPaymentMethods payment = (SAPbobsCOM.WizardPaymentMethods)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oWizardPaymentMethods);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From OPYM");
            payment.Browser.Recordset = oRecSet;
            payment.Browser.MoveFirst();

            while (payment.Browser.EoF == false) {
                JToken temp = context.XMLTOJSON(payment.GetAsXML());
                list.Add(temp);
                payment.Browser.MoveNext();
            }
            return Ok(list);
        }

        //// GET: api/PaymentMethods/5
        //[HttpGet("{id}")]
        //public async Task<IActionResult> Get(int id) {

        //    SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
        //    SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
        //    oRecSet.DoQuery("Select TOP 10 * From CRD2");
        //    JToken temp = context.XMLTOJSON(oRecSet.GetAsXML());
        //    return Ok(temp);
        //}

        //// POST: api/PaymentMethods
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT: api/PaymentMethods/5
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
