using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class PriceListController : ControllerBase {

        // TODO: Class To Serialize Output
        /// <summary>
        /// Get PriceList Information with the SDK Object.
        /// </summary>
        /// <returns>PriceList</returns>
        /// <response code="200">PriceList</response>
        // GET: api/PriceList
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> Get() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.PriceLists items = (SAPbobsCOM.PriceLists)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPriceLists);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From OPLN");
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false) {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                list.Add(temp["OPLN"][0]);
                items.Browser.MoveNext();
            }
            return Ok(list);
        }

        // Class To Serialize Output
        class PriceListOutput {
            public string ListName { get; set; }
            public uint ListNum { get; set; }
        }

        /// <summary>
        /// Get PriceList Name and Num.
        /// </summary>
        /// <returns>PriceList</returns>
        /// <response code="200">PriceList</response>
        // GET: api/PriceList/CRMList
        [ProducesResponseType(typeof(PriceListOutput[]), StatusCodes.Status200OK)]
        [HttpGet("CRMList")]
        public async Task<IActionResult> GetCRMList() {
            
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"ListName\", \"ListNum\" From OPLN");
            oRecSet.MoveFirst();
            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["OPLN"];
            List<PriceListOutput> output = temp.ToObject<List<PriceListOutput>>();
            return Ok(output);
        }

    }
}
