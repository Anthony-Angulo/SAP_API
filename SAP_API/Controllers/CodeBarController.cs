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
    public class CodeBarController : ControllerBase
    {
        //// GET: api/CodeBar
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        // GET: api/CodeBar/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
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
                        ""BcdEntry"",
                        ""BcdCode"",
                        ""BcdName"",
                        ""ItemCode"",
                        ""UomEntry""
                    From OBCD Where ""BcdCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            if (oRecSet.RecordCount == 0)
            {
                return NotFound();
            }
            string itemcode = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"][0]["ItemCode"].ToString();

            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""ItemName"",
                    ""QryGroup7"",
                    ""QryGroup41"",
                    ""ManBtchNum"",
                    ""U_IL_PesMax"",
                    ""U_IL_PesMin"",
                    ""U_IL_PesProm"",
                    ""U_IL_TipPes"",
                    ""NumInSale"",
                    ""NumInBuy""
                From OITM Where ""ItemCode"" = '" + itemcode + "'");
            oRecSet.MoveFirst();
            JToken Detail = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            oRecSet.DoQuery(@"
                Select
                    ""BcdEntry"",
                    ""BcdCode"",
                    ""BcdName"",
                    ""ItemCode"",
                    ""UomEntry""
                From OBCD Where ""ItemCode"" = '" + itemcode + "'");
            oRecSet.MoveFirst();
            JToken CodeBars = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];

            return Ok(new { Detail, CodeBars });
        }

        // POST: api/CodeBar
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Codebar value)
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
            SAPbobsCOM.CompanyService services = context.oCompany.GetCompanyService();

            SAPbobsCOM.BarCodesService barCodesService = (SAPbobsCOM.BarCodesService)services.GetBusinessService(SAPbobsCOM.ServiceTypes.BarCodesService);
            SAPbobsCOM.BarCode barCode = (SAPbobsCOM.BarCode)barCodesService.GetDataInterface(SAPbobsCOM.BarCodesServiceDataInterfaces.bsBarCode);
            barCode.ItemNo = value.ItemCode;
            barCode.BarCode = value.Barcode;
            barCode.UoMEntry = value.UOMEntry;
            try
            {
                SAPbobsCOM.BarCodeParams result = barCodesService.Add(barCode);
                return Ok(result.AbsEntry);
            } catch (Exception x)
            {
                return BadRequest(x.Message);
            }
            
            
        }

        //// PUT: api/CodeBar/5
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
