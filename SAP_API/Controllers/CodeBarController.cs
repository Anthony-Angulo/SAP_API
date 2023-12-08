using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;
using SAP_API.Models;

namespace SAP_API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class CodeBarController : ControllerBase
    {

        // Note: Documentation No Complete.
        // TODO: Class To Serialize Result.
        /// <summary>
        /// Search Item by Codebar
        /// </summary>
        /// <param name="CodeBar">CodeBar.</param>
        /// <returns>Product</returns>
        /// <response code="200"></response>
        // GET: api/CodeBar/:CodeBar
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("{CodeBar}")]
        //[Authorize]
        public async Task<IActionResult> Get(string CodeBar)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
                Select
                    ""BcdEntry"",
                    ""BcdCode"",
                    ""BcdName"",
                    ""ItemCode"",
                    ""UomEntry""
                From OBCD Where ""BcdCode"" = '{CodeBar}';");

            if (oRecSet.RecordCount == 0)
            {
                return NoContent();
                //return Ok(new NotFoundReturning { Detail = null, CodeBars = null, uom = null });
            }

            string ItemCode = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"][0]["ItemCode"].ToString();

            oRecSet.DoQuery($@"
                    Select
                    ""ItemCode"",
                    ""UgpEntry"",
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
                    From OITM Where ""ItemCode"" = '{ItemCode}';");
            oRecSet.MoveFirst();
            JToken Detail = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            oRecSet.DoQuery($@"
                    Select
                    ""BcdEntry"",
                    ""BcdCode"",
                    ""BcdName"",
                    ""ItemCode"",
                    ""UomEntry""
                    From OBCD Where ""ItemCode"" = '{ItemCode}';");
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
                Where header.""UgpEntry"" = '{Detail["UgpEntry"]}';");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];

            return Ok(new { Detail, CodeBars, uom });
        }

        public class Codebar
        {
            public string ItemCode { set; get; }
            public string Barcode { set; get; }
            public int UOMEntry { set; get; }
        }

        /// <summary>
        /// Register Item CodeBar
        /// </summary>
        /// <param name="codebar">CodeBar.</param>
        /// <returns></returns>
        /// <response code="200">Codebar Added</response>
        /// <response code="400">Error. See Output</response>
        // POST: api/CodeBar
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [HttpPost]
        //[Authorize]
        public async Task<IActionResult> Post([FromBody] Codebar codebar)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.CompanyService services = context.oCompany.GetCompanyService();
            SAPbobsCOM.BarCodesService barCodesService = (SAPbobsCOM.BarCodesService)services.GetBusinessService(SAPbobsCOM.ServiceTypes.BarCodesService);
            SAPbobsCOM.BarCode barCode = (SAPbobsCOM.BarCode)barCodesService.GetDataInterface(SAPbobsCOM.BarCodesServiceDataInterfaces.bsBarCode);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
                Select
                    ""BcdEntry"",
                    ""BcdCode"",
                    ""BcdName"",
                    ""ItemCode"",
                    ""UomEntry""
                From OBCD Where ""BcdCode"" = '{codebar.Barcode}';");

            if (oRecSet.RecordCount != 0)
            {
                string itemcode = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"][0]["ItemCode"].ToString();
                return BadRequest("Ya Existe Codigo de Barra Registrado. Producto: " + itemcode);
            }

            barCode.ItemNo = codebar.ItemCode;
            barCode.BarCode = codebar.Barcode;
            barCode.UoMEntry = codebar.UOMEntry;

            try
            {
                SAPbobsCOM.BarCodeParams result = barCodesService.Add(barCode);
                return Ok(result.AbsEntry);
            }
            catch (Exception x)
            {
                return BadRequest(x.Message);
            }

        }

        [HttpGet("ItemCode/{ItemCode}")]
        public async Task<IActionResult> GetCodebarFromItemCode(string ItemCode)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"Select ""CodeBars"" From OITM Where ""ItemCode"" = '{ItemCode}'");

            if (oRecSet.RecordCount != 0)
            {
                string itemcode = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0]["CodeBars"].ToString();
                return Ok(itemcode);
            }
            else
            {
                return NotFound();
            }

        }
    }

    internal class NotFoundReturning
    {
        public object Detail { get; set; }
        public object CodeBars { get; set; }
        public object uom { get; set; }
    }
}
