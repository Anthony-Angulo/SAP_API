﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Entities;
using SAP_API.Models;

namespace SAP_API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {


        private readonly LogsContext _contextLogs;
        private readonly ApplicationDbContext _context;

        public OrderController(LogsContext contextLogs, ApplicationDbContext context)
        {
            _contextLogs = contextLogs;
            _context = context;

        }
        /// <summary>
        /// Get Order List to CRM web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>OrderSearchResponse</returns>
        /// <response code="200">OrderSearchResponse(SearchResponse)</response>
        // POST: api/Order/Search
        [ProducesResponseType(typeof(OrderSearchResponse), StatusCodes.Status200OK)]
        [HttpPost("Search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();
            where.Add(@"ord.""U_SO1_01RETAILONE"" = 'N'");

            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(ord.\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(employee.\"SlpName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty)
            {
                where.Add($"LOWER(contact.\"CardFName\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty)
            {
                where.Add($"LOWER(contact.\"CardName\") Like LOWER('%{request.columns[3].search.value}%')");
            }
            if (request.columns[4].search.value != String.Empty)
            {
                where.Add($"LOWER(warehouse.\"WhsName\") Like LOWER('%{request.columns[4].search.value}%')");
            }
            if (request.columns[5].search.value != String.Empty)
            {
                where.Add($"ord.\"DocTotal\" = {request.columns[5].search.value}");
            }
            if (request.columns[6].search.value != String.Empty)
            {
                where.Add($"LOWER(ord.\"DocCur\") Like LOWER('%{request.columns[6].search.value}%')");
            }
            if (request.columns[7].search.value != String.Empty)
            {
                where.Add($"LOWER(payment.\"PymntGroup\") Like LOWER('%{request.columns[7].search.value}%')");
            }
            if (request.columns[8].search.value != String.Empty)
            {

                List<string> whereOR = new List<string>();
                if ("Abierto".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"ord.""DocStatus"" = 'O' ");
                }
                if ("Cerrado".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"ord.""DocStatus"" = 'C' ");
                }
                if ("Cancelado".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"ord.""CANCELED"" = 'Y' ");
                }

                string whereORClause = "(" + String.Join(" OR ", whereOR) + ")";
                where.Add(whereORClause);
            }
            if (request.columns[9].search.value != String.Empty)
            {
                where.Add($"to_char(to_date(SUBSTRING(ord.\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[9].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY ord.\"DocNum\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY employee.\"SlpName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 2)
            {
                orderby = $" ORDER BY contact.\"CardFName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 3)
            {
                orderby = $" ORDER BY contact.\"CardName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 4)
            {
                orderby = $" ORDER BY warehouse.\"WhsName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 5)
            {
                orderby = $" ORDER BY ord.\"DocTotal\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 6)
            {
                orderby = $" ORDER BY ord.\"DocCur\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 7)
            {
                orderby = $" ORDER BY payment.\"PymntGroup\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 8)
            {
                orderby = $" ORDER BY ord.\"DocStatus\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 9)
            {
                orderby = $" ORDER BY ord.\"DocDate\" {request.order[0].dir},ord.\"DocTime\" {request.order[0].dir}";
            }
            else
            {
                orderby = $" ORDER BY ord.\"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",

                    concat(to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY'),' '|| SUBSTRING(ord.""DocTime"" , 0, LENGTH(ord.""DocTime"")-2) || ':' || RIGHT(ord.""DocTime"",2)) as ""DocDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",

                    (case when ord.""DocCur"" = 'USD' then ord.""DocTotalFC""
                    else ord.""DocTotal"" end)  AS  ""DocTotal"",
                    ord.""CardName"",
                    ord.""DocCur"",
                    payment.""PymntGroup"",
                    contact.""CardFName"",
                    employee.""SlpName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OSLP employee ON ord.""SlpCode"" = employee.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0)
            {
                query += "Where " + whereClause;
            }

            query += orderby;

            if (request.length != -1)
            {
                query += " LIMIT " + request.length + " OFFSET " + request.start + "";
            }

            oRecSet.DoQuery(query);
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"].ToObject<List<OrderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OSLP employee ON ord.""SlpCode"" = employee.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0)
            {
                queryCount += "Where " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0]["COUNT"].ToObject<int>();

            var respose = new OrderSearchResponse
            {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            return Ok(respose);
        }

        // TODO: this route is temporary.
        // After the database integration, the user identification token must 
        // be used to know whether to use filtering by Warehouse or not. 
        // This has to be done in route "Search"
        /// <summary>
        /// Get Order List to CRM web Filter by DatatableParameters and Warehouse.
        /// </summary>
        /// <param name="WhsCode">Warehouse Code</param>
        /// <param name="request">DataTableParameters</param>
        /// <returns>OrderSearchResponse</returns>
        /// <response code="200">OrderSearchResponse(SearchResponse)</response>
        // POST: api/Order/Search/:WhsCode
        [ProducesResponseType(typeof(OrderSearchResponse), StatusCodes.Status200OK)]
        [HttpPost("Search/{WhsCode}")]
        public async Task<IActionResult> SearchWarehouseFilter(string WhsCode, [FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();
            where.Add($"warehouse.\"WhsCode\" = '{WhsCode}'");

            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(ord.\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(employee.\"SlpName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty)
            {
                where.Add($"LOWER(contact.\"CardFName\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty)
            {
                where.Add($"LOWER(contact.\"CardName\") Like LOWER('%{request.columns[3].search.value}%')");
            }
            if (request.columns[5].search.value != String.Empty)
            {
                where.Add($"ord.\"DocTotal\" = {request.columns[5].search.value}");
            }
            if (request.columns[6].search.value != String.Empty)
            {
                where.Add($"LOWER(ord.\"DocCur\") Like LOWER('%{request.columns[6].search.value}%')");
            }
            if (request.columns[7].search.value != String.Empty)
            {
                where.Add($"LOWER(payment.\"PymntGroup\") Like LOWER('%{request.columns[7].search.value}%')");
            }
            if (request.columns[8].search.value != String.Empty)
            {

                List<string> whereOR = new List<string>();
                if ("Abierto".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"ord.""DocStatus"" = 'O' ");
                }
                if ("Cerrado".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"ord.""DocStatus"" = 'C' ");
                }
                if ("Cancelado".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"ord.""CANCELED"" = 'Y' ");
                }

                string whereORClause = "(" + String.Join(" OR ", whereOR) + ")";
                where.Add(whereORClause);
            }
            if (request.columns[9].search.value != String.Empty)
            {
                where.Add($"to_char(to_date(SUBSTRING(ord.\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[9].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY ord.\"DocNum\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY employee.\"SlpName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 2)
            {
                orderby = $" ORDER BY contact.\"CardFName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 3)
            {
                orderby = $" ORDER BY contact.\"CardName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 5)
            {
                orderby = $" ORDER BY ord.\"DocTotal\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 6)
            {
                orderby = $" ORDER BY ord.\"DocCur\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 7)
            {
                orderby = $" ORDER BY payment.\"PymntGroup\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 8)
            {
                orderby = $" ORDER BY ord.\"DocStatus\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 9)
            {
                orderby = $" ORDER BY ord.\"DocDate\" {request.order[0].dir}";
            }
            else
            {
                orderby = $" ORDER BY ord.\"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",

                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",
                    
                    (case when ord.""DocCur"" = 'USD' then ord.""DocTotalFC""
                    else ord.""DocTotal"" end)  AS  ""DocTotal"",

                    ord.""CardName"",
                    ord.""DocCur"",
                    payment.""PymntGroup"",
                    contact.""CardFName"",
                    employee.""SlpName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OSLP employee ON ord.""SlpCode"" = employee.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0)
            {
                query += "Where " + whereClause;
            }

            query += orderby;

            if (request.length != -1)
            {
                query += " LIMIT " + request.length + " OFFSET " + request.start + "";
            }

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"].ToObject<List<OrderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OSLP employee ON ord.""SlpCode"" = employee.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0)
            {
                queryCount += "Where " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0]["COUNT"].ToObject<int>();

            var respose = new OrderSearchResponse
            {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            return Ok(respose);
        }

        // GET: api/Order/WMSDetail/5]
        [Authorize]
        [HttpGet("WMSDetail/{DocEntry}")]
        public async Task<IActionResult> GetWMSDetail(int DocEntry)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            OrderDetail orderDetail;
            JToken order;
            string DocCur;
            oRecSet.DoQuery(@"
                SELECT
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    to_char(to_date(SUBSTRING(ord.""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",
                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(ord.""CancelDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""CancelDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",

                    (case when ord.""DocCur"" = 'USD' then ord.""DocTotalFC""
                    else ord.""DocTotal"" end)  AS  ""Total"",

                    SUBSTRING(ord.""DocTime"" , 0, LENGTH(ord.""DocTime"")-2) || ':' || RIGHT(ord.""DocTime"",2) as ""DocTime"",
                    
                    ord.""Address"",
                    ord.""Address2"",
                    ord.""DocCur"",
                    ord.""Comments"",
                    ord.""DocRate"",
                    payment.""PymntGroup"",
                    contact.""CardName"",
                    contact.""CardCode"",
                    contact.""CardFName"",
                    contact.""ListNum"",
                    employee.""SlpCode"",
                    employee.""SlpName"",
                    warehouse.""WhsCode"",
                    warehouse.""WhsName""
                FROM ORDR ord
                LEFT JOIN NNM1 series ON series.""Series"" = ord.""Series""
                LEFT JOIN OWHS warehouse ON warehouse.""WhsCode"" = series.""SeriesName""
                LEFT JOIN OSLP employee ON employee.""SlpCode"" = ord.""SlpCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OCRD contact ON contact.""CardCode"" = ord.""CardCode""
                WHERE ord.""DocEntry"" = '" + DocEntry + "' ");

            if (oRecSet.RecordCount == 0)
            {
                return NotFound("No Existe Documento");
            }

            order = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0];
            DocCur = order["DocCur"].ToString();
            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""Dscription"",
                    ""Price"",
                    ""Currency"",

                    (case when ""U_CjsPsVr"" != '0' then ""U_CjsPsVr""
                    else ""Quantity"" end)  AS  ""Quantity"",
                    
                    (case when ""U_CjsPsVr"" != '0' then 'CAJA'
                    else ""UomCode"" end)  AS  ""UomCode"",
                    
                    ""InvQty"",
                    ""UomCode2"",

                    (case when '" + DocCur + @"' = 'USD' then ""TotalFrgn""
                    else ""LineTotal"" end)  AS  ""Total""

                From RDR1 Where ""DocEntry"" = '" + DocEntry + "'");
            oRecSet.MoveFirst();
            order["OrderRows"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            orderDetail = order.ToObject<OrderDetail>();

            order = null;
            oRecSet = null;
            DocCur = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orderDetail);
        }

        /*
        // GET: api/Order/WMSDetail/5
        [HttpGet("Test/{DocEntry}")]
        public async Task<IActionResult> GetTest(int DocEntry) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);

            if (order.GetByKey(DocEntry)) {
                order.Lines.SetCurrentLine(2);
                Console.WriteLine(order.Lines.Quantity);
                Console.WriteLine(order.Lines.RemainingOpenQuantity);
                Console.WriteLine(order.Lines.LineStatus);

                order.Lines.Quantity -= order.Lines.RemainingOpenQuantity;

                int result = order.Update();
                if (result == 0) {
                    order.GetByKey(DocEntry);
                    Console.WriteLine(order.Lines.Quantity);
                    Console.WriteLine(order.Lines.RemainingOpenQuantity);
                    Console.WriteLine(order.Lines.LineStatus);
                    return Ok();
                }
                else {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
                return Ok();

            }

            return BadRequest(new { error = "No Existe Documento" });

        }
        */

        // GET: api/Order/5
        // Orden Detalle
        [HttpGet("CRMDetail/{id}")]
        public async Task<IActionResult> GetCRMDetail(int id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                SELECT
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    to_char(to_date(SUBSTRING(ord.""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",
                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(ord.""CancelDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""CancelDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",

                    ord.""DocTime"",
                    ord.""Address"",
                    ord.""Address2"",
                    ord.""DocCur"",
                    ord.""Comments"",
                    ord.""DocTotal"",
                    ord.""DocTotalFC"",
                    ord.""DocRate"",
                    payment.""PymntGroup"",
                    contact.""CardName"",
                    contact.""CardCode"",
                    contact.""CardFName"",
                    contact.""ListNum"",
                    employee.""SlpCode"",
                    employee.""SlpName"",
                    warehouse.""WhsCode"",
                    warehouse.""WhsName""
                FROM ORDR ord
                LEFT JOIN NNM1 series ON series.""Series"" = ord.""Series""
                LEFT JOIN OWHS warehouse ON warehouse.""WhsCode"" = series.""SeriesName""
                LEFT JOIN OSLP employee ON employee.""SlpCode"" = ord.""SlpCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OCRD contact ON contact.""CardCode"" = ord.""CardCode""
                WHERE ord.""DocEntry"" = '" + id + "' ");

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0];

            oRecSet.DoQuery(@"
                Select
                    ""LineNum"",
                    ""ItemCode"",
                    ""Dscription"",
                    ""Price"",
                    ""Currency"",
                    ""Quantity"",
                    ""UomCode"",
                    ""InvQty"",
                    ""OpenQty"",
                    ""UomEntry"",
                    ""UomCode2"",
                    ""LineTotal"",
                    ""U_CjsPsVr"",
                    ""TotalFrgn"",
                    ""Rate""
                From RDR1 Where ""DocEntry"" = '" + id + "'");
            oRecSet.MoveFirst();
            temp["RDR1"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            return Ok(temp);
        }
        [AllowAnonymous]
        // GET: api/Order/CRMOrderDaily
        [HttpGet("CRMOrderDaily")]
        public async Task<IActionResult> GetCRMOrderDaily()
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"Select Count(*) as COUNT From ORDR");
            int CountAll = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0]["COUNT"].ToObject<int>();
            oRecSet.DoQuery(@"Select Count(*) as COUNT From ORDR Where ""DocDate"" = NOW()");
            int CountToday = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0]["COUNT"].ToObject<int>();
            oRecSet = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { CountAll, CountToday });
        }

        class OrderDeliveryOutputLineUom
        {
            public uint BaseEntry { get; set; }
            public string BaseUom { get; set; }
            public uint UomEntry { get; set; }
            public string UomCode { get; set; }
            public double BaseQty { get; set; }
        }
        class OrderDeliveryOutputLine
        {
            public string LineStatus { get; set; }
            public uint LineNum { get; set; }
            public string ItemCode { get; set; }
            public uint UomEntry { get; set; }
            public string WhsCode { get; set; }
            public string UomCode { get; set; }
            public double OpenInvQty { get; set; }
            public double OpenQty { get; set; }

            public string ItemName { get; set; }
            public char QryGroup42 { get; set; }
            public char QryGroup44 { get; set; }
            public char QryGroup45 { get; set; }
            public char ManBtchNum { get; set; }
            public double U_IL_PesMax { get; set; }
            public double U_IL_PesMin { get; set; }
            public double U_IL_PesProm { get; set; }
            public string U_IL_TipPes { get; set; }
            public double NumInSale { get; set; }
            public double NumInBuy { get; set; }
            public List<string> CodeBars { get; set; }
            public List<OrderDeliveryOutputLineUom> Uoms { get; set; }
        }
        class OrderDeliveryOutputLineNew
        {
            public string LineStatus { get; set; }
            public uint LineNum { get; set; }
            public string ItemCode { get; set; }
            public uint UomEntry { get; set; }
            public string WhsCode { get; set; }
            public string UomCode { get; set; }
            public double OpenInvQty { get; set; }
            public double OpenQty { get; set; }

            public string ItemName { get; set; }
            public char QryGroup42 { get; set; }
            public char QryGroup44 { get; set; }
            public char QryGroup45 { get; set; }
            public char QryGroup51 { get; set; }
            public char ManBtchNum { get; set; }
            public double U_IL_PesMax { get; set; }
            public double U_IL_PesMin { get; set; }
            public double U_IL_PesProm { get; set; }
            public string U_IL_TipPes { get; set; }
            public double NumInSale { get; set; }
            public double NumInBuy { get; set; }
            public List<object> CodeBars { get; set; }
            public List<OrderDeliveryOutputLineUom> Uoms { get; set; }
        }
        class OrderDeliveryOutput
        {
            public uint DocEntry { get; set; }
            public uint DocNum { get; set; }
            [Required]
            public string DocStatus { get; set; }
            public string CardName { get; set; }
            public string CardCode { get; set; }
            public List<OrderDeliveryOutputLine> Lines { get; set; }
        }
        class OrderDeliveryOutputNew
        {
            public uint DocEntry { get; set; }
            public uint DocNum { get; set; }
            [Required]
            public string DocStatus { get; set; }
            public string CardName { get; set; }
            public string CardCode { get; set; }
            public List<OrderDeliveryOutputLineNew> Lines { get; set; }
        }
        /// <summary>
        /// Get Order Detail to WMS App Delivery. This route return header and lines
        /// document, plus BarCodes and Uoms Detail.
        /// </summary>
        /// <param name="DocNum">DocNum. An Unsigned Integer that serve as Order Document identifier.</param>
        /// <returns>A Order Detail To Delivery</returns>
        /// <response code="200">Returns Order</response>
        /// <response code="204">No Order Found</response>
        /// <response code="400">Order Document Found, Document Close</response>
        // GET: api/Order/DeliverySAP/:DocNum
        [ProducesResponseType(typeof(OrderDeliveryOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("DeliverySAP/{DocNum}")]
        //[Authorize]
        public async Task<IActionResult> GetOrderToDeliverySAP(uint DocNum)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery($@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocStatus"",
                    ""CardName"",
                    ""CardCode""
                From ORDR WHERE ""DocNum"" = {DocNum};");

            if (oRecSet.RecordCount == 0)
            {
                return NoContent();
            }

            JToken order = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0];

            if (order["DocStatus"].ToString() != "O")
            {
                return BadRequest("Documento Cerrado");
            }

            oRecSet.DoQuery($@"
                Select
                    ""LineStatus"",
                    ""LineNum"",
                    Line.""ItemCode"",
                    ""UomEntry"",
                    ""WhsCode"",
                    ""UomCode"",
                    ""OpenInvQty"",
                    ""OpenQty"",
                    ""ItemName"",
                    ""QryGroup42"",
                    ""QryGroup44"",
                    ""QryGroup45"",
                    ""ManBtchNum"",
                    ""U_IL_PesMax"",
                    ""U_IL_PesMin"",
                    ""U_IL_PesProm"",
                    ""U_IL_TipPes"",
                    ""NumInSale"",
Detail.""UgpEntry""
                From RDR1 as Line
                JOIN OITM as Detail on Detail.""ItemCode"" = Line.""ItemCode""
                WHERE Line.""DocEntry"" = {order["DocEntry"]};");

            order["Lines"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            foreach (var line in order["Lines"])
            {
                oRecSet.DoQuery($@"Select ""BcdCode""
                    From OBCD Where ""ItemCode"" = '{line["ItemCode"]}';");


                var temp = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"].Select(Q => (string)Q["BcdCode"]);
                line["CodeBars"] = JArray.FromObject(temp);

                oRecSet.DoQuery($@"
                    Select 
                        header.""BaseUom"" as ""BaseEntry"",
                        baseUOM.""UomCode"" as ""BaseUom"",
                        detail.""UomEntry"",
                        UOM.""UomCode"",
                        detail.""BaseQty""
                    From OUGP header
                    JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                    JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                    JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                    Where header.""UgpEntry"" = '{line["UgpEntry"]}';");
                line["Uoms"] = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            }

            var output = order.ToObject<OrderDeliveryOutput>();
            return Ok(output);
        }

        /// <summary>
        /// Get Order Detail to WMS App Delivery. This route return header and lines
        /// document, plus BarCodes and Uoms Detail.
        /// </summary>
        /// <param name="DocNum">DocNum. An Unsigned Integer that serve as Order Document identifier.</param>
        /// <returns>A Order Detail To Delivery</returns>
        /// <response code="200">Returns Order</response>
        /// <response code="204">No Order Found</response>
        /// <response code="400">Order Document Found, Document Close</response>
        // GET: api/Order/DeliverySAP/:DocNum
        [ProducesResponseType(typeof(OrderDeliveryOutputNew), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("DeliverySAPNew/{DocNum}")]
        //[Authorize]
        public async Task<IActionResult> GetOrderToDeliverySAPNew(uint DocNum)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocStatus"",
                    ""CardName"",
                    ""CardCode""
                From ORDR WHERE ""DocNum"" = {DocNum};");

            if (oRecSet.RecordCount == 0)
            {
                return NoContent();
            }

            JToken order = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0];

            if (order["DocStatus"].ToString() != "O")
            {
                return BadRequest("Documento Cerrado");
            }

            oRecSet.DoQuery($@"
                Select
                    ""LineStatus"",
                    ""LineNum"",
                    Line.""ItemCode"",
                    ""UomEntry"",
                    ""WhsCode"",
                    ""UomCode"",
                    ""OpenInvQty"",
                    ""OpenQty"",
                    ""ItemName"",
                    ""QryGroup42"",
                    ""QryGroup44"",
                    ""QryGroup45"",
                    ""QryGroup51"",
                    ""ManBtchNum"",
                    ""U_IL_PesMax"",
                    ""U_IL_PesMin"",
                    ""U_IL_PesProm"",
                    ""U_IL_TipPes"",
                    ""NumInSale"",
Detail.""UgpEntry""
                From RDR1 as Line
                JOIN OITM as Detail on Detail.""ItemCode"" = Line.""ItemCode""
                WHERE Line.""DocEntry"" = {order["DocEntry"]};");

            order["Lines"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            foreach (var line in order["Lines"])
            {
                oRecSet.DoQuery($@"
Select  ""BcdCode"",T3.""BaseQty"",T0.""UomEntry""
 From OBCD T0 
 LEFT JOIN OUOM T1 on T0.""UomEntry""= T1.""UomEntry"" 
 LEFT JOIN OUGP T2 on T2.""UgpCode""='{line["ItemCode"]}' 
 LEFT JOIN UGP1 T3 on T2.""UgpEntry""= T3.""UgpEntry"" AND T0.""UomEntry""=T3.""UomEntry""
  Where ""ItemCode"" ='{line["ItemCode"]}';");

                var temp = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];
                line["CodeBars"] = JArray.FromObject(temp);

                oRecSet.DoQuery($@"
                    Select 
                        header.""BaseUom"" as ""BaseEntry"",
                        baseUOM.""UomCode"" as ""BaseUom"",
                        detail.""UomEntry"",
                        UOM.""UomCode"",
                        detail.""BaseQty""
                    From OUGP header
                    JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                    JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                    JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                    Where header.""UgpEntry"" = '{line["UgpEntry"]}';");
                line["Uoms"] = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            }

            var output = order.ToObject<OrderDeliveryOutputNew>();
            return Ok(output);
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        // GET: api/Order/Delivery/5
        // Orden Con informacion extra para la entrega
        [HttpGet("Delivery/{id}")]
        public async Task<IActionResult> GetReception(int id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocStatus"",
                    ""CardName"",
                    ""CardCode""
                From ORDR WHERE ""DocNum"" = " + id);

            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }

            JToken order = context.XMLTOJSON(oRecSet.GetAsXML());
            order["AdmInfo"]?.Parent.Remove();
            order["ORDR"] = order["ORDR"][0];

            if (order["ORDR"]["DocStatus"].ToString() != "O")
            {
                return BadRequest("Documento Cerrado");
            }

            oRecSet.DoQuery(@"
                Select
                    ""LineStatus"",
                    ""LineNum"",
                    ""ItemCode"",
                    ""Dscription"",
                    ""UomEntry"",
                    ""WhsCode"",
                    ""UomCode"",
                    ""OpenInvQty"",
                    ""OpenQty""
                From RDR1 WHERE ""DocEntry"" = " + order["ORDR"]["DocEntry"]);

            order["RDR1"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            foreach (var pro in order["RDR1"])
            {
                oRecSet.DoQuery(@"
                    Select
                        ""ItemCode"",
                        ""ItemName"",
                        ""QryGroup7"",
                        ""QryGroup41"",
                        ""QryGroup42"",
                        ""QryGroup43"",
                        ""QryGroup44"",
                        ""QryGroup45"",
                        ""ManBtchNum"",
                        ""U_IL_PesMax"",
                        ""U_IL_PesMin"",
                        ""U_IL_PesProm"",
                        ""U_IL_TipPes"",
                        ""NumInSale"",
                        ""NumInBuy""
                    From OITM Where ""ItemCode"" = '" + pro["ItemCode"] + "'");
                oRecSet.MoveFirst();
                pro["Detail"] = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
                oRecSet.DoQuery(@"
                    Select
                        ""BcdEntry"",
                        ""BcdCode"",
                        ""BcdName"",
                        ""ItemCode"",
                        ""UomEntry""
                    From OBCD Where ""ItemCode"" = '" + pro["ItemCode"] + "'");
                oRecSet.MoveFirst();
                pro["CodeBars"] = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];
            }
            return Ok(order);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // GET: api/Order/CRMList
        // Todas las Ordernes - Encabezado para lista CRM
        [HttpGet("CRMList")]
        public async Task<IActionResult> GetCRMList()
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""CardName"",
                    contact.""CardFName"",
                    person.""SlpName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }

        // GET: api/Order/CRMList
        // Todas las Ordernes - Encabezado para lista CRM
        [HttpGet("CRMAPPList/Sucursal/{id}")]
        public async Task<IActionResult> GetCRMAPPSucursalList(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""DocStatus"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""
                Where warehouse.""WhsCode"" = '" + id + @"' AND ord.""DocStatus"" = 'O' AND ord.""DocDate"" >= add_days(CURRENT_DATE, -3)");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }

        // GET: api/Order/CRMList
        // Todas las Ordernes - Encabezado para lista CRM
        [HttpGet("CRMList/Sucursal/{id}")]
        public async Task<IActionResult> GetCRMSucursalList(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""CardName"",
                    contact.""CardFName"",
                    person.""SlpName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""
                Where warehouse.""WhsCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }

        // GET: api/Order/CRMList/Contact/C00000001
        // Todas las Ordernes - Encabezado para lista CRM filtrado por cliente
        [HttpGet("CRMList/Contact/{id}")]
        public async Task<IActionResult> GetCRMListCLient(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""Series"",
                    ord.""SlpCode"",
                    ord.""CardName"",
                    person.""SlpName"",
                    warehouse.""WhsCode"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                Where ord.""CardCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }


        // GET: api/order/list
        // Ordenes Filtradas por dia
        [HttpGet("list/{date}")]
        public async Task<IActionResult> GetList(string date)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From ORDR Where \"DocDate\" = '" + date + "'");
            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false)
            {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["ORDR"] = temp["ORDR"][0];
                temp["RDR4"]?.Parent.Remove();
                temp["RDR12"]?.Parent.Remove();
                list.Add(temp);
                items.Browser.MoveNext();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(list);
        }
        // GET: api/order/list
        // Ordenes Filtradas por dia
        [AllowAnonymous]
        [HttpGet("list/{Almacen}")]
        public async Task<IActionResult> GetListByAlmacen(string Almacen)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
Select 
""DocEntry"",
""DocNum""
From ORDR ord
JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
WHERE ""WhsCode"" = '{Almacen}'
AND to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') = to_char(to_date(SUBSTRING(NOW(), 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY')");
            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }
            oRecSet.MoveFirst();
            try
            {
                JToken documentos = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
                List<DocumentosLogs> lst = documentos.ToObject<List<DocumentosLogs>>();
                List<SAPLog> logssap = _contextLogs.SAPLog.Where(x => DateTime.Compare(x.created_at.Date, DateTime.Now.Date) == 0 && x.action == "Impresion").ToList();
                foreach (var item in lst.Where(x => logssap.Exists(p => p.document == x.DocEntry)))
                {
                    item.Impreso = "Impresa";
                }
                var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Inserting Tables");


                var tableWithPeople = ws.Cell(1, 1).InsertTable(lst.AsEnumerable());

                var memoryStream = new System.IO.MemoryStream();
                using (var stream = new System.IO.MemoryStream())
                {
                    wb.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OrdenesImpresas.xlsx");
                }
            }
            catch (Exception ex)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return BadRequest(ex);
            }
        }
        public class DocumentosLogs
        {
            public string DocEntry { get; set; }
            public string DocNum { get; set; }

            public string Impreso { get; set; }
        }

        // GET: api/order/list
        // Ordenes ultmos 3 dias Filtradas por empleado 
        [HttpGet("CRMAPP/list/{employee}")]
        public async Task<IActionResult> GetCRMAPPList(string id, int employee)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                 Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""DocStatus"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""
                Where ord.""DocStatus"" = 'O' AND ord.""DocDate"" >= add_days(CURRENT_DATE, -3) AND ord.""SlpCode"" =" + employee);

            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }
        [HttpGet("CRMAPP/Mayoreos/Order/{CardCode}")]
        public async Task<IActionResult> GetMayoresOrdersApp(string CardCode)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
                SELECT
  ord.""DocEntry"",
  ord.""DocNum"",
  ord.""DocDate"",
  ord.""DocStatus"",
  ord.""CANCELED"",
  CASE
    WHEN --Cancelado
      ord.""CANCELED"" = 'Y' THEN 'X'
    WHEN --Cerrado
      ord.""DocStatus"" = 'C' THEN 'C'
    WHEN
      ord.""DocStatus"" = 'O' THEN
        CASE
          WHEN --Cerrado aunque esté abierto, porque ya se entregó todo
            SUM(product.""OpenQty"") = 0 THEN 'C'
          WHEN --Parcialmente entregado
            SUM(product.""Quantity"") > SUM(product.""OpenQty"") THEN 'P'
          ELSE
            'O'
        END
  END AS ""Estatus"",
  contact.""CardName"",
  contact.""CardFName"",
  warehouse.""WhsName""
  FROM
    ORDR ord LEFT JOIN
    NNM1 serie ON
      ord.""Series"" = serie.""Series"" LEFT JOIN
    OWHS warehouse ON
      serie.""SeriesName"" = warehouse.""WhsCode"" LEFT JOIN
    OSLP person ON
      ord.""SlpCode"" = person.""SlpCode"" LEFT JOIN
    OCRD contact ON
      ord.""CardCode"" = contact.""CardCode"" INNER JOIN
    RDR1 product ON
      ord.""DocEntry"" = product.""DocEntry""
  WHERE
    ord.""DocDate"" >= add_days(CURRENT_DATE, -3) AND
    ord.""CardCode"" LIKE '{CardCode}'
  GROUP BY
    ord.""DocEntry"",
    ord.""DocNum"",
    ord.""DocDate"",
    ord.""DocStatus"",
    ord.""CANCELED"",
    contact.""CardName"",
    contact.""CardFName"",
    warehouse.""WhsName""
  ORDER BY ""DocEntry"" DESC");

            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }
        [HttpGet("CRMAPP/listTag/{employee}")]
        public async Task<IActionResult> GetCRMAPPListTag(string employee)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
                SELECT
  ord.""DocEntry"",
  ord.""DocNum"",
  ord.""DocDate"",
  ord.""DocStatus"",
  ord.""CANCELED"",
  CASE
    WHEN --Cancelado
      ord.""CANCELED"" = 'Y' THEN 'X'
    WHEN --Cerrado
      ord.""DocStatus"" = 'C' THEN 'C'
    WHEN
      ord.""DocStatus"" = 'O' THEN
        CASE
          WHEN --Cerrado aunque esté abierto, porque ya se entregó todo
            SUM(product.""OpenQty"") = 0 THEN 'C'
          WHEN --Parcialmente entregado
            SUM(product.""Quantity"") > SUM(product.""OpenQty"") THEN 'P'
          ELSE
            'O'
        END
  END AS ""Estatus"",
  contact.""CardName"",
  contact.""CardFName"",
  warehouse.""WhsName""
  FROM
    ORDR ord LEFT JOIN
    NNM1 serie ON
      ord.""Series"" = serie.""Series"" LEFT JOIN
    OWHS warehouse ON
      serie.""SeriesName"" = warehouse.""WhsCode"" LEFT JOIN
    OSLP person ON
      ord.""SlpCode"" = person.""SlpCode"" LEFT JOIN
    OCRD contact ON
      ord.""CardCode"" = contact.""CardCode"" INNER JOIN
    RDR1 product ON
      ord.""DocEntry"" = product.""DocEntry""
  WHERE
    ord.""DocDate"" >= add_days(CURRENT_DATE, -3) AND
    ord.""U_nwr_Tag"" LIKE '{employee}'
  GROUP BY
    ord.""DocEntry"",
    ord.""DocNum"",
    ord.""DocDate"",
    ord.""DocStatus"",
    ord.""CANCELED"",
    contact.""CardName"",
    contact.""CardFName"",
    warehouse.""WhsName""
  ORDER BY ""DocEntry"" DESC");

            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }
        [AllowAnonymous]
        [HttpGet("CRMAPP/top10Sellers/{currency}")]
        public async Task<IActionResult> Top10Vendedores(string currency)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
               SELECT top 10 TO_NVARCHAR(""U_nwr_Tag"") as TAG,sum(""DocTotal"") as TOTAL FROM ORDR
WHERE (""U_nwr_Tag"" is not null ) 
AND TO_NVARCHAR(""U_nwr_Tag"")!='0'
AND ""DocCur""='{currency}' 
AND ""DocDate""= CURRENT_DATE
GROUP BY TO_NVARCHAR(""U_nwr_Tag"") order by sum(""DocTotal"") desc");

            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            IList<top10> person = orders.ToObject<IList<top10>>();
            foreach (var item in person)
            {
                item.Name = _context.Users.Where(x => x.Id == item.TAG).First().Name;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(person);
        }
        [HttpGet("CRMAPP/compraspormes/{CardCode}&{Month}&{Year}")]
        public async Task<IActionResult> comprasporMes(string CardCode, int Month, int Year)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
SELECT  T0.""DocDate"",count(T0.""DocEntry"") FROM ORDR T0
 WHERE ""CardCode""='{CardCode}'
 AND MONTH (T0.""DocDate"")={Month}
 AND YEAR (T0.""DocDate"")={Year}
GROUP BY T0.""DocDate""
order by T0.""DocDate"" asc
");

            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }

        [HttpGet("CRMAPP/comprastopProductos/{CardCode}&{Month}&{Year}")]
        public async Task<IActionResult> comprastop10Mes(string CardCode, int Month, int Year)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
SELECT  top 10 ""Dscription"",""ItemCode"",sum(T1.""Quantity"") FROM ORDR T0
join RDR1 T1 on T0.""DocEntry""=T1.""DocEntry""
 WHERE ""CardCode""='{CardCode}' 
 AND MONTH (T0.""DocDate"")={Month}
 AND YEAR (T0.""DocDate"")={Year}
GROUP BY ""Dscription"",""ItemCode"" order by sum(T1.""Quantity"") desc
");

            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }
        public class top10
        {
            public string TAG { get; set; }
            public string TOTAL { get; set; }

            public string Name { get; set; }
        }
        // GET: api/Order/5
        // Orden Detalle
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
SELECT
  ord.""DocStatus"",
  ord.""DocEntry"",
  ord.""DocNum"",
  ord.""DocDate"",
  ord.""DocTime"",
  ord.""DocDueDate"",
  ord.""CancelDate"",
  ord.""Address"",
  ord.""Address2"",
  ord.""DocCur"",
  ord.""Comments"",
  ord.""DocTotal"",
  ord.""DocTotalFC"",
  ord.""DocRate"",
  CASE
    WHEN--Cancelado
      ord.""CANCELED"" = 'Y' THEN 'X'
    WHEN--Cerrado
      ord.""DocStatus"" = 'C' THEN 'C'
    WHEN
      ord.""DocStatus"" = 'O' THEN
        CASE
          WHEN--Cerrado aunque esté abierto, porque ya se entregó todo
            SUM(product.""OpenQty"") = 0 THEN 'C'
          WHEN--Parcialmente entregado
            SUM(product.""Quantity"") > SUM(product.""OpenQty"") THEN 'P'
          ELSE
            'O'
        END
  END AS ""Estatus"",
  --SUM(product.""Quantity""),
  --SUM(product.""OpenQty""),
  payment.""PymntGroup"",
  contact.""CardName"",
  contact.""CardCode"",
  contact.""CardFName"",
  employee.""SlpCode"",
  employee.""SlpName"",
  warehouse.""WhsName""
  FROM
    ORDR ord LEFT JOIN
    NNM1 series ON
      series.""Series"" = ord.""Series"" LEFT JOIN
    OWHS warehouse ON
      warehouse.""WhsCode"" = series.""SeriesName"" LEFT JOIN
    OSLP employee ON
      employee.""SlpCode"" = ord.""SlpCode"" LEFT JOIN
    OCTG payment ON
      payment.""GroupNum"" = ord.""GroupNum"" LEFT JOIN
    OCRD contact ON
      contact.""CardCode"" = ord.""CardCode"" INNER JOIN
    RDR1 product ON
      ord.""DocEntry"" = product.""DocEntry""
  WHERE
    ord.""DocEntry"" = '" + id + @"'
  GROUP BY
    ord.""DocStatus"",
    ord.""DocEntry"",
    ord.""DocNum"",
    ord.""DocDate"",
    ord.""DocTime"",
    ord.""DocDueDate"",
    ord.""CancelDate"",
    ord.""Address"",
    ord.""Address2"",
    ord.""DocCur"",
    ord.""Comments"",
    ord.""DocTotal"",
    ord.""DocTotalFC"",
    ord.""DocRate"",
    ord.""CANCELED"",
    payment.""PymntGroup"",
    contact.""CardName"",
    contact.""CardCode"",
    contact.""CardFName"",
    employee.""SlpCode"",
    employee.""SlpName"",
    warehouse.""WhsName""");

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML());
            temp["ORDR"] = temp["ORDR"][0];
            temp["AdmInfo"]?.Parent.Remove();

            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""Dscription"",
                    ""Price"",
                    ""Currency"",
                    ""Quantity"",
                    ""OpenQty"",
                    ""UomCode"",
                    ""InvQty"",
                    ""UomCode2"",
                    ""LineTotal"",
                    ""U_CjsPsVr"",
                    ""TotalFrgn"",
                    ""Rate""
                From RDR1 Where ""DocEntry"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML());
            temp["RDR1"] = products["RDR1"];
            return Ok(temp);
        }


        private JToken limiteCredito(string CardCode, int Series, SAPContext context)
        {

            JToken result;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"CALL ""ValidaCreditoMXM"" ('{CardCode}', '{Series}', 0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null)
            {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoMXM'}");
            }

            oRecSet.DoQuery($@"CALL ""ValidaCreditoENS"" ('{CardCode}', '{Series}', 0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null)
            {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoENS'}");
            }

            oRecSet.DoQuery($@"CALL ""ValidaCreditoTJ"" ('{CardCode}', '{Series}', 0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null)
            {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoTJ'}");
            }

            oRecSet.DoQuery($@"CALL ""ValidaCreditoSLR"" ('{CardCode}', '{Series}', 0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null)
            {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoSLR'}");
            }
            return JObject.Parse(@"{ RESULT: 'False', AUTH: ''}");

        }

        private JToken facturasPendientes(string CardCode, int Series, SAPContext context)
        {

            JToken result;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery($@"
                SELECT 'True' as Result, 'FacturasVencidasMXM' as Auth
                FROM Dummy
                WHERE '{CardCode}' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE AND T0.""DocStatus"" = 'O')
                AND  '{Series}' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 1))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty)
            {
                return result;
            }

            oRecSet.DoQuery($@"
                SELECT 'True' as Result, 'FacturasVencidasENS' as Auth
                FROM Dummy
                WHERE '{CardCode}' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE AND T0.""DocStatus"" = 'O')
                AND  '{Series}' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 4))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty)
            {
                return result;
            }

            oRecSet.DoQuery($@"
                SELECT 'True' as Result, 'FacturasVencidasTJ' as Auth
                FROM Dummy
                WHERE '{CardCode}' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE AND T0.""DocStatus"" = 'O')
                AND  '{Series}' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 2))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty)
            {
                return result;
            }

            oRecSet.DoQuery($@"
                SELECT 'True' as Result, 'FacturasVencidasSLR' as Auth
                FROM Dummy
                WHERE '{CardCode}' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE AND T0.""DocStatus"" = 'O')
                AND  '{Series}' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 3))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty)
            {
                return result;
            }
            return JObject.Parse(@"{ RESULT: 'False', AUTH: ''}");
        }

        private List<JToken> auth(string CardCode, int Series, SAPContext context)
        {
            List<JToken> result = new List<JToken>();
            JToken resultfact = facturasPendientes(CardCode, Series, context);
            JToken resultcredit = limiteCredito(CardCode, Series, context);
            result.Add(resultfact);
            result.Add(resultcredit);
            return result;
        }




        // POST: api/Order
        // Creacion de Orden
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateOrder value)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (value.auth == 0 && value.payment != 19)
            {
                List<JToken> resultAuth = new List<JToken>();

                if (value.payment == 8)
                {
                    resultAuth.Add(facturasPendientes(value.cardcode, value.series, context));
                    if (resultAuth[0]["RESULT"].ToString() == "True")
                    {
                        //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                        return Conflict(resultAuth);
                    }
                }
                else
                {
                    resultAuth = auth(value.cardcode, value.series, context);
                    if (resultAuth[0]["RESULT"].ToString() == "True" || resultAuth[1]["RESULT"].ToString() == "True")
                    {
                        //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                        return Conflict(resultAuth);
                    }
                }
            }

            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            SAPbobsCOM.BusinessPartners contact = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);

            oRecSet.DoQuery($@"
                Select
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 17 AND serie.""Series"" = {value.series};");

            if (oRecSet.RecordCount == 0)
            {
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return BadRequest(new { error = "Error en sucursal" });
            }

            string warehouse = (string)oRecSet.Fields.Item("WhsCode").Value;

            order.CardCode = value.cardcode;
            order.Series = value.series;
            order.DocCurrency = value.currency;
            order.DocDueDate = value.date;
            //  order.SalesPersonCode=value.SlpCode!=0?value.SlpCode:104;
            order.PaymentGroupCode = value.payment;
            if (value.idUsuario != null)
                order.UserFields.Fields.Item("U_nwr_Tag").Value = value.idUsuario.ToString();

            if (!contact.GetByKey(value.cardcode))
            {
                string error = context.oCompany.GetLastErrorDescription();
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return BadRequest(new { error });
            }

            String temp = (String)contact.UserFields.Fields.Item("U_B1SYS_MainUsage").Value;
            if (temp != String.Empty)
            {
                order.UserFields.Fields.Item("U_SO1_02USOCFDI").Value = temp;
            }
            temp = (String)contact.UserFields.Fields.Item("U_IL_MetPago").Value;
            if (temp != String.Empty)
            {
                order.UserFields.Fields.Item("U_SO1_02METODOPAGO").Value = temp;
            }
            temp = (String)contact.UserFields.Fields.Item("U_IL_ForPago").Value;
            if (temp != String.Empty)
            {
                order.UserFields.Fields.Item("U_SO1_02FORMAPAGO").Value = temp;
            }

            for (int i = 0; i < value.rows.Count; i++)
            {

                order.Lines.ItemCode = value.rows[i].code;
                order.Lines.WarehouseCode = warehouse;

                oRecSet.DoQuery($@"
                    Select
                        ""Currency"",
                        ""Price""
                    FROM ITM1
                    WHERE ""ItemCode"" = '{value.rows[i].code}' 
                    AND ""PriceList"" = {value.priceList};");

                if (oRecSet.RecordCount == 0)
                {
                    //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return BadRequest(new { error = "Error en Lista de Precio" });
                }

                double Price = (double)oRecSet.Fields.Item("Price").Value;
                string Currency = (string)oRecSet.Fields.Item("Currency").Value;

                if (value.rows[i].uom == -2)
                {
                    order.Lines.UnitPrice = Price;
                }
                else
                {
                    order.Lines.UnitPrice = Price * value.rows[i].equivalentePV;
                }
                order.Lines.Currency = Currency;

                if (value.rows[i].uom == -2)
                {
                    order.Lines.UoMEntry = 185;
                    order.Lines.UserFields.Fields.Item("U_CjsPsVr").Value = value.rows[i].quantity;
                    order.Lines.Quantity = value.rows[i].quantity * value.rows[i].equivalentePV;
                }
                else
                {
                    order.Lines.Quantity = value.rows[i].quantity;
                    order.Lines.UoMEntry = value.rows[i].uom;
                }

                order.Lines.Add();
            }

            order.Comments = value.comments;

            if (order.Add() == 0)
            {
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                return Ok();
            }
            else
            {
                string error = context.oCompany.GetLastErrorDescription();
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return BadRequest(new { error });
            }
        }

        // POST: api/Order
        // Creacion de Orden
        [HttpPost("Retail")]
        public async Task<IActionResult> PostRetail([FromBody] CreateOrderRetail value)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            SAPbobsCOM.Items items = (SAPbobsCOM.Items)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems);

            oRecSet.DoQuery(@"
                Select
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 17 AND serie.""Series"" = " + value.series);
            oRecSet.MoveFirst();
            string warehouse = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"][0]["WhsCode"].ToString();

            order.CardCode = value.cardcode;
            order.Series = value.series;
            order.DocCurrency = value.currency;
            order.DocDueDate = value.date;
            order.Address = value.address;
            order.Address2 = "";
            for (int i = 0; i < value.rows.Count; i++)
            {
                order.Lines.ItemCode = value.rows[i].code;
                order.Lines.WarehouseCode = warehouse;

                items.GetByKey(value.rows[i].code);

                for (int j = 0; j < items.PriceList.Count; j++)
                {
                    items.PriceList.SetCurrentLine(j);
                    if (items.PriceList.PriceList == 18)
                    {
                        if (value.rows[i].uom == -2)
                        {
                            order.Lines.UnitPrice = items.PriceList.Price;
                        }
                        else
                        {
                            order.Lines.UnitPrice = items.PriceList.Price * value.rows[i].equivalentePV;
                        }
                        order.Lines.Currency = items.PriceList.Currency;
                        break;
                    }
                }

                if (value.rows[i].uom == -2)
                {
                    order.Lines.UoMEntry = 6;
                    order.Lines.UserFields.Fields.Item("U_CjsPsVr").Value = value.rows[i].quantity;
                    order.Lines.Quantity = value.rows[i].quantity * value.rows[i].equivalentePV;
                }
                else
                {
                    order.Lines.Quantity = value.rows[i].quantity;
                    order.Lines.UoMEntry = value.rows[i].uom;
                }

                order.Lines.Add();
            }

            order.Comments = value.comments;
            int result = order.Add();
            if (result == 0)
            {
                return Ok(new { value = context.oCompany.GetNewObjectKey() });
            }
            else
            {
                string error = context.oCompany.GetLastErrorDescription();
                return BadRequest(new { error });
            }
        }

        // PUT: api/Order/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateOrder value)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Items items = (SAPbobsCOM.Items)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (order.GetByKey(id))
            {
                oRecSet.DoQuery(@"
                    Select
                        warehouse.""WhsName"",
                        warehouse.""WhsCode"",
                        serie.""Series""
                    From OWHS warehouse
                    LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                    Where serie.""Series"" = '" + order.Series + "'");
                oRecSet.MoveFirst();
                string warehouse = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"][0]["WhsCode"].ToString();
                order.Lines.Add();
                for (int i = 0; i < value.newProducts.Count; i++)
                {
                    order.Lines.ItemCode = value.newProducts[i].code;
                    order.Lines.WarehouseCode = warehouse;

                    items.GetByKey(value.newProducts[i].code);

                    for (int j = 0; j < items.PriceList.Count; j++)
                    {
                        items.PriceList.SetCurrentLine(j);
                        if (items.PriceList.PriceList == 2)
                        {
                            if (value.newProducts[i].uom == -2)
                            {
                                order.Lines.UnitPrice = items.PriceList.Price;
                            }
                            else
                            {
                                order.Lines.UnitPrice = items.PriceList.Price * value.newProducts[i].equivalentePV;
                            }
                            order.Lines.Currency = items.PriceList.Currency;
                            break;
                        }
                    }

                    if (value.newProducts[i].uom == -2)
                    {
                        order.Lines.UoMEntry = 6;
                        order.Lines.UserFields.Fields.Item("U_CjsPsVr").Value = value.newProducts[i].quantity;
                        order.Lines.Quantity = value.newProducts[i].quantity * value.newProducts[i].equivalentePV;
                    }
                    else
                    {
                        order.Lines.Quantity = value.newProducts[i].quantity;
                        order.Lines.UoMEntry = value.newProducts[i].uom;
                    }

                    order.Lines.Add();
                }


                for (int i = 0; i < value.ProductsChanged.Count; i++)
                {
                    order.Lines.SetCurrentLine(value.ProductsChanged[i].LineNum);
                    if (order.Lines.Quantity != value.ProductsChanged[i].quantity)
                    {
                        order.Lines.Quantity = value.ProductsChanged[i].quantity;
                    }

                    if (order.Lines.UoMEntry != value.ProductsChanged[i].uom)
                    {
                        order.Lines.UoMEntry = value.ProductsChanged[i].uom;
                        items.GetByKey(order.Lines.ItemCode);
                        for (int j = 0; j < items.PriceList.Count; j++)
                        {
                            items.PriceList.SetCurrentLine(j);
                            if (items.PriceList.PriceList == 2)
                            {
                                order.Lines.UnitPrice = items.PriceList.Price * value.ProductsChanged[i].equivalentePV;
                                order.Lines.Currency = items.PriceList.Currency;
                                break;
                            }
                        }
                    }
                }

                int result = order.Update();
                if (result == 0)
                {
                    return Ok();
                }
                else
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }

            }

            return BadRequest(new { error = "No Existe Documento" });
        }

    }
}
