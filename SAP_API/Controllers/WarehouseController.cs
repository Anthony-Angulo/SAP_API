using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using SAP_API.Entities;
using SAP_API.Models;

namespace SAP_API.Controllers {

    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WarehouseController : ControllerBase {

        // Attributes
        private readonly ApplicationDbContext _context;

        //Constructor
        public WarehouseController(ApplicationDbContext context) {
            _context = context;
        }

        //  Summary:
        //    Get Warehouse Data From Database.
        //
        //  Parameters:
        //      None.
        //
        // GET: api/Warehouse/Extern
        [HttpGet("Extern")]
        public IEnumerable<Warehouse> GetExtern() {
            return _context.Warehouses;
        }

        //  Summary:
        //    Get Warehouse Data From Database.
        //
        //  Parameters:
        //      id. An Unsigned Integer that serve as Warehouse identifier.
        //
        // GET: api/Warehouse/:id
        //[Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id) {
            Warehouse warehouse = _context.Warehouses.Find(id);
            if (warehouse != null) {
                return Ok(warehouse);
            }
            return NotFound();
        }

        //  Summary:
        //    Register a Warehouse Extern Database.
        //
        //  Parameters:
        //      WarehouseDto.
        //
        // POST: api/Warehouse
        //[Authorize(Permissions.Warehouses.Create)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] WarehouseDto value) {

            if (value == null) {
                return BadRequest("Datos Invalidos");
            }

            Warehouse warehouse = new Warehouse {
                WhsCode = value.WhsCode,
                WhsName = value.WhsName,
                Active = value.Active,
                ActiveCRM = value.ActiveCRM
            };

            var result = _context.Warehouses.Add(warehouse);

            if (result.State != EntityState.Added) {
                string Errors = "";
                return BadRequest(Errors);
            }

            try {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException) {
                return BadRequest("A database command did not affect the expected number of rows. This usually indicates an optimistic concurrency violation; that is, a row has been changed in the database since it was queried.");
            }
            catch (DbUpdateException) {
                return BadRequest("An error occurred sending updates to the database.");
            }
            //catch (DbEntityValidationException) {
            //    return BadRequest("The save was aborted because validation of entity property values failed.");
            //}
            catch (NotSupportedException) {
                return BadRequest("An attempt was made to use unsupported behavior such as executing multiple asynchronous commands concurrently on the same context instance.");
            }
            catch (ObjectDisposedException) {
                return BadRequest("The context or connection have been disposed.");
            }
            catch (InvalidOperationException) {
                return BadRequest("Some error occurred attempting to process entities in the context either before or after sending commands to the database.");
            }

            return Ok();
        }

        // PUT: api/Warehouse/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] WarehouseDto value) {

            if (value == null) {
                return BadRequest("Datos Invalidos");
            }

            Warehouse warehouse = _context.Warehouses.Find(id);
            if (warehouse == null) {
                return BadRequest("No Exite Esa Sucursal");
            }

            if (warehouse.WhsCode == value.WhsCode && warehouse.WhsName == value.WhsName && warehouse.Active == value.Active && warehouse.ActiveCRM == value.ActiveCRM) {
                string Errors = "No Hay Cambios que Realizar";
                return BadRequest(Errors);
            }

            warehouse.WhsCode = value.WhsCode;
            warehouse.WhsName = value.WhsName;
            warehouse.Active = value.Active;
            warehouse.ActiveCRM = value.ActiveCRM;

            var result = _context.Warehouses.Update(warehouse);

            if (result.State == EntityState.Detached) {
                string Errors = "No Exite Esta Sucursal";
                return BadRequest(Errors);
            }

            if (result.State == EntityState.Unchanged) {
                string Errors = "No Hay Cambios que Realizar";
                return BadRequest(Errors);
            }

            if (result.State != EntityState.Modified) {
                string Errors = "";
                return BadRequest(Errors);
            }

            try {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException) {
                return BadRequest("A database command did not affect the expected number of rows. This usually indicates an optimistic concurrency violation; that is, a row has been changed in the database since it was queried.");
            }
            catch (DbUpdateException) {
                return BadRequest("An error occurred sending updates to the database.");
            }
            //catch (DbEntityValidationException) {
            //    return BadRequest("The save was aborted because validation of entity property values failed.");
            //}
            catch (NotSupportedException) {
                return BadRequest("An attempt was made to use unsupported behavior such as executing multiple asynchronous commands concurrently on the same context instance.");
            }
            catch (ObjectDisposedException) {
                return BadRequest("The context or connection have been disposed.");
            }
            catch (InvalidOperationException) {
                return BadRequest("Some error occurred attempting to process entities in the context either before or after sending commands to the database.");
            }

            return Ok();
        }

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

        // Lista de Sucursales con la serie para generar una orden de venta Mayoreo
        // GET: api/Warehouse/ListToSell
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
                Where serie.""ObjectCode"" = 17 AND warehouse.""WhsCode""  in ('S01', 'S06', 'S07','S17', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S47', 'S55', 'S59', 'S62', 'S63') ");
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"];
            List<WarehouseWithSerie> warehouseWithSeries = warehouseList.ToObject<List<WarehouseWithSerie>>();
            oRecSet = null;
            warehouseList = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(warehouseWithSeries);
        }

        // Lista de Sucursales con la serie para generar una orden de venta Menudeo
        // GET: api/Warehouse/ListToSellRetail
        [HttpGet("ListToSellRetail")]
        public async Task<IActionResult> GetListToSellRetail() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 17"); // AND warehouse.""WhsCode""  in ('S01', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S55') ");
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"];
            List<WarehouseWithSerie> warehouseWithSeries = warehouseList.ToObject<List<WarehouseWithSerie>>();
            oRecSet = null;
            warehouseList = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(warehouseWithSeries);
        }

        // Lista de Todas Las Sucursales para Toma de Inventario
        // GET: api/Warehouse/ToInventory
        [HttpGet("ToInventory")]
        public async Task<IActionResult> GetWarehouseToInventory() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ""WhsCode"",
                    ""WhsName"" 
                From OWHS
                Where ""WhsCode"" LIKE 'S%'
                AND LENGTH(""WhsCode"") = 3 ");
            oRecSet.MoveFirst();
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"];
            return Ok(warehouseList);
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
                Where serie.""ObjectCode"" = 17 AND warehouse.""WhsCode""  in ('S01','S17', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36','S47', 'S55', 'S59', 'S62','S63') ");
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
                warehouses = "'S01', 'S06','S17' 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S47', 'S55', 'S59', 'S62','S63'";
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
