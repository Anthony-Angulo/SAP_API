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
    public class ReportController : ControllerBase {

        // GET: api/Report/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id) {
            return "value";
        }

        // GET: api/Report/5
        [HttpGet("WarehouseGroupDate/{warehouseCode}/{group}/{fromDate}/{toDate}")]
        public async Task<IActionResult> GetByWarehouseGroupDate(string warehouseCode, int group, string fromDate, string toDate) {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                (Select
                    ""ItemCode"",
                    ""Dscription"",
                    SUM(""Quantity"") as Quantity,
                    ""UomCode"",
                    SUM(""LineTotal"") as LineTotal,
                    ""Currency""
                From INV1
                Where ""Currency"" = 'MXN'
                AND ""ItemCode"" in (
                    Select ""ItemCode""
                    From OITM Where ""QryGroup" + group + @""" = 'Y')
                AND ""DocEntry"" in (
                    Select ""DocEntry""
                    From OINV
                    Where ""DocStatus"" = 'C'
                    AND ""CANCELED"" = 'N'
                    AND ""DocDate"" >= to_timestamp('" + fromDate + @"', 'yyyy-mm-dd')
                    AND ""DocDate"" <= to_timestamp('" + toDate + @"', 'yyyy-mm-dd')
                    AND ""Series"" = (Select ""Series"" From NNM1 Where ""ObjectCode"" = 13 AND ""SeriesName"" = '" + warehouseCode + @"'))
                GROUP BY ""ItemCode"", ""Dscription"", ""UomCode"", ""Currency"")
                UNION (
                Select
                    ""ItemCode"",
                    ""Dscription"",
                    SUM(""Quantity"") as Quantity,
                    ""UomCode"",
                    SUM(""TotalFrgn"") as LineTotal,
                    ""Currency""
                From INV1
                Where ""Currency"" = 'USD'
                AND ""ItemCode"" in (
                    Select ""ItemCode""
                    From OITM Where ""QryGroup" + group + @""" = 'Y')
                AND ""DocEntry"" in (
                    Select ""DocEntry""
                    From OINV
                    Where ""DocStatus"" = 'C'
                    AND ""CANCELED"" = 'N'
                    AND ""DocDate"" >= to_timestamp('" + fromDate + @"', 'yyyy-mm-dd')
                    AND ""DocDate"" <= to_timestamp('" + toDate + @"', 'yyyy-mm-dd')
                    AND ""Series"" = (Select ""Series"" From NNM1 Where ""ObjectCode"" = 13 AND ""SeriesName"" = '" + warehouseCode + @"'))
                GROUP BY ""ItemCode"", ""Dscription"", ""UomCode"", ""Currency"");");

            oRecSet.MoveFirst();
            JToken result = context.XMLTOJSON(oRecSet.GetAsXML())["INV1"];
            return Ok(result);
        }

    }
}
