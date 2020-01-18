using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        //// GET: api/Products
        //[HttpGet]
        //public async Task<IActionResult> Get()
        //{
        //    SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

        //    if (!context.oCompany.Connected)
        //    {
        //        int code = context.oCompany.Connect();
        //        if (code != 0)
        //        {
        //            //return [context.oCompany.GetLastErrorDescription()];
        //        }
        //    }

        //    SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

        //    List<Object> list = new List<Object>();

        //    oRecSet.DoQuery("Select TOP 1 * From OITM where \"ItemCode\" = 'LMCA0305450'");
        //    oRecSet.MoveFirst();
        //    oRecSet.GetAsXML();
        //    //items.Browser.Recordset = oRecSet;
        //    //items.Browser.MoveFirst();

        //    //while (items.Browser.EoF == false)
        //    //{
        //    //    XmlDocument doc = new XmlDocument();
        //    //    doc.LoadXml(items.GetAsXML());
        //    //    var temp = JObject.Parse(JsonConvert.SerializeXmlNode(doc))["BOM"]["BO"];//["OHEM"]["row"];
        //    //    list.Add(temp);
        //    //    items.Browser.MoveNext();
        //    //}
        //    //items = null;
        //    //oRecSet = null;
        //    //GC.Collect();
        //    //GC.WaitForPendingFinalizers();
        //    return Ok(oRecSet.GetAsXML());

        //}

        // GET: api/Products/CRMList
        [HttpGet("CRMList")]
        public async Task<IActionResult> GetCRMList()
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
            oRecSet.DoQuery("Select \"ItemCode\", \"ItemName\" From OITM Where \"SellItem\" = 'Y' AND \"QryGroup3\" = 'Y' AND \"Canceled\" = 'N' AND \"validFor\" = 'Y'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            GC.WaitForPendingFinalizers();
            return Ok(products);
        }

        // GET: api/Products/ProvidersProducts
        [HttpGet("ProvidersProducts")]
        public async Task<IActionResult> GetProvidersProducts()
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
            oRecSet.DoQuery("Select \"ItemCode\", \"ItemName\" From OITM Where \"PrchseItem\" = 'Y' AND \"Canceled\" = 'N'  AND \"validFor\" = 'Y' AND \"ItemCode\" LIKE 'G%'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            GC.WaitForPendingFinalizers();
            return Ok(products);
        }

        // GET: api/Products/CRMList/Stocks
        [HttpGet("CRMList/Stocks")]
        public async Task<IActionResult> GetCRMListStocks()
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
            SAPbobsCOM.Recordset oRecSet2 = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery("Select \"ItemCode\", \"ItemName\" From OITM Where \"SellItem\" = 'Y' AND \"QryGroup3\" = 'Y' AND \"Canceled\" = 'N'  AND \"validFor\" = 'Y'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];


            oRecSet.DoQuery(@"
                Select 
                    ""ItemCode"",
                    ""WhsCode"",
                    ""OnHand""
                From OITW
                Where ""Freezed"" = 'N'
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
            oRecSet.DoQuery("Select \"ItemCode\", \"ItemName\" From OITM Where \"Canceled\" = 'N'  AND \"validFor\" = 'Y'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            GC.WaitForPendingFinalizers();
            return Ok(products);
        }

        // GET: api/Products/Stock
        [HttpGet("Stock/{sucursal}")]
        public async Task<IActionResult> GetStock(string sucursal)
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
                    ""ItemCode"",
                    ""WhsCode"",
                    ""OnHand""
                From OITW
                Where ""Freezed"" = 'N'
                    AND ""Locked"" = 'N'
                    AND ""WhsCode"" = '" + sucursal + @"'
                    AND ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];
            GC.WaitForPendingFinalizers();
            return Ok(stock);
        }

        // GET: api/Products/APPCRM
        [HttpGet("APPCRM")]
        public async Task<IActionResult> GetCRMS()
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
            oRecSet.DoQuery("Select \"ItemName\", \"ItemCode\", \"U_IL_PesProm\" From OITM Where \"SellItem\" = 'Y' AND \"QryGroup3\" = 'Y' AND \"Canceled\" = 'N'  AND \"validFor\" = 'Y'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            oRecSet.DoQuery(@"
                Select 
                    ""PriceList"",
                    ""ItemCode"",
                    ""Currency"",
                    ""Price"",
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
                    ""OnHand""
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
                    detail.""BaseQty""
                From OUGP header
                JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where header.""UgpCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N'  AND ""validFor"" = 'Y')");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            //oRecSet.DoQuery(@"
            //    Select 
            //        detail.""ItemCode"",
            //        detail.""PriceList"",
            //        detail.""UomEntry"",
            //        detail.""Price"",
            //        detail.""Currency"",
            //        UOM.""UomCode""
            //    From ITM9 detail
            //    JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
            //    Where ""ItemCode"" in (Select ""ItemCode"" From OITM Where ""SellItem"" = 'Y' AND ""QryGroup3"" = 'Y' AND ""Canceled"" = 'N')");
            //oRecSet.MoveFirst();
            //JToken priceUOM = context.XMLTOJSON(oRecSet.GetAsXML())["ITM9"];
            var returnValue = new { products, priceList, stock, uom /*, priceUOM */};
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(returnValue);
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
            oRecSet.DoQuery("Select \"ItemName\", \"ItemCode\", \"ItmsGrpCod\", \"U_IL_PesProm\" From OITM where \"ItemCode\" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            oRecSet.DoQuery("Select \"PriceList\", \"ItemCode\", \"Currency\", \"Price\", \"UomEntry\", \"PriceType\" From ITM1 Where \"ItemCode\" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken priceList = context.XMLTOJSON(oRecSet.GetAsXML())["ITM1"];
            oRecSet.DoQuery("Select \"ItemCode\", \"WhsCode\", \"OnHand\" From OITW Where \"ItemCode\" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken stock = context.XMLTOJSON(oRecSet.GetAsXML())["OITW"];
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
            oRecSet.DoQuery(@"
                Select 
                    detail.""ItemCode"",
                    detail.""PriceList"",
                    detail.""UomEntry"",
                    detail.""Price"",
                    detail.""Currency"",
                    UOM.""UomCode""
                From ITM9 detail
                JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                Where ""ItemCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken priceUOM = context.XMLTOJSON(oRecSet.GetAsXML())["ITM9"];
            var returnValue = new { products, priceList, stock, uom, priceUOM };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(returnValue);
        }

        // GET: api/Products/CRM/5
        [HttpGet("UM/{id}")]
        public async Task<IActionResult> GetUM(string id)
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
                Select
                    product.""ItemCode"",
                    product.""ItemName"",
                    stock.""OnHand"",
                    baseUOM.""UomCode"" as base,
                    stock.""OnHand"" / detail.""BaseQty"" as stock,
                    UOM.""UomCode""
                From OITM product
                JOIN OITW stock ON stock.""ItemCode"" = product.""ItemCode""
                LEFT JOIN OUGP header ON header.""UgpCode"" = product.""ItemCode""
                LEFT JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                LEFT JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                LEFT JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry"" AND UOM.""UomEntry"" != baseUOM.""UomEntry""
                Where product.""QryGroup" + group + @""" = 'Y' AND stock.""WhsCode"" = '" + sucursal + @"'");
            oRecSet.MoveFirst();
            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"];
            List<JToken> pro = temp.ToObject<List<JToken>>();
            List<JToken>  products = new List<JToken>();
            for(int i = 0; i< pro.Count; i++) {
                int index = products.FindIndex(a => a["ItemCode"].ToString() == pro[i]["ItemCode"].ToString());
                if(index > -1) {
                    if (products[index]["UomCode"].ToString() == "") {
                        products[index] = pro[i];
                    }
                } else {
                    if (pro[i]["UomCode"].ToString() == "") {
                        pro[i]["STOCK"] = 0;
                    }
                    products.Add(pro[i]);
                }
                
            }
            GC.WaitForPendingFinalizers();
            return Ok(products);
        }

        // GET: api/Products/Properties
        [HttpGet("Properties")]
        public async Task<IActionResult> GetProperties()
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
                    *
                From OITG");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML())["OITG"];
            GC.WaitForPendingFinalizers();
            return Ok(products);
        }

        // GET: api/Products/Detail/5
        [HttpGet("Detail/{id}")]
        public async Task<IActionResult> GetDetail(string id)
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
                    ""BcdEntry"",
                    ""BcdCode"",
                    ""BcdName"",
                    ""ItemCode"",
                    ""UomEntry""
                From OBCD Where ""ItemCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken CodeBars = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];

            return Ok(new { Detail, CodeBars});
        }

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

        //// POST: api/Products
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT: api/Products/5
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
