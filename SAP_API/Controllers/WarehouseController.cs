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
    public class WarehouseController : ControllerBase {

        public class WarehouseWithSerie {
            public string WhsName { get; set; }
            public string WhsCode { get; set; }
            public int Series { get; set; }
        }

        // Lista de Todas Las Sucursales
        // GET: api/Warehouse
        [HttpGet]
        public async Task<IActionResult> Get() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
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

        // Lista de Sucursales con la serie para generar una orden de venta
        // GET: api/Warehouse/orderlist
        [HttpGet("ListToSell")]
        public async Task<IActionResult> GetListToSell() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 17 AND warehouse.""WhsCode""  in ('S01', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S55') ");
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"];
            List<WarehouseWithSerie> warehouseWithSeries = warehouseList.ToObject<List<WarehouseWithSerie>>();
            oRecSet = null;
            warehouseList = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(warehouseWithSeries);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        // Sucursales y serie del documento orden de venta
        // GET: api/Warehouse/orderlist
        [HttpGet("list")]
        public async Task<IActionResult> GetList() {
            
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
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

        // Sucursales y serie del documento orden de venta, filtradas por vendedor
        // GET: api/Warehouse/list/200
        [HttpGet("list/{id}")]
        public async Task<IActionResult> GetList(int id) {
            
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($"Select \"Fax\" From OSLP Where \"SlpCode\" = {id}");
            oRecSet.MoveFirst();
            string warehouses = context.XMLTOJSON(oRecSet.GetAsXML())["OSLP"][0]["Fax"].ToString();
            warehouses = warehouses.Trim();
            if (warehouses.Equals("")) {
                warehouses = "'S01', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S55'";
            } else {
                warehouses = warehouses.ToUpper();
                warehouses = "'" + warehouses + "'";
                warehouses = warehouses.Replace(" ", "");
                warehouses = warehouses.Replace(",", "','");
            }

            oRecSet.DoQuery(@"
                Select
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 17 AND warehouse.""WhsCode""  in (" + warehouses + ") ");
            oRecSet.MoveFirst();
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"];

            return Ok(warehouseList);
        }

        // GET: api/Warehouse/porderlist
        [HttpGet("purchaseorderlist")]
        public async Task<IActionResult> GetPList() {
            
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
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
        public async Task<IActionResult> GetTSRList() {
            
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            // Release WMS
            //oRecSet.DoQuery(@"
            //    Select
            //        warehouse.""WhsCode"" as ""WhsCodeTSR"",
            //        warehouse2.""WhsCode"" as ""WhsCode"",
            //        warehouse.""WhsName""
            //    From OWHS warehouse
            //    JOIN OWHS warehouse2 ON warehouse.""WhsName"" = warehouse2.""WhsName"" AND warehouse.""WhsCode"" != warehouse2.""WhsCode""
            //    Where warehouse.""WhsCode"" LIKE 'TSR%'");

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

        //// GET: api/Warehouse/5
        //[HttpGet("{id}")]
        //public async Task<IActionResult> Get(int id)
        //{
        //    SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

        //    if (!context.oCompany.Connected) {
        //        int code = context.oCompany.Connect();
        //        if (code != 0) {
        //            string error = context.oCompany.GetLastErrorDescription();
        //            return BadRequest(new { error });
        //        }
        //    }

        //    SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

        //    oRecSet.DoQuery(@"
        //        Select
        //            serie1.""SeriesName"",
        //            serie1.""Series"",
        //            serie1.""ObjectCode"",
        //            serie2.""SeriesName""as s1,
        //            serie2.""Series"" as s2,
        //            serie2.""ObjectCode"" as s3
        //        From NNM1 serie1
        //        JOIN NNM1 serie2 ON serie1.""SeriesName"" = serie2.""SeriesName""");
        //    oRecSet.MoveFirst();
        //    JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["NNM1"];
        //    return Ok(warehouseList);
        //}

    }
}
