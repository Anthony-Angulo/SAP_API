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
    public class ProductsController : ControllerBase
    {
        public class ProductSearchDetail : SearchDetail {
            public string ItemName { get; set; }
            public string ItemCode { get; set; }
        }

        public class ProductSearchResponse : SearchResponse<ProductSearchDetail> { }

        public class ProductWithStockSearchDetail : SearchDetail {
            public string ItemName { get; set; }
            public string ItemCode { get; set; }
            public double OnHand { get; set; }
        }

        public class ProductWithStockSearchResponse : SearchResponse<ProductWithStockSearchDetail> { }

        [HttpPost("Search/ToSell")]
        public async Task<IActionResult> GetSearchToSell([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            
            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY \"ItemCode\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY \"ItemName\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY \"ItemCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ""ItemName"",
                    ""ItemCode""
                From OITM
                Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y'";

            if (where.Count != 0) {
                query += " AND " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"].ToObject<List<ProductSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From OITM
                Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y' ";

            if (where.Count != 0) {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

            var respose = new ProductSearchResponse
            {
                Data = orders,
                Draw = request.Draw,
                RecordsFiltered = COUNT,
                RecordsTotal = COUNT,
            };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(respose);
        }

        [HttpPost("Search/ToSellWithStock/{warehouse}")]
        public async Task<IActionResult> GetSearchToSellWithStock(string warehouse, [FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(item.\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(item.\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(stock.\"OnHand\") Like LOWER('%{request.columns[2].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY item.\"ItemCode\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY item.\"ItemName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY stock.\"OnHand\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY item.\"ItemCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    item.""ItemName"",
                    item.""ItemCode"",
                    stock.""OnHand""
                From OITM item
                JOIN OITW stock ON item.""ItemCode"" = stock.""ItemCode""
                Where ""WhsCode"" = '" + warehouse + @"'
                AND ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y'";

            if (where.Count != 0) {
                query += " AND " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"].ToObject<List<ProductWithStockSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From OITM item
                JOIN OITW stock ON item.""ItemCode"" = stock.""ItemCode""
                Where ""WhsCode"" = '" + warehouse + @"'
                AND ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y' ";

            if (where.Count != 0) {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

            var respose = new ProductWithStockSearchResponse
            {
                Data = orders,
                Draw = request.Draw,
                RecordsFiltered = COUNT,
                RecordsTotal = COUNT,
            };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(respose);
        }

        // GET: api/Products/ProvidersProducts
        [HttpPost("Search/ToBuy")]
        public async Task<IActionResult> GetSearchToBuy([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY \"ItemCode\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY \"ItemName\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY \"ItemCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ""ItemName"",
                    ""ItemCode""
                From OITM
                Where ""PrchseItem"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y' AND ""ItemCode"" LIKE 'G%' ";

            if (where.Count != 0)
            {
                query += " AND " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"].ToObject<List<ProductSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From OITM
                Where ""PrchseItem"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y' AND ""ItemCode"" LIKE 'G%' ";

            if (where.Count != 0)
            {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

            var respose = new ProductSearchResponse
            {
                Data = orders,
                Draw = request.Draw,
                RecordsFiltered = COUNT,
                RecordsTotal = COUNT,
            };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(respose);
        }

        // GET: api/Products/CRMToSell/5
        [HttpGet("CRMToSell/{id}/{priceList}/{warehouse}")]
        public async Task<IActionResult> GetCRMToSell(string id, int priceList, string warehouse) {

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
                Select 
                    product.""ItemName"", 
                    product.""ItemCode"", 
                    RTRIM(RTRIM(product.""U_IL_PesProm"", '0'), '.') AS ""U_IL_PesProm"",
                    product.""SUoMEntry"",
                    priceList.""PriceList"",
                    priceList.""Currency"",
                    RTRIM(RTRIM(priceList.""Price"", '0'), '.') AS ""Price"",
                    priceList.""UomEntry"",
                    priceList.""PriceType"",
                    stock.""WhsCode"",
                    RTRIM(RTRIM(stock.""OnHand"", '0'), '.') AS ""OnHand""
                From OITM product
                JOIN ITM1 priceList ON priceList.""ItemCode"" = product.""ItemCode""
                JOIN OITW stock ON stock.""ItemCode"" = product.""ItemCode""
                Where product.""ItemCode"" = '" + id + @"'
                AND stock.""WhsCode"" = '" + warehouse + @"'
                AND priceList.""PriceList"" = " + priceList);
            oRecSet.MoveFirst();
            JToken product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            oRecSet.DoQuery(@"
                Select 
                    header.""UgpCode"",
                    header.""BaseUom"",
                    baseUOM.""UomCode"" as ""BaseCode"",
                    detail.""UomEntry"",
                    UOM.""UomCode"",
                    RTRIM(RTRIM(detail.""BaseQty"", '0'), '.') AS ""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpCode"" = '" + id + @"'");
                //AND detail.""UomEntry"" in (Select ""UomEntry"" FROM ITM4 Where ""UomType"" = 'S' AND ""ItemCode"" = '" + id + "')");
            oRecSet.MoveFirst();
            product["uom"] = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(product);
        }


        // GET: api/Products/CRMToSellEdit/5
        [HttpGet("CRMToSellEdit/{id}")]
        public async Task<IActionResult> GetCRMToSellEdit(string id) {

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
                Select 
                    product.""ItemName"", 
                    product.""ItemCode"", 
                    RTRIM(RTRIM(product.""U_IL_PesProm"", '0'), '.') AS ""U_IL_PesProm"",
                    product.""SUoMEntry""
                From OITM product
                Where product.""ItemCode"" = '" + id + @"'");
            oRecSet.MoveFirst();
            JToken product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            oRecSet.DoQuery(@"
                Select 
                    header.""UgpCode"",
                    header.""BaseUom"",
                    baseUOM.""UomCode"" as ""BaseCode"",
                    detail.""UomEntry"",
                    UOM.""UomCode"",
                    RTRIM(RTRIM(detail.""BaseQty"", '0'), '.') AS ""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpCode"" = '" + id + @"'");
            //AND detail.""UomEntry"" in (Select ""UomEntry"" FROM ITM4 Where ""UomType"" = 'S' AND ""ItemCode"" = '" + id + "')");
            oRecSet.MoveFirst();
            product["uom"] = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(product);
        }


        // GET: api/Products/CRMToBuy/5
        [HttpGet("CRMToBuy/{id}")]
        public async Task<IActionResult> GetCRMToBuy(string id) {

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
                Select 
                    ""ItemName"", 
                    ""ItemCode""
                From OITM
                Where ""ItemCode"" = '" + id + @"'");
            oRecSet.MoveFirst();
            JToken product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(product);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            
            SAPbobsCOM.IItems items = (SAPbobsCOM.IItems)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems);
            if (items.GetByKey(id)) {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                return Ok(temp);
            }
            return NotFound("No Existe Producto");
        }

        // GET: api/Products/UomDetailWithLastSellPrice
        [HttpGet("UomDetailWithLastSellPrice/{id}")]
        public async Task<IActionResult> GetUomDetail(string id) {

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
                Select
                    product.""ItemCode"",
                    product.""ItemName"",
                    product.""NumInSale"",
                    product.""SUoMEntry"",
                    product.""IUoMEntry"",
                    product.""U_IL_PesProm"",
                    RTRIM(RTRIM(priceList.""Price"", '0'), '.') AS ""Price"",
                    priceList.""Currency""
                From OITM product
                JOIN ITM1 priceList ON priceList.""ItemCode"" = product.""ItemCode""
                Where product.""ItemCode"" = '" + id + @"'
                AND priceList.""PriceList"" = 11");
            oRecSet.MoveFirst();
            JToken product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(product);
        }

        // GET: api/Products/Uoms
        [HttpGet("Uoms")]
        public async Task<IActionResult> GetUoms() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"UomEntry\", \"UomCode\" From OUOM");
            oRecSet.MoveFirst();
            JToken uoms = context.XMLTOJSON(oRecSet.GetAsXML())["OUOM"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(uoms);
        }

        // GET: api/Products/CRMList
        [HttpGet("CRMList")]
        public async Task<IActionResult> GetCRMList()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"Select ""ItemCode"", ""ItemName"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y';");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(products);
        }

        // GET: api/Products/ProvidersProducts
        [HttpGet("ProvidersProducts")]
        public async Task<IActionResult> GetProvidersProducts()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"Select ""ItemCode"", ""ItemName"" From OITM Where ""PrchseItem"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y' AND ""ItemCode"" LIKE 'G%';");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(products);
        }

        // GET: api/Products/CRMList/Stocks
        [HttpGet("CRMList/Stocks")]
        public async Task<IActionResult> GetCRMListStocks()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"Select ""ItemCode"", ""ItemName"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y';");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];

            oRecSet.DoQuery(@"
                Select 
                    ""ItemCode"",
                    ""WhsCode"",
                    RTRIM(RTRIM(""OnHand"", '0'), '.') AS ""OnHand""
                From OITW
                Where ""OnHand"" != 0
                AND ""Freezed"" = 'N'
                AND ""Locked"" = 'N'
                AND ""WhsCode"" in ('S01', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S55')
                AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];

            ////Task<List<List<object>>> pro = comp(products, 0);
            ////Task<List<List<object>>> sto = comp(stock, 1);
            //Task<List<List<object>>> pro = Task.Run(() =>
            //{
            //    oRecSet.DoQuery("Select \"ItemCode\", \"ItemName\" From OITM Where \"SellItem\" = 'Y' AND \"QryGroup3\" = 'Y' AND \"Canceled\" = 'N'");
            //    oRecSet.MoveFirst();
            //    JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            //    return context.comp(products, 0);
            //});

            //Task<List<List<object>>> sto = Task.Run(() =>
            //{
            //    oRecSet2.DoQuery(@"
            //    Select 
            //        ""ItemCode"",
            //        ""WhsCode"",
            //        ""OnHand""
            //    From OITW
            //    Where ""Freezed"" = 'N'
            //        AND ""Locked"" = 'N'
            //        AND ""WhsCode"" in ('S01', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S55')
            //        AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N')");
            //    oRecSet2.MoveFirst();
            //    JToken stock = context.XMLTOJSON(oRecSet2.GetAsXML())["OITW"];
            //    return context.comp(stock, 1);
            //});

            ////List<Dictionary<string, object>> collection = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(products.ToString());
            ////List<Dictionary<string, object>> collection2 = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(stock.ToString());
            ////vaaa["products"] = JObject.Parse("{products:" + JsonConvert.SerializeObject(JSONH.pack(collection)) + "}")["products"];
            ////vaaa["stock"] = JObject.Parse("{stocks:" + JsonConvert.SerializeObject(JSONH.pack(collection2, 1)) + "}")["stocks"];
            //await Task.WhenAll(pro, sto);
            //return Ok(new { products = pro.Result, stock = sto.Result });
            return Ok(new { products , stock });
        }

        // GET: api/Products/TranferList
        [HttpGet("TransferList")]
        public async Task<IActionResult> GetTransferList()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"ItemCode\", \"ItemName\" From OITM Where \"Canceled\" = 'N'  AND \"validFor\" = 'Y'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(products);
        }

        // GET: api/Products/Stock
        [HttpGet("Stock/{sucursal}")]
        public async Task<IActionResult> GetStock(string sucursal)
        {
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
                Select 
                    ""ItemCode"",
                    ""WhsCode"",
                    RTRIM(RTRIM(""OnHand"", '0'), '.') AS ""OnHand""
                From OITW
                Where ""Freezed"" = 'N'
                AND ""Locked"" = 'N'
                AND ""WhsCode"" = '" + sucursal + @"'
                AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(stock);
        }

        // GET: api/Products/APPCRM
        [HttpGet("APPCRM")]
        public async Task<IActionResult> GetCRMS()
        {
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
                Select 
                    ""ItemName"",
                    ""ItemCode"",
                    RTRIM(RTRIM(""U_IL_PesProm"", '0'), '.') AS ""U_IL_PesProm""
                From OITM
                Where ""SellItem"" = 'Y'
                AND ""QryGroup3"" = 'Y'
                AND ""Canceled"" = 'N'
                AND ""validFor"" = 'Y'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            oRecSet.DoQuery(@"
                Select 
                    ""PriceList"",
                    ""ItemCode"",
                    ""Currency"",
                    RTRIM(RTRIM(""Price"", '0'), '.') AS ""Price"",
                    ""UomEntry""
                From ITM1
                Where ""PriceList"" = '2'
                AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken priceList = context.XMLTOJSON(oRecSet.GetAsXML())["ITM1"];
            oRecSet.DoQuery(@"
                Select 
                    ""ItemCode"",
                    ""WhsCode"",
                    RTRIM(RTRIM(""OnHand"", '0'), '.') AS ""OnHand""
                From OITW
                Where ""Freezed"" = 'N'
                    AND ""Locked"" = 'N'
                    AND ""WhsCode"" in ('S01', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S55')
                    AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];
            oRecSet.DoQuery(@"
                Select 
                    header.""UgpCode"",
                    header.""BaseUom"",
                    baseUOM.""UomCode"" as baseUOM,
                    detail.""UomEntry"",
                    UOM.""UomCode"",
                    RTRIM(RTRIM(detail.""BaseQty"", '0'), '.') AS ""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { products, priceList, stock, uom });
        }

        // CON NUEVA VERSION APP NO PUBLICADA
        // GET: api/Products/APPCRM/200
        [HttpGet("APPCRM/{id}")]
        public async Task<IActionResult> GetCRMSContact(int id)
        {
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
                Select 
                    ""ItemName"",
                    ""ItemCode"",
                    RTRIM(RTRIM(""U_IL_PesProm"", '0'), '.') AS ""U_IL_PesProm""
                From OITM
                Where ""SellItem"" = 'Y'
                AND ""QryGroup3"" = 'Y'
                AND ""Canceled"" = 'N'
                AND ""validFor"" = 'Y'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            oRecSet.DoQuery(@"
                Select 
                    ""PriceList"",
                    ""ItemCode"",
                    ""Currency"",
                    RTRIM(RTRIM(""Price"", '0'), '.') AS ""Price"",
                    ""UomEntry""
                From ITM1
                Where ""Currency"" is not NULL
                AND ""Price"" != 0
                AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')
                AND ""PriceList"" in (SELECT Distinct ""ListNum"" From OCRD Where ""CardType"" = 'C' AND ""SlpCode"" = " + id + @" AND ""CardCode"" LIKE '%-P');");
            oRecSet.MoveFirst();
            JToken priceList = context.XMLTOJSON(oRecSet.GetAsXML())["ITM1"];


            oRecSet.DoQuery($"Select \"Fax\" From OSLP Where \"SlpCode\" = {id}");
            oRecSet.MoveFirst();
            string warehouses = context.XMLTOJSON(oRecSet.GetAsXML())["OSLP"][0]["Fax"].ToString();
            warehouses = warehouses.Trim();
            if (warehouses.Equals(""))
            {
                warehouses = "'S01', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S55'";
            }
            else
            {
                warehouses = warehouses.ToUpper();
                warehouses = "'" + warehouses + "'";
                warehouses = warehouses.Replace(",", "','");
            }

            oRecSet.DoQuery(@"
                Select 
                    ""ItemCode"",
                    ""WhsCode"",
                    RTRIM(RTRIM(""OnHand"", '0'), '.') AS ""OnHand""
                From OITW
                Where ""OnHand"" != 0 
                AND ""Freezed"" = 'N'
                AND ""Locked"" = 'N'
                AND ""WhsCode"" in (" + warehouses + @")
                AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];
            
            oRecSet.DoQuery(@"
                Select 
                    header.""UgpCode"",
                    header.""BaseUom"",
                    baseUOM.""UomCode"" as baseUOM,
                    detail.""UomEntry"",
                    UOM.""UomCode"",
                    RTRIM(RTRIM(detail.""BaseQty"", '0'), '.') AS ""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { products, priceList, stock, uom });
        }

        //// COMP
        //// GET: api/Products/APPCRM
        //[HttpGet("APPCRM")]
        //public async Task<IActionResult> GetCRMS()
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
        //    SAPbobsCOM.Recordset oRecSet1 = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
        //    SAPbobsCOM.Recordset oRecSet2 = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
        //    SAPbobsCOM.Recordset oRecSet3 = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
        //    Task<List<List<object>>> pro = Task.Run(() =>
        //    {
        //        oRecSet.DoQuery("Select \"ItemName\", \"ItemCode\", \"U_IL_PesProm\" From OITM Where \"SellItem\" = 'Y' AND \"QryGroup3\" = 'Y' AND \"Canceled\" = 'N'");
        //        oRecSet.MoveFirst();
        //        JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
        //        return context.comp(products, 2);
        //    });

        //    Task<List<List<object>>> pl = Task.Run(() =>
        //    {
        //        oRecSet1.DoQuery(@"
        //            Select 
        //                ""PriceList"",
        //                ""ItemCode"",
        //                ""Currency"",
        //                ""Price"",
        //                ""UomEntry""
        //            From ITM1
        //            Where ""PriceList"" = '2'
        //                AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N')");
        //        oRecSet1.MoveFirst();
        //        JToken priceList = context.XMLTOJSON(oRecSet1.GetAsXML())["ITM1"];
        //        return context.comp(priceList, 2);
        //    });

        //    Task<List<List<object>>> sto = Task.Run(() =>
        //    {
        //        oRecSet2.DoQuery(@"
        //            Select 
        //                ""ItemCode"",
        //                ""WhsCode"",
        //                ""OnHand""
        //            From OITW
        //            Where ""Freezed"" = 'N'
        //                AND ""Locked"" = 'N'
        //                AND ""WhsCode"" in ('S01', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S55')
        //                AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N')");
        //        oRecSet2.MoveFirst();
        //        JToken stock = context.XMLTOJSON(oRecSet2.GetAsXML())["OITW"];
        //        return context.comp(stock, 1);
        //    });

        //    Task<List<List<object>>> um = Task.Run(() =>
        //    {
        //        oRecSet3.DoQuery(@"
        //            Select 
        //                header.""UgpCode"",
        //                header.""BaseUom"",
        //                baseUOM.""UomCode"" as baseUOM,
        //                detail.""UomEntry"",
        //                UOM.""UomCode"",
        //                detail.""BaseQty""
        //            From OUGP header
        //            JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
        //            JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
        //            JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
        //            Where header.""UgpCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N')");
        //        oRecSet3.MoveFirst();
        //        JToken uom = context.XMLTOJSON(oRecSet3.GetAsXML())["OUGP"];
        //        return context.comp(uom, 2);
        //    });

        //    GC.Collect();
        //    GC.WaitForPendingFinalizers();
        //    await Task.WhenAll(pro, pl, sto, um);
        //    return Ok(new { products = pro.Result, priceList = pl.Result, stock = sto.Result, uom = um.Result });
        //}

        // GET: api/Products/CRM/5
        [HttpGet("CRM/{id}")]
        public async Task<IActionResult> GetCRM(string id)
        {
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
                Select 
                    ""ItemName"", 
                    ""ItemCode"", 
                    ""ItmsGrpCod"",
                    RTRIM(RTRIM(""U_IL_PesProm"", '0'), '.') AS ""U_IL_PesProm"",
                    ""SUoMEntry""
                From OITM where ""ItemCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            oRecSet.DoQuery(@"
                Select
                    ""PriceList"",
                    ""ItemCode"",
                    ""Currency"",
                    RTRIM(RTRIM(""Price"", '0'), '.') AS ""Price"",
                    ""UomEntry"",
                    ""PriceType""
                From ITM1 Where ""ItemCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken priceList = context.XMLTOJSON(oRecSet.GetAsXML())["ITM1"];
            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""WhsCode"",
                    RTRIM(RTRIM(""OnHand"", '0'), '.') AS ""OnHand""
                From OITW
                WHERE ""WhsCode"" in ('S01', 'S06', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S55')
                AND ""ItemCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];
            oRecSet.DoQuery(@"
                Select 
                    header.""UgpCode"",
                    header.""BaseUom"",
                    baseUOM.""UomCode"" as baseUOM,
                    detail.""UomEntry"",
                    UOM.""UomCode"",
                    RTRIM(RTRIM(detail.""BaseQty"", '0'), '.') AS ""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { products, priceList, stock, uom });
        }

        // GET: api/Products/CRM/5
        [HttpGet("UM/{id}")]
        public async Task<IActionResult> GetUM(string id)
        {
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
                Select 
                    header.""UgpCode"",
                    header.""BaseUom"",
                    dd.""ItemName"",
                    baseUOM.""UomCode"" as baseUOM
                From OUGP header
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OITM dd ON dd.""ItemCode"" = header.""UgpCode""
                Where header.""UgpCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"][0];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(uom);
        }

        // GET: api/Products/WMSReport
        [HttpGet("WMSReport/{sucursal}/{group}")]
        public async Task<IActionResult> GetWMSReport(string sucursal, string group)
        {
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
                SELECT
                    T0.""ItemCode"",
                    T1.""ItemName"",
                    T0.""OnHand"" as ""StockBase"",

                    (case when T1.""InvntryUom"" = 'H87' then 'PZ'
                    when T1.""InvntryUom"" = 'XPK' then 'PQ'
                    when T1.""InvntryUom"" = 'XSA' then 'SC'
                    when T1.""InvntryUom"" = 'H87' then 'PZ'
                    when T1.""InvntryUom"" = 'KGM' then 'KG'
                    when T1.""InvntryUom"" = 'XBX' then 'CJ'
                    when T1.""InvntryUom"" = 'XBJ' then 'CB'
                    when T1.""InvntryUom"" = 'BLL' then 'GALON'
                    else T1.""InvntryUom"" end)  AS  ""UOMBase"",

                    (T0.""OnHand""/
                    case 
                    when (T1.""QryGroup5""  = 'Y') then T1.""NumInSale"" 
                    when (T1.""QryGroup6""  = 'Y' and T1.""U_IL_PesProm"" <> 0 ) then  T1.""U_IL_PesProm"" 
                    when (T1.""QryGroup6""  = 'Y' and T1.""U_IL_PesProm""= 0 ) then T1.""NumInSale"" 
                    when (T1.""QryGroup6""  = 'Y') then T1.""NumInSale"" 
                    when (T1.""InvntryUom"" = 'XPK') then T1.""NumInSale"" 
                    when T1.""U_IL_PesProm""= 0 then 1 
                    else T1.""U_IL_PesProm"" end) as ""Stock"",

                    (case when (T1.""QryGroup5""  = 'Y') then T2.""UomCode""
                    when (T1.""QryGroup6"" = 'Y' and T1.""InvntryUom"" = 'H87') then 'Cajas'
                    when (T1.""QryGroup6"" = 'Y' and T1.""InvntryUom"" = 'KGM') then 'Cajas'
                    when (T1.""QryGroup6""  = 'Y') then T2.""UomCode""
                    when (T1.""QryGroup7""  = 'Y' and T1.""U_IL_PesProm""= 0) then 'KGM'                
                    when (T1.""QryGroup7""  = 'Y' and ""U_IL_PesProm""= 0) then 'Cajas'
                    when (T1.""QryGroup7""  = 'Y' and T1.""InvntryUom"" = 'H87') then 'KGM' 
                    when (T1.""QryGroup7""  = 'Y' and T1.""InvntryUom"" = 'XPK') then T2.""UomCode"" 
                    else 'Cajas' end) AS ""UOM""

                FROM OITW T0 
                INNER JOIN OITM T1 ON T0.""ItemCode"" = T1.""ItemCode"" 
                INNER JOIN OUOM T2 ON T1.""PUoMEntry"" = T2.""UomEntry"" 
                INNER JOIN OUGP T3 ON T1.""UgpEntry"" = T3.""UgpEntry"" 
                INNER JOIN UGP1 T4 ON T3.""UgpEntry"" = T4.""UgpEntry"" AND T1.""PUoMEntry"" = T4.""UomEntry""
                INNER JOIN OUOM T5 ON T1.""PUoMEntry"" = T5.""UomEntry""

                WHERE T0.""WhsCode"" = '" + sucursal + @"' 
                AND T1.""QryGroup" + group + @""" = 'Y' 
                AND T0.""OnHand"" > 0 
                ORDER BY T1.""ItemName"";");

            //(
            //    Select
            //        product.""ItemCode"",
            //        product.""ItemName"",
            //        stock.""OnHand"",
            //        baseUOM.""UomCode"" as base,
            //        stock.""OnHand"" / detail.""BaseQty"" as stock,
            //        UOM.""UomCode""
            //    From OITM product
            //    JOIN OITW stock ON stock.""ItemCode"" = product.""ItemCode""
            //    LEFT JOIN OUGP header ON header.""UgpCode"" = product.""ItemCode""
            //    LEFT JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
            //    LEFT JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
            //    LEFT JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry"" AND UOM.""UomEntry"" != baseUOM.""UomEntry""
            //    Where product.""QryGroup" + group + @""" = 'Y' AND stock.""WhsCode"" = '" + sucursal + @"' AND UOM.""UomCode"" is not null 
            //    ) UNION (
            //    Select
            //        product.""ItemCode"",
            //        product.""ItemName"",
            //        stock.""OnHand"",
            //        baseUOM.""UomCode"" as base,
            //        stock.""OnHand"" / product.""U_IL_PesProm"" as stock,
            //        'Caja' as ""UomCode""
            //    From OITM product
            //    JOIN OITW stock ON stock.""ItemCode"" = product.""ItemCode""
            //    LEFT JOIN OUGP header ON header.""UgpCode"" = product.""ItemCode""
            //    LEFT JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
            //    LEFT JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
            //    LEFT JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry"" AND UOM.""UomEntry"" != baseUOM.""UomEntry""
            //    Where product.""QryGroup" + group + @""" = 'Y' AND stock.""WhsCode"" = '" + sucursal + @"' AND UOM.""UomCode"" is null AND product.""U_IL_PesProm""  != 0
            //    ) UNION (
            //    Select
            //        product.""ItemCode"",
            //        product.""ItemName"",
            //        stock.""OnHand"",
            //        baseUOM.""UomCode"" as base,
            //        0 as stock,
            //        '' as ""UomCode""
            //    From OITM product
            //    JOIN OITW stock ON stock.""ItemCode"" = product.""ItemCode""
            //    LEFT JOIN OUGP header ON header.""UgpCode"" = product.""ItemCode""
            //    LEFT JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
            //    LEFT JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
            //    LEFT JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry"" AND UOM.""UomEntry"" != baseUOM.""UomEntry""
            //    Where product.""QryGroup" + group + @""" = 'Y' AND stock.""WhsCode"" = '" + sucursal + @"' AND UOM.""UomCode"" is null AND product.""U_IL_PesProm"" = 0
            //    )");
            oRecSet.MoveFirst();
            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];
            //List<JToken> pro = temp.ToObject<List<JToken>>();
            //List<JToken>  products = new List<JToken>();
            //for(int i = 0; i< pro.Count; i++) {
            //    int index = products.FindIndex(a => a["ItemCode"].ToString() == pro[i]["ItemCode"].ToString());
            //    if(index > -1) {
            //        if (products[index]["UomCode"].ToString() == "") {
            //            products[index] = pro[i];
            //        }
            //    } else {
            //        if (pro[i]["UomCode"].ToString() == "") {
            //            pro[i]["STOCK"] = 0;
            //        }
            //        products.Add(pro[i]);
            //    }

            //}
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(temp);
        }


        // GET: api/Products/S01/5
        [HttpGet("{sucursal}/{group}")]
        public async Task<IActionResult> GetWarehouseGroup(string sucursal, string group)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0){
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select 
                    product.""ItemName"",
                    product.""ItemCode"",
                    RTRIM(RTRIM(stock.""OnHand"", '0'), '.') AS ""OnHand"",
                    product.""ManBtchNum"",
                    product.""U_IL_TipPes""
                From OITM product
                JOIN OITW stock ON stock.""ItemCode"" = product.""ItemCode""
                Where product.""QryGroup" + group + @""" = 'Y' 
                AND product.""Canceled"" = 'N'
                AND product.""validFor"" = 'Y'
                AND stock.""WhsCode"" = '" + sucursal + @"'");
            oRecSet.MoveFirst();
            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(temp);
        }

        // GET: api/Products/Properties
        [HttpGet("Properties")]
        public async Task<IActionResult> GetProperties() {
            
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"Select * From OITG");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITG"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(products);
        }

        // GET: api/Products/Detail/5
        [HttpGet("Detail/{id}")]
        public async Task<IActionResult> GetDetail(string id)
        {
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
                From OITM Where ""ItemCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken Detail = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];

            oRecSet.DoQuery(@"
                Select 
                    header.""UgpCode"",
                    header.""BaseUom"",
                    baseUOM.""UomCode"" as baseUOM,
                    detail.""UomEntry"",
                    UOM.""UomCode"",
                    detail.""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];


            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { Detail, uom });
        }

    }
}
