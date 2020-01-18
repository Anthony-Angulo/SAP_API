using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;
using Microsoft.Extensions.Configuration;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        // GET: api/Warehouse
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

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""WhsCode"",
                    ""WhsName""
                From OWHS");
            oRecSet.MoveFirst();
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"];
            return Ok(warehouseList);
        }

        // GET: api/Warehouse/orderlist
        [HttpGet("list")]
        public async Task<IActionResult> GetList()
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
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 17 AND warehouse.""WhsCode""  in ('S01', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S55') ");
            oRecSet.MoveFirst();
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"];
            return Ok(warehouseList);
        }

        // GET: api/Warehouse/porderlist
        [HttpGet("porderlist")]
        public async Task<IActionResult> GetPList()
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
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 22");
            oRecSet.MoveFirst();
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"];
            return Ok(warehouseList);
        }

        // GET: api/Warehouse/TSR
        [HttpGet("tsr")]
        public async Task<IActionResult> GetTSRList()
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
                    warehouse.""WhsCode"" as WhsTSRCode,
                    warehouse2.""WhsCode"" as WhsCode,
                    warehouse.""WhsName""
                From OWHS warehouse
                JOIN OWHS warehouse2 ON warehouse.""WhsName"" = warehouse2.""WhsName"" AND warehouse.""WhsCode"" != warehouse2.""WhsCode""
                Where warehouse.""WhsCode"" LIKE 'TSR%'");
            oRecSet.MoveFirst();
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"];
            return Ok(warehouseList);
        }

        // GET: api/Warehouse/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
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
                    serie1.""SeriesName"",
                    serie1.""Series"",
                    serie1.""ObjectCode"",
                    serie2.""SeriesName""as s1,
                    serie2.""Series"" as s2,
                    serie2.""ObjectCode"" as s3
                From NNM1 serie1
                JOIN NNM1 serie2 ON serie1.""SeriesName"" = serie2.""SeriesName""");
            oRecSet.MoveFirst();
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["NNM1"];
            return Ok(warehouseList);
        }

        //// POST: api/Warehouse
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT: api/Warehouse/5
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
