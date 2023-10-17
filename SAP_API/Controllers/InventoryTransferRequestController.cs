using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;


namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryTransferRequestController : ControllerBase
    {


        /// <summary>
        /// Get InvntoryTransferRequest List to WMS web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>TransferRequestSearchResponse</returns>
        /// <response code="200">TransferRequestSearchResponse(SearchResponse)</response>
        // POST: api/InventoryTransferRequest/Search
        [ProducesResponseType(typeof(TransferRequestSearchResponse), StatusCodes.Status200OK)]
        [HttpPost("Search")]
        public async Task<IActionResult> GetSearch([FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(\"Filler\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty)
            {
                where.Add($"LOWER(\"ToWhsCode\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty)
            {
                List<string> whereOR = new List<string>();
                if ("Abierto".Contains(request.columns[3].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"""DocStatus"" = 'O' ");
                }
                if ("Cerrado".Contains(request.columns[3].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"""DocStatus"" = 'C' ");
                }
                if ("Cancelado".Contains(request.columns[3].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"""CANCELED"" = 'Y' ");
                }

                string whereORClause = "(" + String.Join(" OR ", whereOR) + ")";
                where.Add(whereORClause);
            }
            if (request.columns[4].search.value != String.Empty)
            {
                where.Add($"to_char(to_date(SUBSTRING(\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[4].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY \"DocNum\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY \"Filler\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 2)
            {
                orderby = $" ORDER BY \"ToWhsCode\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 3)
            {
                orderby = $" ORDER BY \"DocStatus\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 4)
            {
                orderby = $" ORDER BY \"DocDate\" {request.order[0].dir}";
            }
            else
            {
                orderby = $" ORDER BY \"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ""DocEntry"",
                    ""DocNum"",

                    to_char(to_date(SUBSTRING(""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when ""CANCELED"" = 'Y' then 'Cancelado'
                    when ""DocStatus"" = 'O' then 'Abierto'
                    when ""DocStatus"" = 'C' then 'Cerrado'
                    else ""DocStatus"" end)  AS  ""DocStatus"",

                    ""ToWhsCode"",
                    ""Filler""
                From OWTQ ";

            if (where.Count != 0)
            {
                query += "Where " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            List<TransferRequestSearchDetail> orders = context.XMLTOJSON(oRecSet.GetAsXML())["OWTQ"].ToObject<List<TransferRequestSearchDetail>>();

            string queryCount = @"Select Count (*) as COUNT From OWTQ ";

            if (where.Count != 0)
            {
                queryCount += "Where " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OWTQ"][0]["COUNT"].ToObject<int>();

            TransferRequestSearchResponse respose = new TransferRequestSearchResponse
            {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(respose);
        }

        // GET: api/InventoryTransferRequest/WMSDetail/5/DocEntry
        [HttpGet("WMSDetail/{id}/{doctype}")]
        public async Task<IActionResult> GetWMSDetail(int id, string doctype)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (!doctype.Equals("DocEntry") && !doctype.Equals("DocNum"))
            {
                return BadRequest(new { error = "Doc Type to Search Invalid" });
            }

            oRecSet.DoQuery(@"
                SELECT
                    document.""DocEntry"",
                    document.""DocNum"",

                    to_char(to_date(SUBSTRING(document.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when document.""CANCELED"" = 'Y' then 'Cancelado'
                    when document.""DocStatus"" = 'O' then 'Abierto'
                    when document.""DocStatus"" = 'C' then 'Cerrado'
                    else document.""DocStatus"" end)  AS  ""DocStatus"",

                    document.""Filler"",

                    whsOrigin.""WhsName"" as ""WhsNameOrigin"",
                    whsDest.""WhsName"" as ""WhsNameDest""

                From OWTQ document
                JOIN OWHS whsOrigin ON document.""Filler"" = whsOrigin.""WhsCode""
                JOIN OWHS whsDest ON document.""ToWhsCode"" = whsDest.""WhsCode""
                WHERE """ + doctype + @""" = " + id);

            if (oRecSet.RecordCount == 0)
            {
                return NotFound("No Existe Documento");
            }

            JToken transfer = context.XMLTOJSON(oRecSet.GetAsXML())["OWTQ"][0];
            int DocEntry;
            int DocNum;
            if (doctype.Equals("DocEntry"))
            {
                DocEntry = id;
                DocNum = transfer["DocNum"].ToObject<int>();
            }
            else
            {
                DocEntry = transfer["DocEntry"].ToObject<int>();
                DocNum = id;
            }

            oRecSet.DoQuery(@"
                SELECT
                    ""LineNum"",

                    (case when ""LineStatus"" = 'O' then 'Abierto'
                    when ""LineStatus"" = 'C' then 'Cerrado'
                    else ""LineStatus"" end)  AS  ""LineStatus"",

                    ""ItemCode"",
                    ""Dscription"",
                    ""Quantity"",
                    ""UomCode""
                FROM WTQ1 WHERE ""DocEntry"" = " + DocEntry);
            transfer["Lines"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTQ1"];

            // Inventory Transfers
            oRecSet.DoQuery(@"
                SELECT
                    ""DocEntry"",
                    ""DocNum"",

                    to_char(to_date(SUBSTRING(""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",

                    ""ToWhsCode"",
                    ""Filler""
                FROM OWTR
                WHERE ""DocEntry"" in (SELECT ""DocEntry"" FROM WTR1 WHERE ""BaseEntry"" = " + DocEntry + ")");

            if (oRecSet.RecordCount != 0)
            {
                transfer["TransferList"] = context.XMLTOJSON(oRecSet.GetAsXML())["OWTR"];

                oRecSet.DoQuery(@"
                    SELECT
                        ""DocEntry"",
                        ""ItemCode"",
                        ""Dscription"",
                        ""Quantity"",
                        ""UomCode""
                    FROM WTR1
                    WHERE ""BaseEntry"" = " + DocEntry);
                transfer["TransferLines"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTR1"];

                List<string> DocEntryListToGetBarCodes = new List<string>();

                foreach (JToken transfers in transfer["TransferList"])
                {
                    DocEntryListToGetBarCodes.Add(transfers["DocEntry"].ToString());
                }

                string DocEntriesToGetBarCode = String.Join(",", DocEntryListToGetBarCodes);

                oRecSet.DoQuery(@"
                    SELECT
                        ""ItemCode"",
                        ""BatchNum"",
                        ""LineNum"",
                        ""BaseEntry"",
                        ""Quantity""
                    From IBT1 WHERE ""BaseEntry"" in (" + DocEntriesToGetBarCode + @") AND ""WhsCode"" = '" + transfer["Filler"].ToString() + "'");
                transfer["BarCodes"] = context.XMLTOJSON(oRecSet.GetAsXML())["IBT1"];

                //foreach (JToken transferDetail in transfer["TransfersDetail"])
                //{
                //    oRecSet.DoQuery(@"SELECT * From IBT1 WHERE ""ItemCode"" = '" + transferDetail["ItemCode"] + @"' AND ""BaseEntry"" = " + transferDetail["DocEntry"] + @" AND ""WhsCode"" = '" + transferDetail["FromWhsCod"] + "'");
                //    transferDetail["Codes"] = context.XMLTOJSON(oRecSet.GetAsXML())["IBT1"];
                //}

                oRecSet.DoQuery(@"
                    SELECT
                        ""DocNum"",
                        ""DocEntry"",

                        to_char(to_date(SUBSTRING(""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                        (case when ""CANCELED"" = 'Y' then 'Cancelado'
                        when ""DocStatus"" = 'O' then 'Abierto'
                        when ""DocStatus"" = 'C' then 'Cerrado'
                        else ""DocStatus"" end)  AS  ""DocStatus""

                    From OWTQ WHERE ""U_SO1_02NUMRECEPCION"" = " + DocNum);

                if (oRecSet.RecordCount != 0)
                {

                    transfer["TransferRequestCopyList"] = context.XMLTOJSON(oRecSet.GetAsXML())["OWTQ"];

                    oRecSet.DoQuery(@"
                        SELECT
                            ""DocEntry"",
                            ""ItemCode"",
                            ""Quantity"",
                            ""UomCode""
                        FROM WTQ1 WHERE ""DocEntry"" in (SELECT ""DocEntry"" From OWTQ WHERE ""U_SO1_02NUMRECEPCION"" = " + DocNum + ")");
                    transfer["TransferRequestCopyLines"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTQ1"];
                }
            }

            return Ok(transfer);
        }

        class TransferDeliveryOutputLineUom
        {
            public uint BaseEntry { get; set; }
            public string BaseUom { get; set; }
            public uint UomEntry { get; set; }
            public string UomCode { get; set; }
            public double BaseQty { get; set; }
        }
        class TransferDeliveryOutputLine
        {
            public string LineStatus { get; set; }
            public uint LineNum { get; set; }
            public string ItemCode { get; set; }
            public uint UomEntry { get; set; }
            public string WhsCode { get; set; }
            public string UomCode { get; set; }
            public double OpenInvQty { get; set; }
            public double OpenQty { get; set; }
            public string FromWhsCod { get; set; }
            public string ItemName { get; set; }
            public char QryGroup44 { get; set; }
            public char QryGroup45 { get; set; }
            public char ManBtchNum { get; set; }
            public double U_IL_PesMax { get; set; }
            public double U_IL_PesMin { get; set; }
            public double U_IL_PesProm { get; set; }
            public string U_IL_TipPes { get; set; }
            public double NumInSale { get; set; }
            public double NumInBuy { get; set; }
            public List<object> CodeBars { get; set; }
            public List<TransferDeliveryOutputLineUom> Uoms { get; set; }
        }
        class TransferDeliveryOutput
        {
            public uint DocEntry { get; set; }
            public uint DocNum { get; set; }
            public string DocStatus { get; set; }
            public string ToWhsCode { get; set; }
            public string Filler { get; set; }
            public List<TransferDeliveryOutputLine> Lines { get; set; }
        }
        ///REFCIFICACION DE DOCUMENTOS
        /// <summary>
        /// Get TransferRequest Detail to WMS App Delivery. This route return header and lines
        /// document, plus BarCodes and Uoms Detail.
        /// </summary>
        /// <param name="DocNum">DocNum. An Unsigned Integer that serve as TrasnferRequest Document identifier.</param>
        /// <returns>A TrasnferRequest Detail To Delivery</returns>
        /// <response code="200">Returns TransferRequest</response>
        /// <response code="204">No Order Found</response>
        /// <response code="400">Document Found, Document Close</response>
        // GET: api/InventoryTransferRequest/DeliverySAP/:DocNum
        [ProducesResponseType(typeof(TransferDeliveryOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        //[Authorize]
        [HttpGet("DeliverySAP/{DocNum}")]
        public async Task<IActionResult> GetDeliveryTransferRequest(uint DocNum)
        {


            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocStatus"",
                    ""ToWhsCode"",
                    ""Filler""
                From OWTQ WHERE ""DocNum"" = {DocNum};");

            if (oRecSet.RecordCount == 0)
            {
                return NoContent();
            }

            JToken request = context.XMLTOJSON(oRecSet.GetAsXML())["OWTQ"][0];

            if (request["DocStatus"].ToString() != "O")
            {
                return BadRequest("Documento Cerrado");
            }

            oRecSet.DoQuery($@"
                Select
                    ""LineStatus"",
                    ""LineNum"",
                    Line.""ItemCode"",
                    ""FromWhsCod"",
                    ""UomEntry"",
                    ""WhsCode"",
                    ""UomCode"",
                    ""OpenInvQty"",
                    ""OpenQty"",
                    ""ItemName"",
                    ""QryGroup44"",
                    ""QryGroup45"",
                    ""ManBtchNum"",
                    ""U_IL_PesMax"",
                    ""U_IL_PesMin"",
                    ""U_IL_PesProm"",
                    ""U_IL_TipPes"",
                    ""NumInSale"",
Detail.""UgpEntry""
                From WTQ1 as Line
                JOIN OITM as Detail on Detail.""ItemCode"" = Line.""ItemCode""
                WHERE Line.""DocEntry"" = {request["DocEntry"]};");

            request["Lines"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTQ1"];
            foreach (var line in request["Lines"])
            {

                oRecSet.DoQuery($@"
                    Select ""BcdCode"",""UomEntry""
                    From OBCD Where ""ItemCode"" = '{line["ItemCode"]}';");

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

            TransferDeliveryOutput output = request.ToObject<TransferDeliveryOutput>();
            return Ok(output);
        }

        // GET: api/InventoryTransferRequest/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.StockTransfer request = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryTransferRequest);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (request.GetByKey(id))
            {
                JToken temp = context.XMLTOJSON(request.GetAsXML());
                temp["OWTQ"] = temp["OWTQ"][0];
                temp["AdmInfo"]?.Parent.Remove();
                temp["WTQ12"]?.Parent.Remove();
                return Ok(temp);
            }
            return NotFound("No Existe Documento");
        }

        // DEPRECATED
        // GET: api/InventoryTransferRequest/Reception/5
        [HttpGet("Reception/{id}")]
        public async Task<IActionResult> GetReception(int id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocStatus"",
                    ""ToWhsCode"",
                    ""Filler""
                From OWTQ WHERE ""DocNum"" = " + id);

            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }

            JToken request = context.XMLTOJSON(oRecSet.GetAsXML());
            request["AdmInfo"]?.Parent.Remove();
            request["OWTQ"] = request["OWTQ"][0];

            if (request["OWTQ"]["DocStatus"].ToString() != "O")
            {
                return BadRequest("Documento Cerrado");
            }

            oRecSet.DoQuery(@"
                Select
                    ""LineStatus"",
                    ""LineNum"",
                    ""ItemCode"",
                    ""Dscription"",
                    ""UseBaseUn"",
                    ""UomEntry"",
                    ""WhsCode"",
                    ""UomCode"",
                    ""FromWhsCod"",
                    ""OpenInvQty"",
                    ""OpenQty""
                From WTQ1 WHERE ""DocEntry"" = " + request["OWTQ"]["DocEntry"]);

            request["WTQ1"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTQ1"];

            foreach (var pro in request["WTQ1"])
            {
                oRecSet.DoQuery(@"
                    Select
                        ""ItemCode"",
                        ""ItemName"",
                        ""QryGroup7"",
                        ""QryGroup41"",
                        ""QryGroup42"",
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

            return Ok(request);
        }

        // GET: api/InventoryTransferRequest/list
        [HttpGet("list/{date}")]
        public async Task<IActionResult> GetList(string date)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocDate"",
                    ""CANCELED"",
                    ""DocStatus""
                From OWTQ Where ""DocDate"" = '" + date + "'");

            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }

            JToken request = context.XMLTOJSON(oRecSet.GetAsXML())["OWTQ"];

            return Ok(request);
        }

        // GET: api/InventoryTransferRequest/WmsTransferLabels/5
        // TODO: Change THIS to /Detail For wms WEB Authorization
        [HttpGet("WmsTransferLabels/{id}/{doctype}")]
        //[Authorize]
        public async Task<IActionResult> GetDetailForWmsTransferLabels(int id, string doctype)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (!doctype.Equals("DocEntry") && !doctype.Equals("DocNum"))
            {
                return BadRequest(new { error = "Doc Type to Search Invalid" });
            }

            oRecSet.DoQuery(@"
                SELECT
                    ""DocEntry"",
                    ""DocNum"",
                    ""CANCELED"",
                    ""DocStatus"",
                    ""DocDate"",
                    ""Filler"",
                    ""ToWhsCode""
                From OWTQ WHERE """ + doctype + @""" = " + id);
            if (oRecSet.RecordCount == 0)
            {
                return NotFound("No Existe Documento");
            }

            JToken transfer = JToken.Parse("{}");
            transfer["OWTQ"] = context.XMLTOJSON(oRecSet.GetAsXML())["OWTQ"][0];
            int docentry = transfer["OWTQ"]["DocEntry"].ToObject<int>();
            int docnum = transfer["OWTQ"]["DocNum"].ToObject<int>();

            //oRecSet.DoQuery(@"
            //    SELECT
            //        *
            //    From  IBT1 Where ""WhsCode"" = 'S01' AND ""ItemCode"" = 'A0305243'");

            //transfer["IBT1"] = context.XMLTOJSON(oRecSet.GetAsXML())["IBT1"];

            // Inventory Transfer Request Rows
            //oRecSet.DoQuery(@"
            //    SELECT
            //        ""DocEntry"",
            //        ""LineNum"",
            //        ""LineStatus"",
            //        ""ItemCode"",
            //        ""Dscription"",
            //        ""Quantity"",
            //        ""OpenQty"",
            //        ""WhsCode"",
            //        ""UomCode""
            //    FROM WTQ1 WHERE ""DocEntry"" = " + docentry);
            oRecSet.DoQuery(@"
                SELECT
                    ""LineNum"",
                    ""LineStatus"",
                    ""ItemCode"",
                    ""Dscription"",
                    ""Quantity"",
                    ""UomCode""
                FROM WTQ1 WHERE ""DocEntry"" = " + docentry);
            transfer["WTQ1"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTQ1"];

            // Inventory Transfer
            oRecSet.DoQuery(@"
                SELECT
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocDate"",
                    ""DocDueDate"",
                    ""ToWhsCode"",
                    ""Filler""
                FROM OWTR 
                WHERE ""DocEntry"" in (SELECT ""DocEntry"" FROM WTR1 WHERE ""BaseEntry"" = " + docentry + ")");

            if (oRecSet.RecordCount != 0)
            {
                transfer["Transfers"] = context.XMLTOJSON(oRecSet.GetAsXML())["OWTR"];

                oRecSet.DoQuery(@"
                    SELECT
                        ""DocEntry"",
                        ""ItemCode"",
                        ""Dscription"",
                        ""Quantity"",
                        ""UomCode""
                    FROM WTR1
                    WHERE ""BaseEntry"" = " + docentry);
                transfer["TransfersDetail"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTR1"];

                string docEntrys = "";
                foreach (JToken transfers in transfer["Transfers"])
                {
                    docEntrys += transfers["DocEntry"] + ",";
                }
                docEntrys = docEntrys.Substring(0, docEntrys.Length - 1);
                oRecSet.DoQuery(@"
                    SELECT
                        ""ItemCode"",
                        ""BatchNum"",
                        ""LineNum"",
                        ""BaseEntry"",
                        ""Quantity""
                    From IBT1 WHERE ""BaseEntry"" in (" + docEntrys + @") AND ""WhsCode"" = '" + transfer["OWTQ"]["Filler"].ToString() + "'");
                transfer["Codes"] = context.XMLTOJSON(oRecSet.GetAsXML())["IBT1"];

                //foreach (JToken transferDetail in transfer["TransfersDetail"])
                //{
                //    oRecSet.DoQuery(@"SELECT * From IBT1 WHERE ""ItemCode"" = '" + transferDetail["ItemCode"] + @"' AND ""BaseEntry"" = " + transferDetail["DocEntry"] + @" AND ""WhsCode"" = '" + transferDetail["FromWhsCod"] + "'");
                //    transferDetail["Codes"] = context.XMLTOJSON(oRecSet.GetAsXML())["IBT1"];
                //}

                oRecSet.DoQuery(@"SELECT ""DocNum"", ""DocEntry"", ""DocDate"" From OWTQ WHERE ""U_SO1_02NUMRECEPCION"" = " + docnum);

                if (oRecSet.RecordCount != 0)
                {
                    transfer["Requests"] = context.XMLTOJSON(oRecSet.GetAsXML())["OWTQ"];

                    oRecSet.DoQuery(@"
                        SELECT
                            ""DocEntry"",
                            ""ItemCode"",
                            ""Quantity"",
                            ""UomCode""
                        FROM WTQ1 WHERE ""DocEntry"" in (SELECT ""DocEntry"" From OWTQ WHERE ""U_SO1_02NUMRECEPCION"" = " + docnum + ")");
                    transfer["RequestsDetail"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTQ1"];
                }
            }

            return Ok(transfer);
        }

        // GET: api/InventoryTransferRequest/Detail/5
        [HttpGet("Detail/{id}/{doctype}")]
        public async Task<IActionResult> GetDetail(int id, string doctype)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (!doctype.Equals("DocEntry") && !doctype.Equals("DocNum"))
            {
                return BadRequest(new { error = "Doc Type to Search Invalid" });
            }

            oRecSet.DoQuery(@"
                SELECT
                    ""DocEntry"",
                    ""DocNum"",
                    ""CANCELED"",
                    ""DocStatus"",
                    ""DocDate"",
                    ""Filler"",
                    ""ToWhsCode""
                From OWTQ WHERE """ + doctype + @""" = " + id);
            if (oRecSet.RecordCount == 0)
            {
                return NotFound("No Existe Documento");
            }

            JToken transfer = JToken.Parse("{}");
            transfer["OWTQ"] = context.XMLTOJSON(oRecSet.GetAsXML())["OWTQ"][0];
            int docentry = transfer["OWTQ"]["DocEntry"].ToObject<int>();
            int docnum = transfer["OWTQ"]["DocNum"].ToObject<int>();

            //oRecSet.DoQuery(@"
            //    SELECT
            //        *
            //    From  IBT1 Where ""WhsCode"" = 'S01' AND ""ItemCode"" = 'A0305243'");

            //transfer["IBT1"] = context.XMLTOJSON(oRecSet.GetAsXML())["IBT1"];

            // Inventory Transfer Request Rows
            //oRecSet.DoQuery(@"
            //    SELECT
            //        ""DocEntry"",
            //        ""LineNum"",
            //        ""LineStatus"",
            //        ""ItemCode"",
            //        ""Dscription"",
            //        ""Quantity"",
            //        ""OpenQty"",
            //        ""WhsCode"",
            //        ""UomCode""
            //    FROM WTQ1 WHERE ""DocEntry"" = " + docentry);
            oRecSet.DoQuery(@"
                SELECT
                    ""LineNum"",
                    ""LineStatus"",
                    ""ItemCode"",
                    ""Dscription"",
                    ""Quantity"",
                    ""UomCode""
                FROM WTQ1 WHERE ""DocEntry"" = " + docentry);
            transfer["WTQ1"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTQ1"];

            // Inventory Transfer
            oRecSet.DoQuery(@"
                SELECT
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocDate"",
                    ""DocDueDate"",
                    ""ToWhsCode"",
                    ""Filler""
                FROM OWTR 
                WHERE ""DocEntry"" in (SELECT ""DocEntry"" FROM WTR1 WHERE ""BaseEntry"" = " + docentry + ")");

            if (oRecSet.RecordCount != 0)
            {
                transfer["Transfers"] = context.XMLTOJSON(oRecSet.GetAsXML())["OWTR"];

                oRecSet.DoQuery(@"
                    SELECT
                        ""DocEntry"",
                        ""ItemCode"",
                        ""Dscription"",
                        ""Quantity"",
                        ""UomCode""
                    FROM WTR1
                    WHERE ""BaseEntry"" = " + docentry);
                transfer["TransfersDetail"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTR1"];

                string docEntrys = "";
                foreach (JToken transfers in transfer["Transfers"])
                {
                    docEntrys += transfers["DocEntry"] + ",";
                }
                docEntrys = docEntrys.Substring(0, docEntrys.Length - 1);
                oRecSet.DoQuery(@"
                    SELECT
                        ""ItemCode"",
                        ""BatchNum"",
                        ""LineNum"",
                        ""BaseEntry"",
                        ""Quantity""
                    From IBT1 WHERE ""BaseEntry"" in (" + docEntrys + @") AND ""WhsCode"" = '" + transfer["OWTQ"]["Filler"].ToString() + "'");
                transfer["Codes"] = context.XMLTOJSON(oRecSet.GetAsXML())["IBT1"];

                //foreach (JToken transferDetail in transfer["TransfersDetail"])
                //{
                //    oRecSet.DoQuery(@"SELECT * From IBT1 WHERE ""ItemCode"" = '" + transferDetail["ItemCode"] + @"' AND ""BaseEntry"" = " + transferDetail["DocEntry"] + @" AND ""WhsCode"" = '" + transferDetail["FromWhsCod"] + "'");
                //    transferDetail["Codes"] = context.XMLTOJSON(oRecSet.GetAsXML())["IBT1"];
                //}

                oRecSet.DoQuery(@"SELECT ""DocNum"", ""DocEntry"", ""DocDate"" From OWTQ WHERE ""U_SO1_02NUMRECEPCION"" = " + docnum);

                if (oRecSet.RecordCount != 0)
                {
                    transfer["Requests"] = context.XMLTOJSON(oRecSet.GetAsXML())["OWTQ"];

                    oRecSet.DoQuery(@"
                        SELECT
                            ""DocEntry"",
                            ""ItemCode"",
                            ""Quantity"",
                            ""UomCode""
                        FROM WTQ1 WHERE ""DocEntry"" in (SELECT ""DocEntry"" From OWTQ WHERE ""U_SO1_02NUMRECEPCION"" = " + docnum + ")");
                    transfer["RequestsDetail"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTQ1"];
                }
            }

            return Ok(transfer);
        }

        // POST: api/InventoryTransferRequest
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TransferRequest value)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.StockTransfer newRequest = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryTransferRequest);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                        Select
                            warehouse.""WhsCode"",
                            warehouse.""WhsName"",
                            serie.""Series""
                        From OWHS warehouse
                        LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                        Where serie.""ObjectCode"" = 1250000001 AND warehouse.""WhsCode"" = '" + value.towhs.whscode + "'");
            oRecSet.MoveFirst();
            JToken warehouseList = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"];

            int warehouseSerie = warehouseList[0]["Series"].ToObject<int>();

            newRequest.Series = warehouseSerie;
            newRequest.FromWarehouse = value.fromwhs.whscode;
            newRequest.ToWarehouse = value.towhs.whscode;

            for (int i = 0; i < value.rows.Count; i++)
            {
                newRequest.Lines.ItemCode = value.rows[i].code;


                if (value.rows[i].uom == -2)
                {
                    newRequest.Lines.UoMEntry = 185;
                    newRequest.Lines.UserFields.Fields.Item("U_CjsPsVr").Value = value.rows[i].quantity;
                    newRequest.Lines.Quantity = value.rows[i].quantity * value.rows[i].equivalentePV;
                    newRequest.Lines.UseBaseUnits = SAPbobsCOM.BoYesNoEnum.tYES;
                }
                else
                {
                    newRequest.Lines.Quantity = value.rows[i].quantity;
                    newRequest.Lines.UoMEntry = value.rows[i].uom;
                    newRequest.Lines.UseBaseUnits = (SAPbobsCOM.BoYesNoEnum)value.rows[i].uomBase;
                }

                newRequest.Lines.FromWarehouseCode = value.fromwhs.whscode;
                newRequest.Lines.WarehouseCode = value.towhs.whstsrcode;
                newRequest.Lines.Add();
            }

            int result = newRequest.Add();
            if (result != 0)
            {
                string error = context.oCompany.GetLastErrorDescription();
                return BadRequest(error);
            }
            return Ok(context.oCompany.GetNewObjectKey());
        }

        // PUT: api/InventoryTransferRequest/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateTransferRequest value)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.StockTransfer request = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryTransferRequest);

            if (request.GetByKey(id))
            {
                request.Lines.SetCurrentLine(0);
                string from = request.Lines.FromWarehouseCode;
                string to = request.Lines.WarehouseCode;
                request.Lines.Add();
                for (int i = 0; i < value.newProducts.Count; i++)
                {
                    request.Lines.ItemCode = value.newProducts[i].code;
                    request.Lines.Quantity = value.newProducts[i].quantity;
                    request.Lines.UoMEntry = value.newProducts[i].uom;
                    request.Lines.UseBaseUnits = (SAPbobsCOM.BoYesNoEnum)value.newProducts[i].uomBase;
                    request.Lines.FromWarehouseCode = from;
                    request.Lines.WarehouseCode = to;
                    request.Lines.Add();
                }

                for (int i = 0; i < value.ProductsChanged.Count; i++)
                {
                    request.Lines.SetCurrentLine(value.ProductsChanged[i].LineNum);
                    if (request.Lines.Quantity != value.ProductsChanged[i].quantity)
                    {
                        request.Lines.Quantity = value.ProductsChanged[i].quantity;
                    }

                    if (request.Lines.UoMEntry != value.ProductsChanged[i].uom)
                    {
                        request.Lines.UseBaseUnits = (SAPbobsCOM.BoYesNoEnum)value.ProductsChanged[i].uomBase;
                        request.Lines.UoMEntry = value.ProductsChanged[i].uom;
                    }
                }

                int result = request.Update();
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
