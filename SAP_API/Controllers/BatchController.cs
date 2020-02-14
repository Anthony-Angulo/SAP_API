using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BatchController : ControllerBase
    {
        //// GET: api/Batch
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

        //    SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
        //    oRecSet.DoQuery(@"
        //        Select
        //            ""ItemCode"",
        //            ""BatchNum"",
        //            ""IntrSerial"",
        //            ""Quantity"",
        //            ""U_IL_CodBar""
        //        From OIBT
        //        Where ""ItemCode"" = 'A0305869' AND ""WhsCode"" = 'S01'");
        //    oRecSet.MoveFirst();
        //    JToken batchListDDD = context.XMLTOJSON(oRecSet.GetAsXML());//["OWHS"];
        //    return Ok(batchListDDD);
        //}

        // GET: api/Batch/(Sucursal:S01)/(Codigo:A0305869)
        [HttpGet("{sucursal}/{cod}")]
        public async Task<IActionResult> Get(string sucursal, string cod)
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
                    ""ManBtchNum""
                From OITM
                Where ""ItemCode"" = '" + cod + "'");
            oRecSet.MoveFirst();
            string ManBtnNum = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["ManBtchNum"].ToString();

            if (ManBtnNum == "N")
            {
                oRecSet.DoQuery(@"
                    Select ""OnHand""
                    From OITW
                    Where ""WhsCode"" = '" + sucursal + @"'
                    AND ""ItemCode"" = '" + cod + "'");
                oRecSet.MoveFirst();
                double stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"][0]["OnHand"].ToObject<double>();
                return Ok(new { value = "No Maneja Lote" , stock });
            }

            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""BatchNum"",
                    ""Quantity"",
                    ""U_IL_CodBar"",
                    ""CreateDate""
                From OIBT
                Where ""Quantity"" != 0 AND ""ItemCode"" = '" + cod + @"' AND ""WhsCode"" = '" + sucursal +"'");
            oRecSet.MoveFirst();
            JToken batchList = context.XMLTOJSON(oRecSet.GetAsXML())["OIBT"];
            return Ok(batchList);
        }

    }
}
