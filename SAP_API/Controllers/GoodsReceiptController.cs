using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.VariantTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;
using SAPbobsCOM;

namespace SAP_API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class GoodsReceiptController : ControllerBase
    {

        /// <summary>
        /// Get GoodsReceipt List to WMS web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>GoodsReceiptSearchResponse</returns>
        /// <response code="200">GoodsReceiptSearchResponse(SearchResponse)</response>
        // POST: api/GoodsReceipt/Search
        [ProducesResponseType(typeof(SearchResponse<GoodsReceiptSearchDetail>), StatusCodes.Status200OK)]
        [HttpPost("Search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(document.\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(warehouse.\"WhsName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty)
            {

                List<string> whereOR = new List<string>();
                if ("Abierto".Contains(request.columns[2].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"document.""DocStatus"" = 'O' ");
                }
                if ("Cerrado".Contains(request.columns[2].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"document.""DocStatus"" = 'C' ");
                }
                if ("Cancelado".Contains(request.columns[2].search.value, StringComparison.CurrentCultureIgnoreCase))
                {
                    whereOR.Add(@"document.""CANCELED"" = 'Y' ");
                }

                string whereORClause = "(" + String.Join(" OR ", whereOR) + ")";
                where.Add(whereORClause);
            }
            if (request.columns[3].search.value != String.Empty)
            {
                where.Add($"to_char(to_date(SUBSTRING(document.\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[3].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY document.\"DocNum\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY warehouse.\"WhsName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 2)
            {
                orderby = $" ORDER BY document.\"DocStatus\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 3)
            {
                orderby = $" ORDER BY document.\"DocDate\" {request.order[0].dir}";
            }
            else
            {
                orderby = $" ORDER BY document.\"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    document.""DocEntry"",
                    document.""DocNum"",
                    to_char(to_date(SUBSTRING(document.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when document.""CANCELED"" = 'Y' then 'Cancelado'
                    when document.""DocStatus"" = 'O' then 'Abierto'
                    when document.""DocStatus"" = 'C' then 'Cerrado'
                    else document.""DocStatus"" end)  AS  ""DocStatus"",


                    warehouse.""WhsName""
                From OIGN document
                LEFT JOIN NNM1 serie ON document.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode"" ";

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
            var goodsReceiptList = context.XMLTOJSON(oRecSet.GetAsXML())["OIGN"].ToObject<List<GoodsReceiptSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From OIGN document
                LEFT JOIN NNM1 serie ON document.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode"" ";

            if (where.Count != 0)
            {
                queryCount += "Where " + whereClause;
            }

            oRecSet.DoQuery(queryCount);
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OIGN"][0]["COUNT"].ToObject<int>();

            SearchResponse<GoodsReceiptSearchDetail> respose = new SearchResponse<GoodsReceiptSearchDetail>
            {
                data = goodsReceiptList,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            return Ok(respose);
        }

        // Class To Serialize GoodsReceipt Query Result 
        class GoodsReceiptDetailLine
        {
            public string ItemCode;
            public string Dscription;
            public double Quantity;
            public string UomCode;
            public string UomCode2;
            public double InvQty;
        }

        // Class To Serialize GoodsReceipt Query Result
        class GoodsReceiptDetail
        {
            public uint DocEntry;
            public uint DocNum;
            public string DocDate;
            public string DocStatus;
            public string WhsName;
            public List<GoodsReceiptDetailLine> Lines;
        };

        /// <summary>
        /// Get GoodsRecipt Detail to WMS GoodsRecipt Detail Page
        /// </summary>
        /// <param name="DocEntry">DocEntry. An Unsigned Integer that serve as Document identifier.</param>
        /// <returns>A GoodsReceipt Detail</returns>
        /// <response code="200">Returns GoodsReceipt Detail</response>
        /// <response code="204">No GoodsReceipt Found</response>
        // GET: api/GoodsReceipt/:DocEntry
        [ProducesResponseType(typeof(GoodsReceiptDetail), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("{DocEntry}")]
        public async Task<IActionResult> Get(uint DocEntry)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery($@"
                SELECT
                    document.""DocEntry"",
                    document.""DocNum"",
                    to_char(to_date(SUBSTRING(document.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when document.""CANCELED"" = 'Y' then 'Cancelado'
                    when document.""DocStatus"" = 'O' then 'Abierto'
                    when document.""DocStatus"" = 'C' then 'Cerrado'
                    else document.""DocStatus"" end)  AS  ""DocStatus"",

                    warehouse.""WhsName""
                FROM OIGN document
                LEFT JOIN NNM1 series ON series.""Series"" = document.""Series""
                LEFT JOIN OWHS warehouse ON warehouse.""WhsCode"" = series.""SeriesName""
                WHERE document.""DocEntry"" = '{DocEntry}';");

            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NoContent();
            }

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["OIGN"][0];

            oRecSet.DoQuery($@"
                Select
                    ""ItemCode"",
                    ""Dscription"",
                    ""Quantity"",
                    ""UomCode"",
                    ""InvQty"",
                    ""UomCode2""
                From IGN1 Where ""DocEntry"" = '{DocEntry}';");
            oRecSet.MoveFirst();
            temp["Lines"] = context.XMLTOJSON(oRecSet.GetAsXML())["IGN1"];

            GoodsReceiptDetail output = temp.ToObject<GoodsReceiptDetail>();

            //Force Garbage Collector. Recommendation by InterLatin Dude. SDK Problem with memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            //var s1 = Stopwatch.StartNew();
            //s1.Stop();
            //const int _max = 1000000;
            //Console.WriteLine(((double)(s1.Elapsed.TotalMilliseconds * 1000 * 1000) / _max).ToString("0.00 ns"));

            return Ok(output);
        }

        public class GoodReciept
        {
            public string serie;
            public List<GoodRecieptRows> Rows;

        }

        public class GoodRecieptRows
        {
            public string ItemCode;
            public double quantity;
            public string whsCode;
            public double cost;
        }



        [HttpPost]
        public async Task<IActionResult> PostGoodsReciept([FromBody] GoodReciept value)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            Documents goods = (Documents)context.oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);
            oRecSet.DoQuery($@"
                Select
                    serie1.""SeriesName"",
                    serie1.""Series"",
                    serie1.""ObjectCode""
                From NNM1 serie1
                Where serie1.""ObjectCode"" = 59 AND serie1.""SeriesName"" = '{value.serie}';");

            int Serie = (int)oRecSet.Fields.Item("Series").Value;

            if (oRecSet.RecordCount == 0)
            {
                return BadRequest("Error En Sucursal.");
            }

            goods.TaxDate = DateTime.Now;
            goods.Series = Serie;

            oRecSet.DoQuery($@"
                Select
                    s.""U_D1""
                From ""@IL_CECOS"" s
                Where s.""Code"" = '{value.serie}';");

            oRecSet.MoveFirst();

            if (oRecSet.RecordCount == 0)
            {
                return BadRequest("Cuenta de Centros de Costo No Encontrada Para Ese Almacen.");
            }


            string cuenta = (string)oRecSet.Fields.Item("U_D1").Value;

            for (int i = 0; i < value.Rows.Count; i++)
            {
                goods.Lines.ItemCode = value.Rows[i].ItemCode;
                goods.Lines.Quantity = value.Rows[i].quantity;
                //goods.Lines.UnitPrice = value.Rows[i].StockPrice;
                goods.Lines.UnitPrice = value.Rows[i].cost;
                goods.Lines.WarehouseCode = value.Rows[i].whsCode;
                goods.Lines.CostingCode = cuenta;
                oRecSet.DoQuery(@"
                    Select T2.""DfltProfit"",
                            T2.""WhsCode"",
                            T0.""ItemCode""
                    From OITM T0 INNER JOIN OITB T1 ON T0.""ItmsGrpCod"" = T1.""ItmsGrpCod""
                    INNER JOIN OGAR T2 ON T1.""ItmsGrpCod"" = T2.""ItmsGrpCod"" 
                    Where T2.""WhsCode"" = '!^|' AND T0.""ItemCode"" = '" + value.Rows[i].ItemCode + "'");
                oRecSet.MoveFirst();
                if (oRecSet.RecordCount == 0)
                {
                    return BadRequest("Uno o varios productos no existen en SAP V10.");
                }
                JToken accCode = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];

                if (accCode["DfltProfit"].Equals(null))
                {
                    JToken accCode1 = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][1];
                    goods.Lines.AccountCode = (string)accCode1["DfltProfit"];
                }
                else
                {
                    goods.Lines.AccountCode = (string)accCode["DfltProfit"];
                }


                oRecSet.DoQuery(@"
                    Select ""ItemCode"", 
                           ""ManBtchNum"",
                           ""ItmsGrpCod""
                    From OITM Where ""ItemCode"" = '" + value.Rows[i].ItemCode + "'");
                oRecSet.MoveFirst();
                if (oRecSet.RecordCount == 0)
                {
                    return BadRequest("Uno o varios productos no existen en SAP V10.");
                }
                JToken product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];

                String lote = product["ManBtchNum"].ToObject<String>();


                if (lote.Equals("Y"))
                {
                    goods.Lines.BatchNumbers.BatchNumber = "SI";
                    goods.Lines.BatchNumbers.Quantity = value.Rows[i].quantity;
                    goods.Lines.BatchNumbers.Add();
                }

                //int Serie = (int)oRecSet.Fields.Item("Series").Value;
                //string accCode = (string)oRecSet.Fields.Item("DfltProfit

                goods.Lines.Add();
            }




            if (goods.Add() == 0)
            {
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                return Ok(new { value = context.oCompany.GetNewObjectKey() });
            }    
            else
            {
                string error = context.oCompany.GetLastErrorDescription();
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return BadRequest(new { error });
            }

        }

        public class GoodRecieptAut
        {
            public string Filler;
            public int DocEntry;
            public int DocNum;
            public string ToWhsCode;
            public string U_SO1_01SUCURSAL;
            public List<GoodRecieptRowsAut> Rows;
        }

        public class GoodRecieptRowsAut
        {
            public string ItemCode;
            public double Quantity;
            public string StockPrice;
            public string WhsCode;
            public string FromWhsCod;
            public double InvQty;
        }

        [HttpPost("Replica")]
        public async Task<IActionResult> PostGoodsRecieptAut([FromBody] GoodRecieptAut value)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            Documents goods = (Documents)context.oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);

            oRecSet.DoQuery($@"
                Select
                    s.""U_SO1_02NUMRECEPCION""
                From OIGN s
                Where s.""U_SO1_02NUMRECEPCION"" = '{value.DocNum}';");

            if (oRecSet.RecordCount != 0)
            {
                return Ok();
            }

            oRecSet.DoQuery($@"
                Select
                    serie1.""SeriesName"",
                    serie1.""Series"",
                    serie1.""ObjectCode""
                From NNM1 serie1
                Where serie1.""ObjectCode"" = 59 AND serie1.""SeriesName"" = '{value.Filler.Substring(0,3)}';");

            int Serie = (int)oRecSet.Fields.Item("Series").Value;

            if (oRecSet.RecordCount == 0)
            {
                return BadRequest("Error En Sucursal.");
            }

            goods.TaxDate = DateTime.Now;
            goods.Series = Serie;
            goods.UserFields.Fields.Item("U_SO1_02NUMRECEPCION").Value = value.DocNum.ToString();

            oRecSet.DoQuery($@"
                Select
                    s.""U_D1""
                From ""@IL_CECOS"" s
                Where s.""Code"" = '{value.Filler.Substring(0, 3)}';");

            oRecSet.MoveFirst();

            if (oRecSet.RecordCount == 0)
            {
                return BadRequest("Cuenta de Centros de Costo No Encontrada Para Ese Almacen.");
            }


            string cuenta = (string)oRecSet.Fields.Item("U_D1").Value;

            for (int i = 0; i < value.Rows.Count; i++)
            {
                goods.Lines.ItemCode = value.Rows[i].ItemCode;
                goods.Lines.Quantity = value.Rows[i].InvQty;
                //goods.Lines.UnitPrice = value.Rows[i].StockPrice;
                goods.Lines.UnitPrice = double.Parse(value.Rows[i].StockPrice);
                goods.Lines.WarehouseCode = value.Rows[i].FromWhsCod.Substring(0, 3);
                goods.Lines.CostingCode = cuenta;
                oRecSet.DoQuery(@"
                    Select T2.""DfltProfit"",
                            T2.""WhsCode"",
                            T0.""ItemCode""
                    From OITM T0 INNER JOIN OITB T1 ON T0.""ItmsGrpCod"" = T1.""ItmsGrpCod""
                    INNER JOIN OGAR T2 ON T1.""ItmsGrpCod"" = T2.""ItmsGrpCod"" 
                    Where T2.""WhsCode"" = '!^|' AND T0.""ItemCode"" = '" + value.Rows[i].ItemCode + "'");
                oRecSet.MoveFirst();
                if (oRecSet.RecordCount == 0)
                {
                    return BadRequest("Uno o varios productos no existen en SAP V10.");
                }
                JToken accCode = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];

                if (accCode["DfltProfit"].Equals(null))
                {
                    JToken accCode1 = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][1];
                    goods.Lines.AccountCode = (string)accCode1["DfltProfit"];
                }
                else
                {
                    goods.Lines.AccountCode = (string)accCode["DfltProfit"];
                }


                oRecSet.DoQuery(@"
                    Select ""ItemCode"", 
                           ""ManBtchNum"",
                           ""ItmsGrpCod""
                    From OITM Where ""ItemCode"" = '" + value.Rows[i].ItemCode + "'");
                oRecSet.MoveFirst();
                if (oRecSet.RecordCount == 0)
                {
                    return BadRequest("Uno o varios productos no existen en SAP V10.");
                }
                JToken product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];

                String lote = product["ManBtchNum"].ToObject<String>();


                if (lote.Equals("Y"))
                {
                    goods.Lines.BatchNumbers.BatchNumber = "SI";
                    goods.Lines.BatchNumbers.Quantity = value.Rows[i].InvQty;
                    goods.Lines.BatchNumbers.Add();
                }

                //int Serie = (int)oRecSet.Fields.Item("Series").Value;
                //string accCode = (string)oRecSet.Fields.Item("DfltProfit

                goods.Lines.Add();
            }




            if (goods.Add() == 0)
            {
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                return Ok(new { value = context.oCompany.GetNewObjectKey() });
            }
            else
            {
                string error = context.oCompany.GetLastErrorDescription();
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return BadRequest(new { error });
            }

        }

    }
}
