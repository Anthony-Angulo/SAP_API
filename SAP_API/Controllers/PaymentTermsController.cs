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
    public class PaymentTermsController : ControllerBase {
        // GET: api/PaymentTerms
        [HttpGet]
        public async Task<IActionResult> Get() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.PaymentTermsTypes payment = (SAPbobsCOM.PaymentTermsTypes)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPaymentTermsTypes);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From OCTG");
            payment.Browser.Recordset = oRecSet;
            payment.Browser.MoveFirst();

            while (payment.Browser.EoF == false) {
                JToken temp = context.XMLTOJSON(payment.GetAsXML());
                temp = temp["OCTG"][0];
                list.Add(temp);
                payment.Browser.MoveNext();
            }
            return Ok(list);
        }

        // GET: api/PaymentTerms/CRMList
        [HttpGet("CRMList")]
        public async Task<IActionResult> GetCRMList() {
            
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"GroupNum\", \"PymntGroup\" From OCTG");
            oRecSet.MoveFirst();
            JToken paymentTermsList = context.XMLTOJSON(oRecSet.GetAsXML())["OCTG"];
            return Ok(paymentTermsList);
        }

        //// GET: api/PaymentTerms/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST: api/PaymentTerms
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT: api/PaymentTerms/5
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
