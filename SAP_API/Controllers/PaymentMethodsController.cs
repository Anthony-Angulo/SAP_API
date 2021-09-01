using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentMethodsController : ControllerBase {
    
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
    }
}
