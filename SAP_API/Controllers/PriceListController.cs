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
    public class PriceListController : ControllerBase
    {
        // GET: api/PriceList
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

            SAPbobsCOM.PriceLists items = (SAPbobsCOM.PriceLists)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPriceLists);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From OPLN");
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false)
            {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                list.Add(temp["OPLN"][0]);
                items.Browser.MoveNext();
            }

            return Ok(list);
        }

        // GET: api/PriceList/CRMList
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
            oRecSet.DoQuery("Select \"ListName\", \"ListNum\" From OPLN");
            oRecSet.MoveFirst();
            JToken priceList = context.XMLTOJSON(oRecSet.GetAsXML())["OPLN"];
            return Ok(priceList);
        }

        //// GET: api/PriceList/5
        //[HttpGet("{id}", Name = "Get")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST: api/PriceList
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT: api/PriceList/5
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
