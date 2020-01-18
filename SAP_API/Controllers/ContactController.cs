using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {

        //// GET: api/Contact
        //[HttpGet]
        //public async Task<IActionResult> Get()
        //{
        //    SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

        //    if (!context.oCompany.Connected)
        //    {
        //        int code = context.oCompany.Connect();
        //        if (code != 0)
        //        {
        //            string error = context.oCompany.GetLastErrorDescription();
        //            return BadRequest(new { error });
        //        }
        //    }

        //    SAPbobsCOM.BusinessPartners items = context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);
        //    SAPbobsCOM.Recordset oRecSet = context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

        //    List<Object> list = new List<Object>();

        //    oRecSet.DoQuery("Select TOP 1 * From OCRD");
        //    items.Browser.Recordset = oRecSet;
        //    items.Browser.MoveFirst();

        //    while (items.Browser.EoF == false)
        //    {
        //        JToken temp = context.XMLTOJSON(items.GetAsXML());
        //        list.Add(temp);
        //        items.Browser.MoveNext();
        //    }

        //    return Ok(list);
        //}

        // GET: api/Contact/CRM/5
        [HttpGet("CRM/{id}")]
        public async Task<IActionResult> GetCRMID(string id)
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

            SAPbobsCOM.BusinessPartners items = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);
            SAPbobsCOM.SalesPersons seller = (SAPbobsCOM.SalesPersons)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oSalesPersons);
            SAPbobsCOM.PaymentTermsTypes payment = (SAPbobsCOM.PaymentTermsTypes)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPaymentTermsTypes);
            SAPbobsCOM.UserTable sboTable = (SAPbobsCOM.UserTable)context.oCompany.UserTables.Item("SO1_01FORMAPAGO");

            JToken pagos = context.XMLTOJSON(sboTable.GetAsXML())["OCRD"];

            if (items.GetByKey(id))
            {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["OCRD"] = temp["OCRD"][0];

                if (seller.GetByKey(temp["OCRD"]["SlpCode"].ToObject<int>()))
                {
                    JToken temp2 = context.XMLTOJSON(seller.GetAsXML());
                    temp["OSLP"] = temp2["OSLP"][0];
                }
                if (payment.GetByKey(temp["OCRD"]["GroupNum"].ToObject<int>()))
                {
                    JToken temp3 = context.XMLTOJSON(payment.GetAsXML());
                    temp["OCTG"] = temp3["OCTG"][0];
                }

                temp["PAGO"] = pagos;
                return Ok(temp);
            }
            return NotFound("No Existe Contacto");
        }

        // GET: api/Contact/CRMList
        [HttpGet("CRMList")]
        public async Task<IActionResult> GetCRMList()
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

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"CardCode\", \"CardName\", \"CardFName\"  From OCRD Where \"CardType\" = 'C' AND \"CardCode\" LIKE '%-P'");
            oRecSet.MoveFirst();
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //return Ok(context.comp(contacts, 0).Result);
            return Ok(contacts);
        }

        // GET: api/Contact/CRMListProveedor
        [HttpGet("CRMListProveedor")]
        public async Task<IActionResult> GetCRMListProveedor()
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

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"CardCode\", \"CardName\", \"Currency\", \"CardFName\"  From OCRD Where \"CardType\" = 'S'");
            oRecSet.MoveFirst();
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(contacts);
        }

        // GET: api/Contact/APPCRM
        [HttpGet("APPCRM")]
        public async Task<IActionResult> GetAPPCRM()
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

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    ""Address"",
                    ""ZipCode"",
                    ""Country"",
                    ""Block"",
                    ""GroupNum""
                From OCRD Where ""CardType"" = 'C' AND ""CardCode"" LIKE '%-P'");
            oRecSet.MoveFirst();
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //return Ok(await context.comp(contacts, 3));
            return Ok(contacts);
        }

        //// POST: api/Contact
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT: api/Contact/5
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
