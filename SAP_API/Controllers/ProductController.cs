using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    [Authorize]
    public class ProductsController : ControllerBase
    {
        [AllowAnonymous]
        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.IItems items = (SAPbobsCOM.IItems)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems);
            if (items.GetByKey(id))
            {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                return Ok(temp);
            }
            return NotFound("No Existe Producto");
        }
        [HttpPost("UpdateGTIN")]
        public async Task<IActionResult> UpdateGtin(GTIPPost GTIN)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.IItems items = (SAPbobsCOM.IItems)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems);
            if (items.GetByKey(GTIN.id))
            {

                items.SupplierCatalogNo = GTIN.SupplierNumber;
                int updated = items.Update();
                if (updated != 0)
                {
                    return BadRequest("Error al actualizar");
                }
                return Ok("Actualizado correctamente");
            }
            return NotFound("No Existe Producto");
        }
        [HttpGet("GetbyGTIN/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetbyGTIN(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            string query = $"SELECT \"ItemCode\" FROM OITM where \"SuppCatNum\" ='{id}'";
            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            JToken Product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];

            oRecSet.DoQuery($@"
                Select
                    ""ItemCode"",
                    ""ItemName"",
                    ""QryGroup7"",
                    ""QryGroup41"",
                    ""QryGroup45"",
                    ""ManBtchNum"",
                    ""U_IL_PesMax"",
                    ""U_IL_PesMin"",
                    ""U_IL_PesProm"",
""UgpEntry"",
""SuppCatNum"",
                    ""U_IL_TipPes"",
                    ""NumInSale"",
                    ""NumInBuy""
                From OITM Where ""ItemCode"" = '{Product["ItemCode"]}'");
            if (oRecSet.RecordCount == 0) return NotFound("Articulo no encontrado");
            JToken Detail = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            oRecSet.DoQuery($@"
                    Select
                    ""BcdEntry"",
                    ""BcdCode"",
                    ""BcdName"",
                    ""ItemCode"",
                    ""UomEntry""
                    From OBCD Where ""ItemCode"" = '{Product["ItemCode"]}';");
            oRecSet.MoveFirst();
            JToken CodeBars = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];

            oRecSet.DoQuery($@"
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
                Where header.""UgpEntry"" = '" + Detail["UgpEntry"] + "'");

            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { Detail, CodeBars, uom });
        }
        [HttpPost("Search/ToSell")]
        public async Task<IActionResult> GetSearchToSell([FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY \"ItemCode\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY \"ItemName\" {request.order[0].dir}";
            }
            else
            {
                orderby = $" ORDER BY \"ItemCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ""ItemName"",
                    ""ItemCode""
                From OITM
                Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y'";

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
                Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y' ";

            if (where.Count != 0)
            {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

            var respose = new ProductSearchResponse
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

        [HttpPost("Search/ToSellWithStock/{warehouse}")]
        public async Task<IActionResult> GetSearchToSellWithStock(string warehouse, [FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(item.\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(item.\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty)
            {
                where.Add($"LOWER(stock.\"OnHand\") Like LOWER('%{request.columns[2].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY item.\"ItemCode\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY item.\"ItemName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 2)
            {
                orderby = $" ORDER BY stock.\"OnHand\" {request.order[0].dir}";
            }
            else
            {
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

            if (where.Count != 0)
            {
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

            if (where.Count != 0)
            {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

            var respose = new ProductWithStockSearchResponse
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
        [HttpGet("GetGrouped")]
        public IActionResult GetGroupedByProduct()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<ProductoParaCliente> Productos = new List<ProductoParaCliente>();

            string query = $@"SELECT
T0.""ItemCode"",

T0.""ItemName"",

T1.""ItmsGrpCod"" AS ""Cód.Grupo"",

T0.""U_IL_SubGrupo"",

CASE

WHEN T0.""QryGroup5"" = 'Y' THEN '05'

WHEN T0.""QryGroup6"" = 'Y' THEN '06'

WHEN T0.""QryGroup7"" = 'Y' THEN '07'

WHEN T0.""QryGroup8"" = 'Y' THEN '08'

WHEN T0.""QryGroup9"" = 'Y' THEN '09'

WHEN T0.""QryGroup10"" = 'Y' THEN '10'

WHEN T0.""QryGroup11"" = 'Y' THEN '11'

WHEN T0.""QryGroup12"" = 'Y' THEN '12'

WHEN T0.""QryGroup13"" = 'Y' THEN '13'

WHEN T0.""QryGroup14"" = 'Y' THEN '14'

WHEN T0.""QryGroup15"" = 'Y' THEN '15'

END AS ""Cód.Categoría"",

CASE

WHEN T0.""QryGroup5"" = 'Y' THEN 'ABARROTES'

WHEN T0.""QryGroup6"" = 'Y' THEN 'LACTEOS'

WHEN T0.""QryGroup7"" = 'Y' THEN 'CARNES'

WHEN T0.""QryGroup8"" = 'Y' THEN 'FRUTAS Y VERDURAS'

WHEN T0.""QryGroup9"" = 'Y' THEN 'OTROS SERVICIOS'

WHEN T0.""QryGroup10"" = 'Y' THEN 'MERCANCIAS GENERALES'

WHEN T0.""QryGroup11"" = 'Y' THEN 'PANADERIA Y TORTILLERIA'

WHEN T0.""QryGroup12"" = 'Y' THEN 'DONATIVOS CRIT'

WHEN T0.""QryGroup13"" = 'Y' THEN 'FARMACIA'

WHEN T0.""QryGroup14"" = 'Y' THEN 'ALIMENTOS PREPARADOS'

WHEN T0.""QryGroup15"" = 'Y' THEN 'PRODUCTOS EMPACADOS'

END AS ""ItmsGrpNam""

FROM OITM T0 INNER JOIN OITB T1 ON T0.""ItmsGrpCod"" = T1.""ItmsGrpCod""
WHERE ""QryGroup3"" = 'Y'
ORDER BY ""ItmsGrpNam""
";
            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            Productos = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"].ToObject<List<ProductoParaCliente>>();
            return Ok(Productos);

        }
        [HttpPost("Search/ToSellWithStockRetail/{warehouse}")]
        public async Task<IActionResult> GetSearchToSellWithStockRetail(string warehouse, [FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(item.\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(item.\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty)
            {
                where.Add($"LOWER(stock.\"OnHand\") Like LOWER('%{request.columns[2].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY item.\"ItemCode\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY item.\"ItemName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 2)
            {
                orderby = $" ORDER BY stock.\"OnHand\" {request.order[0].dir}";
            }
            else
            {
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
                AND ""SellItem"" = 'Y' AND ""QryGroup4"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y'";

            if (where.Count != 0)
            {
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
                AND ""SellItem"" = 'Y' AND ""QryGroup4"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y' ";

            if (where.Count != 0)
            {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

            var respose = new ProductWithStockSearchResponse
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
        // GET: api/Products/ProvidersProducts
        [HttpPost("Search/ToBuy")]
        public async Task<IActionResult> GetSearchToBuy([FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY \"ItemCode\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY \"ItemName\" {request.order[0].dir}";
            }
            else
            {
                orderby = $" ORDER BY \"ItemCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ""ItemName"",
                    ""ItemCode""
                From OITM
                Where ""PrchseItem"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y' AND (""ItemCode"" LIKE 'G%' OR ""ItemCode"" LIKE 'ACT%') ";

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
                Where ""PrchseItem"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y' AND (""ItemCode"" LIKE 'G%' OR ""ItemCode"" LIKE 'ACT%')  ";

            if (where.Count != 0)
            {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

            var respose = new ProductSearchResponse
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
        [HttpPost("Search/ToTransferWithStock/{warehouse}")]
        public async Task<IActionResult> GetSearchToTransferWithStock(string warehouse, [FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(item.\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(item.\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty)
            {
                where.Add($"LOWER(stock.\"OnHand\") Like LOWER('%{request.columns[2].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY item.\"ItemCode\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY item.\"ItemName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 2)
            {
                orderby = $" ORDER BY stock.\"OnHand\" {request.order[0].dir}";
            }
            else
            {
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
                AND ""Canceled"" = 'N' AND ""validFor"" = 'Y'";

            if (where.Count != 0)
            {
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
                AND ""Canceled"" = 'N' AND ""validFor"" = 'Y' ";

            if (where.Count != 0)
            {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

            var respose = new ProductWithStockSearchResponse
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
        [HttpPost("search/PriceList/{priceList}/{group}")]
        public async Task<IActionResult> GetSearchPriceList(string priceList, int group, [FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(product.\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(product.\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty)
            {
                where.Add($"LOWER(priceList.\"Price\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty)
            {
                where.Add($"LOWER(iuom.\"UomCode\") Like LOWER('%{request.columns[3].search.value}%')");
            }
            if (request.columns[4].search.value != String.Empty)
            {
                where.Add($"(((product.\"IUoMEntry\"  = '6' AND product.\"SUoMEntry\"  = '6' AND product.\"U_IL_PesProm\" != 0) AND LOWER((product.\"U_IL_PesProm\" * priceList.\"Price\")) Like LOWER('%{request.columns[4].search.value}%')) OR ((product.\"SUoMEntry\" != '6' AND product.\"U_IL_PesProm\" = 0) AND LOWER((product.\"NumInSale\" *priceList.\"Price\")) Like LOWER('%{request.columns[4].search.value}%'))) ");
            }
            if (request.columns[5].search.value != String.Empty)
            {
                where.Add($"(((product.\"IUoMEntry\"  = '6' AND product.\"SUoMEntry\"  = '6' AND product.\"U_IL_PesProm\" != 0) AND LOWER('Caja') Like LOWER('%{request.columns[5].search.value}%')) OR ((product.\"SUoMEntry\" != '6' AND product.\"U_IL_PesProm\" = 0) AND LOWER(suom.\"UomCode\") Like LOWER('%{request.columns[5].search.value}%'))) ");
            }
            if (request.columns[6].search.value != String.Empty)
            {
                where.Add($"LOWER(priceList.\"Currency\") Like LOWER('%{request.columns[6].search.value}%')");
            }

            if (group != -1)
            {
                where.Add($"product.\"QryGroup" + group + "\" = 'Y'");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY product.\"ItemCode\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY product.\"ItemName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 2)
            {
                orderby = $" ORDER BY priceList.\"Price\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 3)
            {
                orderby = $" ORDER BY iuom.\"UomCode\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 4)
            {
                orderby = $" ORDER BY \"Price2\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 5)
            {
                orderby = $" ORDER BY \"UomCode2\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 6)
            {
                orderby = $" ORDER BY priceList.\"Currency\" {request.order[0].dir}";
            }
            else
            {
                orderby = $" ORDER BY product.\"ItemCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    product.""ItemCode"",
                    product.""ItemName"",
                    priceList.""Price"",
                    iuom.""UomCode"",

                    (case when (product.""IUoMEntry""  = '6' AND product.""SUoMEntry""  = '6' AND product.""U_IL_PesProm"" != 0) then (product.""U_IL_PesProm"" * priceList.""Price"")
                    else (product.""NumInSale"" *priceList.""Price"") end) AS ""Price2"",

                    (case when (product.""IUoMEntry""  = '6' AND product.""SUoMEntry""  = '6' AND product.""U_IL_PesProm"" != 0) then 'Caja'
                    else suom.""UomCode"" end) AS ""UomCode2"",


                    priceList.""Currency""
                From OITM product
                JOIN ITM1 priceList ON product.""ItemCode"" = priceList.""ItemCode""
                JOIN OUOM iuom ON product.""IUoMEntry"" = iuom.""UomEntry""
                JOIN OUOM suom ON product.""SUoMEntry"" = suom.""UomEntry""
                Where (product.""QryGroup5"" = 'Y' OR  product.""QryGroup6"" = 'Y' OR  product.""QryGroup7"" = 'Y' OR product.""QryGroup8"" = 'Y')
                AND priceList.""PriceList"" = " + priceList;

            if (where.Count != 0)
            {
                query += " AND " + whereClause;
            }

            query += orderby;

            if (request.length != -1)
            {
                query += " LIMIT " + request.length + " OFFSET " + request.start + "";
            }

            oRecSet.DoQuery(query);
            List<ProductPriceListDetail> products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"].ToObject<List<ProductPriceListDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From OITM product
                JOIN ITM1 priceList ON product.""ItemCode"" = priceList.""ItemCode""
                JOIN OUOM iuom ON product.""IUoMEntry"" = iuom.""UomEntry""
                JOIN OUOM suom ON product.""SUoMEntry"" = suom.""UomEntry""
                Where (product.""QryGroup5"" = 'Y' OR  product.""QryGroup6"" = 'Y' OR  product.""QryGroup7"" = 'Y' OR product.""QryGroup8"" = 'Y')
                AND priceList.""PriceList"" = " + priceList;

            if (where.Count != 0)
            {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

            ProductPriceListSearchResponse respose = new ProductPriceListSearchResponse
            {
                data = products,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(respose);
        }
        [HttpPost("Search/{warehouse}/{property}")]
        public async Task<IActionResult> GetSearchWithStockAndProperty(string warehouse, int property, [FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty)
            {
                where.Add($"LOWER(item.\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty)
            {
                where.Add($"LOWER(item.\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty)
            {
                where.Add($"LOWER(stock.\"OnHand\") Like LOWER('%{request.columns[2].search.value}%')");
            }

            if (property != -1)
            {
                where.Add($"\"QryGroup" + property + "\" = 'Y' ");
            }

            string orderby = "";
            if (request.order[0].column == 0)
            {
                orderby = $" ORDER BY item.\"ItemCode\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 1)
            {
                orderby = $" ORDER BY item.\"ItemName\" {request.order[0].dir}";
            }
            else if (request.order[0].column == 2)
            {
                orderby = $" ORDER BY stock.\"OnHand\" {request.order[0].dir}";
            }
            else
            {
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
                AND ""InvntItem"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y'";

            if (where.Count != 0)
            {
                query += " AND " + whereClause;
            }

            query += orderby;

            if (request.length != -1)
            {
                query += " LIMIT " + request.length + " OFFSET " + request.start + "";
            }

            oRecSet.DoQuery(query);
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"].ToObject<List<ProductWithStockSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From OITM item
                JOIN OITW stock ON item.""ItemCode"" = stock.""ItemCode""
                Where ""WhsCode"" = '" + warehouse + @"'
                AND ""InvntItem"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y' ";

            if (where.Count != 0)
            {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

            var respose = new ProductWithStockSearchResponse
            {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            return Ok(respose);
        }
        [AllowAnonymous]
        // GET: api/Products/CRMToSell/5
        [HttpGet("CRMToSell/{id}/{priceList}/{warehouse}")]
        public async Task<IActionResult> GetCRMToSell(string id, int priceList, string warehouse)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            try
            {

                SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                JToken product;
                ProductDetail productDetail;

                oRecSet.DoQuery(@"
                    Select
                        product.""ItemName"",
                        product.""ItemCode"",
                        product.""U_IL_PesProm"" AS ""PesProm"",
                        product.""SUoMEntry"",
                        product.""QryGroup7"" as ""Meet"",
                        priceList.""PriceList"",
                        priceList.""Currency"",
                        priceList.""Price"",
                        priceList.""UomEntry"",
                        priceList.""PriceType"",
                        stock.""WhsCode"",
                        stock.""OnHand"",
  product.""UgpEntry""
                    From OITM product
                    JOIN ITM1 priceList ON priceList.""ItemCode"" = product.""ItemCode""
                    JOIN OITW stock ON stock.""ItemCode"" = product.""ItemCode""
                    Where product.""ItemCode"" = '" + id + @"'
                    AND stock.""WhsCode"" = '" + warehouse + @"'
                    AND priceList.""PriceList"" = " + priceList);
                product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];

                oRecSet.DoQuery(@"
                Select 
                    header.""UgpCode"",
                    header.""BaseUom"",
                    baseUOM.""UomCode"" as ""BaseCode"",
                    detail.""UomEntry"",
                    UOM.""UomCode"",
                    detail.""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpEntry"" = '" + product["UgpEntry"] + @"'");
                //AND detail.""UomEntry"" in (Select ""UomEntry"" FROM ITM4 Where ""UomType"" = 'S' AND ""ItemCode"" = '" + id + "')");
                product["UOMList"] = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
                productDetail = product.ToObject<ProductDetail>();
                oRecSet = null;
                product = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return Ok(productDetail);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [AllowAnonymous]
        // GET: api/Products/CRMToSell/5
        [HttpGet("VerifiPrice/{id}/{priceList}/{warehouse}")]
        public async Task<IActionResult> GetVerifyPrice(string id, int priceList, string warehouse)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            try
            {

                SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                JToken product;

                oRecSet.DoQuery(@"
                    Select
                        product.""ItemName"",
                        product.""FrgnName"",
                        product.""ItemCode"",
                        product.""U_IL_PesProm"" AS ""PesProm"",
                        product.""SUoMEntry"",
                        product.""QryGroup7"" as ""Meet"",
                        priceList.""PriceList"",
                        priceList.""Currency"",
                        priceList.""Price"",
  product.""UgpEntry"",
                        priceList.""UomEntry"",
                        priceList.""PriceType"",
                        stock.""WhsCode"",
                        product.""TaxCodeAR"",
                        stock.""OnHand""
                    From OITM product
                    JOIN ITM1 priceList ON priceList.""ItemCode"" = product.""ItemCode""
                    JOIN OITW stock ON stock.""ItemCode"" = product.""ItemCode""
                    Where product.""ItemCode"" = '" + id + @"'
                    AND stock.""WhsCode"" = '" + warehouse + @"'
                    AND priceList.""PriceList"" = " + priceList);
                product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];

                oRecSet.DoQuery(@"
                Select 
                    header.""UgpCode"",
                    header.""BaseUom"",
                    baseUOM.""UomCode"" as ""BaseCode"",
                    detail.""UomEntry"",
                    UOM.""UomCode"",
                    detail.""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                 Where header.""UgpEntry"" = '" + product["UgpEntry"] + @"'");

                //AND detail.""UomEntry"" in (Select ""UomEntry"" FROM ITM4 Where ""UomType"" = 'S' AND ""ItemCode"" = '" + id + "')");
                product["UOMList"] = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
                oRecSet.DoQuery($@"SELECT
                 ""Rate"" FROM OSTC WHERE ""Code""='{product["TaxCodeAR"]}'");
                product["Rate"] = context.XMLTOJSON(oRecSet.GetAsXML())["OSTC"][0]["Rate"];
                oRecSet = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return Ok(product);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // GET: api/Products/Properties
        [HttpGet("Properties")]
        public async Task<IActionResult> GetProperties()
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<Property> propertyList;
            oRecSet.DoQuery(@"Select ""ItmsGrpNam"", ""ItmsTypCod"" From OITG");
            JToken properties = context.XMLTOJSON(oRecSet.GetAsXML())["OITG"];
            propertyList = properties.ToObject<List<Property>>();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(propertyList);
        }
        /// //////////////////////////////////////////
        // GET: api/Products/CRMToSellEdit/5
        [HttpGet("CRMToSellEdit/{id}")]
        public async Task<IActionResult> GetCRMToSellEdit(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select 
                    product.""ItemName"", 
product.""UgpEntry"",
                    product.""ItemCode"", 
                    RTRIM(RTRIM(product.""U_IL_PesProm"", '0'), '.') AS ""U_IL_PesProm"",
                    product.""SUoMEntry""
                From OITM product
                Where product.""ItemCode"" = '" + id + @"'");
            oRecSet.MoveFirst();
            JToken product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            oRecSet.DoQuery(@$"
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
                Where header.""UgpEntry"" = '" + product["UgpEntry"] + @"'");
            //AND detail.""UomEntry"" in (Select ""UomEntry"" FROM ITM4 Where ""UomType"" = 'S' AND ""ItemCode"" = '" + id + "')");
            oRecSet.MoveFirst();
            product["uom"] = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(product);
        }
        // GET: api/Products/ToTransfer/5/S01
        [HttpGet("ToTransfer/{itemcode}/{warehouse}")]
        public async Task<IActionResult> GetToTransfer(string itemcode, string warehouse)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            JToken product;
            ProductToTransferDetail productDetail;

            oRecSet.DoQuery(@"
                Select
                    product.""ItemName"",
                    product.""ItemCode"",
                    product.""U_IL_PesProm"",
                    warehouse.""OnHand"",
                    product.""validFor"",
  product.""UgpEntry""
                From OITM product
                LEFT JOIN OITW warehouse ON warehouse.""ItemCode"" = product.""ItemCode"" 
                Where product.""ItemCode"" = '" + itemcode + @"'
                AND warehouse.""WhsCode"" = '" + warehouse + "'");
            //oRecSet.DoQuery(@"
            //    Select
            //        product.""ItemName"",
            //        product.""ItemCode"",
            //        product.""U_IL_PesProm"" as ""PesProm"",
            //        warehouse.""OnHand""
            //    From OITM product
            //    LEFT JOIN OITW warehouse ON warehouse.""ItemCode"" = product.""ItemCode"" 
            //    Where product.""ItemCode"" = '" + itemcode + @"'
            //    AND warehouse.""WhsCode"" = '" + warehouse + "'");
            oRecSet.MoveFirst();
            product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            //oRecSet.DoQuery(@"
            //    Select 
            //        header.""UgpCode"",
            //        header.""BaseUom"",
            //        baseUOM.""UomCode"" as ""BaseCode"",
            //        detail.""UomEntry"",
            //        UOM.""UomCode"",
            //        RTRIM(RTRIM(detail.""BaseQty"", '0'), '.') AS ""BaseQty""
            //    From OUGP header
            //    JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
            //    JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
            //    JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
            //    Where header.""UgpCode"" = '" + itemcode + "'");
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
                Where header.""UgpEntry"" = '" + product["UgpEntry"] + "'");
            product["uom"] = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            //product["UOMList"] = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            //productDetail = product.ToObject<ProductToTransferDetail>();
            oRecSet = null;
            //product = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(product);
            //return Ok(productDetail);
        }
        //[HttpPost("/UpdatePesProm")]
        //public async Task<IActionResult> GetUpdatePesProm([FromBody] string value) {

        //    SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
        //    SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

        //    List<string> where = new List<string>();
        //    if (request.columns[0].search.value != String.Empty)
        //    {
        //        where.Add($"LOWER(item.\"ItemCode\") Like LOWER('%{request.columns[0].search.value}%')");
        //    }
        //    if (request.columns[1].search.value != String.Empty)
        //    {
        //        where.Add($"LOWER(item.\"ItemName\") Like LOWER('%{request.columns[1].search.value}%')");
        //    }
        //    if (request.columns[2].search.value != String.Empty)
        //    {
        //        where.Add($"LOWER(stock.\"OnHand\") Like LOWER('%{request.columns[2].search.value}%')");
        //    }

        //    string orderby = "";
        //    if (request.order[0].column == 0)
        //    {
        //        orderby = $" ORDER BY item.\"ItemCode\" {request.order[0].dir}";
        //    }
        //    else if (request.order[0].column == 1)
        //    {
        //        orderby = $" ORDER BY item.\"ItemName\" {request.order[0].dir}";
        //    }
        //    else if (request.order[0].column == 2)
        //    {
        //        orderby = $" ORDER BY stock.\"OnHand\" {request.order[0].dir}";
        //    }
        //    else
        //    {
        //        orderby = $" ORDER BY item.\"ItemCode\" DESC";
        //    }

        //    string whereClause = String.Join(" AND ", where);

        //    string query = @"
        //        Select
        //            item.""ItemName"",
        //            item.""ItemCode"",
        //            stock.""OnHand""
        //        From OITM item
        //        JOIN OITW stock ON item.""ItemCode"" = stock.""ItemCode""
        //        Where ""WhsCode"" = '" + warehouse + @"'
        //        AND ""SellItem"" = 'Y' AND ""QryGroup4"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y'";

        //    if (where.Count != 0)
        //    {
        //        query += " AND " + whereClause;
        //    }

        //    query += orderby;

        //    query += " LIMIT " + request.length + " OFFSET " + request.start + "";

        //    oRecSet.DoQuery(query);
        //    oRecSet.MoveFirst();
        //    var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"].ToObject<List<ProductWithStockSearchDetail>>();

        //    string queryCount = @"
        //        Select
        //            Count (*) as COUNT
        //        From OITM item
        //        JOIN OITW stock ON item.""ItemCode"" = stock.""ItemCode""
        //        Where ""WhsCode"" = '" + warehouse + @"'
        //        AND ""SellItem"" = 'Y' AND ""QryGroup4"" = 'Y' AND ""Canceled"" = 'N' AND ""validFor"" = 'Y' ";

        //    if (where.Count != 0)
        //    {
        //        queryCount += " AND " + whereClause;
        //    }
        //    oRecSet.DoQuery(queryCount);
        //    oRecSet.MoveFirst();
        //    int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["COUNT"].ToObject<int>();

        //    var respose = new ProductWithStockSearchResponse
        //    {
        //        data = orders,
        //        draw = request.Draw,
        //        recordsFiltered = COUNT,
        //        recordsTotal = COUNT,
        //    };
        //    GC.Collect();
        //    GC.WaitForPendingFinalizers();
        //    return Ok(respose);
        //}

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // GET: api/Products/CRMToBuy/5
        [HttpGet("CRMToBuy/{id}")]
        public async Task<IActionResult> GetCRMToBuy(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
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

        // GET: api/Products/WMSToInventory/A0101001/S01
        [HttpGet("WMSToInventory/{id}/{warehouse}")]
        public async Task<IActionResult> Get(string id, string warehouse)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    product.""ItemCode"",
                    product.""ItemName"",
product.""UgpEntry"",
                    product.""QryGroup7"",
                    product.""QryGroup41"",
                    product.""ManBtchNum"",
                    product.""U_IL_PesMax"",
                    product.""U_IL_PesMin"",
                    product.""U_IL_PesProm"",
                    product.""U_IL_TipPes"",
                    product.""NumInSale"",
                    product.""NumInBuy"",
                    RTRIM(RTRIM(stock.""OnHand"", '0'), '.') AS ""OnHand""
                From OITM product Where ""ItemCode"" = '" + id + @"'
                AND stock.""WhsCode"" = '" + warehouse + @"'");
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
                Where header.""UgpEntry"" = '" + Detail["UgpEntry"] + "'");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { Detail, uom });
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // GET: api/Products/UpdateStock
        [HttpPost("UpdateStock/{warehouse}")]
        public async Task<IActionResult> GetUpdateStock(string warehouse, [FromBody] string[] itemcodes)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            string itemcodesFormat = String.Join("','", itemcodes);
            oRecSet.DoQuery(@"
                Select 
                    product.""ItemCode"",
                    RTRIM(RTRIM(stock.""OnHand"", '0'), '.') AS ""OnHand""
                From OITM product
                JOIN OITW stock ON stock.""ItemCode"" = product.""ItemCode""
                Where product.""ItemCode"" in ('" + itemcodesFormat + @"') 
                AND stock.""WhsCode"" = '" + warehouse + @"'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(products);
        }

        // TODO: Warehouse filter New WMS
        // GET: api/Products/UomDetailWithLastSellPrice
        [HttpPost("UomDetailWithLastSellPrice")]
        public async Task<IActionResult> GetUomDetails([FromBody] string[] itemcodes)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            string itemcodesFormat = String.Join("','", itemcodes);
            oRecSet.DoQuery(@"
                Select
                    product.""ItemCode"",
                    product.""ItemName"",
                    product.""NumInSale"",
                    product.""SUoMEntry"",
                    product.""IUoMEntry"",
                    product.""U_IL_PesProm"",
                    warehouse.""AvgPrice"" AS ""Price"",
                    'MXN' as ""Currency"",
                    product.""QryGroup5"",
                    product.""QryGroup6"",
                    product.""QryGroup7"",
                    product.""QryGroup8"",
                    product.""QryGroup39""
                From OITM product
                JOIN OITW warehouse ON product.""ItemCode"" = warehouse.""ItemCode""
                Where product.""ItemCode"" in ('" + itemcodesFormat + @"')
                AND warehouse.""WhsCode"" = 'S01' ");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            List<ProductLastSellPriceWMS> productLastSellPrices = products.ToObject<List<ProductLastSellPriceWMS>>();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(productLastSellPrices);
        }
        [HttpPost("UomDetailWithLastSellPrice/{Id}")]
        public async Task<IActionResult> GetUomDetailsWithSource([FromRoute] String Id, [FromBody] string[] itemcodes)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            string itemcodesFormat = String.Join("','", itemcodes);
            oRecSet.DoQuery(@$"
                Select
                    product.""ItemCode"",
                    product.""ItemName"",
                    product.""NumInSale"",
                    product.""SUoMEntry"",
                    product.""IUoMEntry"",
                    product.""U_IL_PesProm"",
                    warehouse.""AvgPrice"" AS ""Price"",
                    'MXN' as ""Currency"",
                    product.""QryGroup5"",
                    product.""QryGroup6"",
                    product.""QryGroup7"",
                    product.""QryGroup8"",
                    product.""QryGroup39""
                From OITM product
                JOIN OITW warehouse ON product.""ItemCode"" = warehouse.""ItemCode""
                Where product.""ItemCode"" in ('" + itemcodesFormat + $@"')
                AND warehouse.""WhsCode"" = '{Id}' ");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            List<ProductLastSellPriceWMS> productLastSellPrices = products.ToObject<List<ProductLastSellPriceWMS>>();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(productLastSellPrices);
        }

        // GET: api/Products/UomDetailWithLastSellPrice
        [HttpGet("UomDetailWithLastSellPrice/{id}")]
        public async Task<IActionResult> GetUomDetail(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
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
                AND priceList.""PriceList"" = 23");
            oRecSet.MoveFirst();
            JToken product = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(product);
        }

        // GET: api/Products/Uoms
        [HttpGet("Uoms")]
        public async Task<IActionResult> GetUoms()
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
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
                AND ""WhsCode"" in ('S01', 'S70'
, 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S47', 'S55')
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
            return Ok(new { products, stock });
        }

        // GET: api/Products/TranferList
        [HttpGet("TransferList")]
        public async Task<IActionResult> GetTransferList()
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"ItemCode\", \"ItemName\" From OITM Where \"Canceled\" = 'N' AND \"validFor\" = 'Y' AND \"SellItem\" = 'Y' AND \"InvntItem\" = 'Y' ");
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
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select 
                    ""ItemName"",
                    ""UgpEntry"",
                    ""ItemCode"",
""U_IL_Marca"",
                    ""QryGroup7"" as ""Meet"",
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
                Where ""PriceList"" in (
                    SELECT Distinct ""ListNum""
                    From OCRD
                    Where ""CardType"" = 'C' AND ""CardCode"" LIKE '%-P')
                AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();// in (19, 13, 14, 15, 16)
            JToken priceList = context.XMLTOJSON(oRecSet.GetAsXML())["ITM1"];
            oRecSet.DoQuery(@"
                Select 
                    ""ItemCode"",
                    ""WhsCode"",
                    RTRIM(RTRIM(""OnHand"", '0'), '.') AS ""OnHand""
                From OITW
                Where ""OnHand"" != 0 
                    AND ""Freezed"" = 'N'
                    AND ""Locked"" = 'N'
                    AND ""WhsCode"" in ('S01','S70', 'S06', 'S07','S17', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S47', 'S55', 'S59', 'S62','S63')
                    AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];
            oRecSet.DoQuery(@"
                Select 
                   header.""UgpCode"",
                    header.""BaseUom"",
                    baseUOM.""UomCode"" as baseUOM,
                    detail.""UomEntry"",
header.""UgpEntry"",
                    UOM.""UomCode"",
                    RTRIM(RTRIM(detail.""BaseQty"", '0'), '.') AS ""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpEntry"" in (Select ""UgpEntry"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            var returnValue = new { products, priceList, stock, uom };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(returnValue);
        }

        [AllowAnonymous]
        // GET: api/Products/APPCRM
        [HttpGet("TARIMASPRODUCTOS")]
        public async Task<IActionResult> TARIMASPRODUCTOS()
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select 
                    ""ItemName"",
                    ""UgpEntry"",
                    ""ItemCode"",
                    ""QryGroup7"" as ""Meet"",
""CodeBars"" as ""CodeBars"",
                    RTRIM(RTRIM(""U_IL_PesProm"", '0'), '.') AS ""U_IL_PesProm""
                From OITM WHERE ""Canceled"" = 'N'  AND ""validFor"" = 'Y'  AND ""ItemName"" is not null
               ");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            oRecSet.DoQuery(@"
                Select 
                   header.""UgpCode"",
                    header.""BaseUom"",
                    baseUOM.""UomCode"" as baseUOM,
                    detail.""UomEntry"",
header.""UgpEntry"",
                    UOM.""UomCode"",
                    RTRIM(RTRIM(detail.""BaseQty"", '0'), '.') AS ""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpEntry"" in (Select ""UgpEntry"" From OITM WHERE ""Canceled"" = 'N'  AND ""validFor"" = 'Y'  AND ""ItemName"" is not null)");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];

            oRecSet.DoQuery(@"
 Select
                    ""BcdCode"",
                    OITM.""ItemCode""
                From OBCD
                join OITM on OITM.""ItemCode"" = OBCD.""ItemCode""  WHERE ""Canceled"" = 'N'  AND ""validFor"" = 'Y'  AND ""ItemName"" is not null
               ;

            ");
            JToken CodeBars = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];

            var returnValue = new { products, uom, CodeBars };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(returnValue);
        }

        [HttpGet("APPCRM/{CardCode}")]
        public async Task<IActionResult> GetCRMS(String CardCode)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery($@"
                Select 
                    ""ItemName"",
                    ""ItemCode"",
                    ""QryGroup7"" as ""Meet"",
                    RTRIM(RTRIM(""U_IL_PesProm"", '0'), '.') AS ""U_IL_PesProm"",
                    ""U_IL_TipPes"" AS ""U_IL_TipPes"",
""QryGroup1"" AS ""Nacional"",
""U_IL_Giro"",
                    ""QryGroup2"" AS ""Extranjero""
                From OITM
                Where ""SellItem"" = 'Y'
                AND ""QryGroup3"" = 'Y'
                AND ""Canceled"" = 'N'
                AND ""validFor"" = 'Y'
                AND ""ItemCode""='{CardCode}' ");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            oRecSet.DoQuery($@"
                Select 
                    ""PriceList"",
                    ""ItemCode"",
                    ""Currency"",
                    RTRIM(RTRIM(""Price"", '0'), '.') AS ""Price"",
                    ""UomEntry""
                From ITM1
                Where   ""PriceList"" in ('26','27','28','29','30','31','32','33')
                AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y' AND ""ItemCode""='{CardCode}' )");
            oRecSet.MoveFirst();// in (19, 13, 14, 15, 16)
            JToken priceList = context.XMLTOJSON(oRecSet.GetAsXML())["ITM1"];
            oRecSet.DoQuery($@"
                Select 
                    ""ItemCode"",
                    ""WhsCode"",
                    RTRIM(RTRIM(""OnHand"", '0'), '.') AS ""OnHand""
                From OITW
                Where ""OnHand"" != 0 
                    AND ""Freezed"" = 'N'
                    AND ""Locked"" = 'N'
                    AND ""WhsCode"" in ('S01','S70', 'S06', 'S07', 'S10', 'S12', 'S13','S17', 'S15', 'S24', 'S36', 'S47', 'S55', 'S59', 'S62','S63')
                    AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y' AND ""ItemCode""='{CardCode}' )");
            oRecSet.MoveFirst();
            JToken stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];
            oRecSet.DoQuery($@"
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
                Where header.""UgpEntry"" in (Select ""UgpEntry"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y' AND ""ItemCode""='{CardCode}' )");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            var returnValue = new { products, priceList, stock, uom };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(returnValue);
        }
        // CON NUEVA VERSION APP NO PUBLICADA
        // GET: api/Products/APPCRM/200
        [HttpGet("APPCRM/{id}")]
        public async Task<IActionResult> GetCRMSContact(int id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
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
                AND ""PriceList"" in (
                    SELECT Distinct ""ListNum""
                    From OCRD employeeSales
                    JOIN OHEM employee ON ""SlpCode"" = ""salesPrson""
                    Where ""CardType"" = 'C' AND ""empID"" = " + id + @" AND ""CardCode"" LIKE '%-P');");
            oRecSet.MoveFirst();
            JToken priceList = context.XMLTOJSON(oRecSet.GetAsXML())["ITM1"];

            //oRecSet.DoQuery($"Select \"Fax\" From OSLP Where \"SlpCode\" = {id}");
            //oRecSet.MoveFirst();
            //string warehouses = context.XMLTOJSON(oRecSet.GetAsXML())["OSLP"][0]["Fax"].ToString();
            //warehouses = warehouses.Trim();
            //if (warehouses.Equals("")) { 
            string warehouses = "'S01','S70' 'S06','S17', 'S07', 'S10', 'S12', 'S13', 'S15', 'S24', 'S36', 'S47', 'S55', 'S59', 'S62','S63'";
            //}
            //else {
            //    warehouses = warehouses.ToUpper();
            //    warehouses = "'" + warehouses + "'";
            //    warehouses = warehouses.Replace(",", "','");
            //}

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
                Where header.""UgpEntry"" in (Select ""UgpEntry"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { products, priceList, stock, uom });
        }


        // GET: api/Products/CRM/5
        [HttpGet("CRM/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCRM(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select 
                    ""ItemName"", 
                    ""UgpEntry"",
                    ""ItemCode"", 
                    ""ItmsGrpCod"",
                    ""ManBtchNum"",
                    ""U_IL_TipPes"",
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
                WHERE ""WhsCode"" in ('S01','S70', 'S06', 'S07', 'S10','S17', 'S12', 'S13', 'S15', 'S24', 'S36', 'S37', 'S47', 'S55', 'S59','S62')
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
                Where header.""UgpEntry"" = '" + products["UgpEntry"] + "'");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { products, priceList, stock, uom });
        }

        // GET: api/Products/CRM/5
        [HttpGet("CRMWMSSS/{id}")]
        public async Task<IActionResult> GetCRMWMSSS(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select 
                    ""ItemName"", 
                    ""ItemCode"",
""UgpEntry"",
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
                WHERE ""WhsCode"" in ('S01','S70', 'S06', 'S07', 'S10','S17', 'S12', 'S13', 'S15', 'S24', 'S36', 'S37', 'S47', 'S55', 'S59','S62')
                AND ""ItemCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];
            oRecSet.DoQuery(@"
                Select 
                    baseUOM.""UomCode"" as baseUOM,
                    detail.""UomEntry"",
                    UOM.""UomCode"",
                    RTRIM(RTRIM(detail.""BaseQty"", '0'), '.') AS ""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpEntry"" = '" + products["UgpEntry"] + "'");
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


        // GET: api/Products/ConsumoInterno/5
        [HttpGet("ConsumoInterno/{id}")]
        //[Authorize]
        public async Task<IActionResult> GetDetailConumoInterno(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""ItemName"",
                    ""QryGroup7""
                From OITM Where ""ItemCode"" = '" + id + "'");

            JToken Detail = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];


            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(Detail);
        }


        // GET: api/Products/Detail/5
        [HttpGet("Detail/{id}")]
        [AllowAnonymous]
        //[Authorize]
        public async Task<IActionResult> GetDetail(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""ItemName"",
                    ""QryGroup7"",
                    ""QryGroup41"",
                    ""QryGroup45"",
                    ""ManBtchNum"",
                    ""U_IL_PesMax"",
                    ""U_IL_PesMin"",
                    ""U_IL_PesProm"",
""UgpEntry"",
                    ""U_IL_TipPes"",""SuppCatNum"",
                    ""NumInSale"",
                    ""NumInBuy""
                From OITM Where ""ItemCode"" = '" + id + "'");
            if (oRecSet.RecordCount == 0) return NotFound("No existe articulo");
            JToken Detail = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];

            oRecSet.DoQuery($@"
                SELECT 
            T2.""UgpCode"",
            T2.""BaseUom"",
			T1.""UomCode"" as baseUOM,
			T0.""BcdCode"",
			T3.""UomEntry"",
			T3.""BaseQty"",
			T1.""UomCode""            
            FROM OBCD T0
            LEFT JOIN OUOM T1 on T0.""UomEntry""= T1.""UomEntry"" 
			LEFT JOIN OUGP T2 on T2.""UgpCode""='{id}' 
			LEFT JOIN UGP1 T3 on T2.""UgpEntry""= T3.""UgpEntry"" AND T0.""UomEntry""=T3.""UomEntry"" 
             WHERE ""ItemCode""='{id}';");

            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { Detail, uom });
        }


        // GET: api/Products/InventoryCompleteProducts
        [HttpGet("InventoryCompleteProducts/{warehouse}")]
        public async Task<IActionResult> GetInventoryCompleteProducts(string warehouse)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
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
                Where product.""Canceled"" = 'N'
                AND product.""validFor"" = 'Y'
                AND stock.""WhsCode"" = '" + warehouse + @"'
                AND product.""ItemCode"" in (
                    SELECT ""ItemCode"" FROM PDN1 WHERE ""DocDate"" > ADD_MONTHS (CURRENT_DATE, -3) AND ""WhsCode"" = '" + warehouse + @"' AND ""DocEntry"" in (SELECT ""DocEntry"" FROM OPDN WHERE ""DocDate"" > ADD_MONTHS (CURRENT_DATE, -3)  AND ""CANCELED"" = 'N')
                    Union
                    SELECT ""ItemCode"" FROM IGN1 WHERE ""DocDate"" > ADD_MONTHS(CURRENT_DATE, -3) AND ""WhsCode"" = '" + warehouse + @"' AND ""DocEntry"" in (SELECT ""DocEntry"" FROM OIGN WHERE ""DocDate"" > ADD_MONTHS(CURRENT_DATE, -3) AND ""CANCELED"" = 'N')
                    Union
                    SELECT ""ItemCode"" FROM WTR1 WHERE ""DocDate"" > ADD_MONTHS(CURRENT_DATE, -3) AND ""WhsCode"" = '" + warehouse + @"' AND ""DocEntry"" in (SELECT ""DocEntry"" FROM OWTR WHERE ""DocDate"" > ADD_MONTHS(CURRENT_DATE, -3) AND ""CANCELED"" = 'N')
                    Union
                    Select ""ItemCode"" FROM OITW WHERE ""WhsCode"" = '" + warehouse + @"' AND ""OnHand"" > 0) ");

            oRecSet.MoveFirst();
            JToken Products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(Products);
        }

    }
}
