using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CodeBarController : ControllerBase {
        /// <summary>
        ///     Busqueda de Producto por codigo de barra 
        /// </summary>
        /// <param name="id"></param>
        /// <returns>
        ///     Regresa Detalle del Producto, sus codigos y su unidades de medida
        /// </returns>
        // GET: api/CodeBar/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id) {

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
                    ""BcdEntry"",
                    ""BcdCode"",
                    ""BcdName"",
                    ""ItemCode"",
                    ""UomEntry""
                From OBCD Where ""BcdCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            if (oRecSet.RecordCount == 0) {
                return NotFound();
            }

            string itemcode = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"][0]["ItemCode"].ToString();

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
                From OITM Where ""ItemCode"" = '" + itemcode + "'");
            oRecSet.MoveFirst();
            JToken Detail = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
            oRecSet.DoQuery(@"
                Select
                    ""BcdEntry"",
                    ""BcdCode"",
                    ""BcdName"",
                    ""ItemCode"",
                    ""UomEntry""
                From OBCD Where ""ItemCode"" = '" + itemcode + "'");
            oRecSet.MoveFirst();
            JToken CodeBars = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];

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
                Where header.""UgpCode"" = '" + itemcode + "'");
            oRecSet.MoveFirst();
            JToken uom = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];

            return Ok(new { Detail, CodeBars, uom });
        }

        /// <summary>
        ///     Agregar Codigo a un producto Relacionado con una unidad de medida
        /// </summary>
        /// <param name="value"></param>
        /// <returns>
        ///     Regresa el identificador del codigo creado
        /// </returns>
        // POST: api/CodeBar
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Codebar value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            SAPbobsCOM.CompanyService services = context.oCompany.GetCompanyService();
            SAPbobsCOM.BarCodesService barCodesService = (SAPbobsCOM.BarCodesService)services.GetBusinessService(SAPbobsCOM.ServiceTypes.BarCodesService);
            SAPbobsCOM.BarCode barCode = (SAPbobsCOM.BarCode)barCodesService.GetDataInterface(SAPbobsCOM.BarCodesServiceDataInterfaces.bsBarCode);

            barCode.ItemNo = value.ItemCode;
            barCode.BarCode = value.Barcode;
            barCode.UoMEntry = value.UOMEntry;
            
            try {
                SAPbobsCOM.BarCodeParams result = barCodesService.Add(barCode);
                return Ok(result.AbsEntry);
            } catch (Exception x) {
                return BadRequest(x.Message);
            }
            
        }

    }
}
