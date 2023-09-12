using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using SAP_API.Models;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using ClosedXML.Excel;
using System.IO;
using System.Net.Mime;
using Newtonsoft.Json;
using SAPbobsCOM;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryTransferController : ControllerBase {
        private readonly IConfiguration _configuration;

        public InventoryTransferController( IConfiguration configuration)
        {
            _configuration = configuration;

        }
        /// <summary>
        /// Get Transfer List to WMS web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>TransferSearchResponse</returns>
        /// <response code="200">TransferSearchResponse(SearchResponse)</response>
        // POST: api/InventoryTransfer/Search
        [ProducesResponseType(typeof(TransferSearchResponse), StatusCodes.Status200OK)]
        [HttpPost("Search")]
        public async Task<IActionResult> GetSearch([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(\"Filler\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(\"ToWhsCode\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty) {
                where.Add($"to_char(to_date(SUBSTRING(\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[3].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY \"DocNum\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY \"Filler\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY \"ToWhsCode\" {request.order[0].dir}";
            } else if (request.order[0].column == 3) {
                orderby = $" ORDER BY \"DocDate\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY \"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    to_char(to_date(SUBSTRING(""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    ""ToWhsCode"",
                    ""Filler""
                From OWTR ";

            if (where.Count != 0) {
                query += "Where " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            List<TransferSearchDetail> orders = context.XMLTOJSON(oRecSet.GetAsXML())["OWTR"].ToObject<List<TransferSearchDetail>>();

            string queryCount = @"Select Count (*) as COUNT From OWTR ";

            if (where.Count != 0) {
                queryCount += "Where " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OWTR"][0]["COUNT"].ToObject<int>();

            TransferSearchResponse respose = new TransferSearchResponse {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };

            return Ok(respose);
        }

        /// <summary>
        /// Get InventoryTransfer Detail to WMS InventoryTransfer Detail Page
        /// </summary>
        /// <param name="DocEntry">DocEntry. An Unsigned Integer that serve as Document identifier.</param>
        /// <returns>A InventoryTransfer Detail</returns>
        /// <response code="200">Returns InventoryTransfer Detail</response>
        /// <response code="204">No InventoryTransfer Found</response>
        // GET: api/InventoryTransfer/WMSDetail/:DocEntry
        [ProducesResponseType(typeof(TransferDetail), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("WMSDetail/{DocEntry}")]
        public async Task<IActionResult> GetWMSDetail(uint DocEntry) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    to_char(to_date(SUBSTRING(""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",
                    to_char(to_date(SUBSTRING(""CancelDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""CancelDate"",
                    ""Comments"",
                    ""ToWhsCode"",
                    ""Filler""
                From OWTR
                WHERE ""DocEntry"" = '{DocEntry}';");

            if (oRecSet.RecordCount == 0) {
                return NoContent();
            }

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["OWTR"][0];

            oRecSet.DoQuery($@"
                Select
                    ""ItemCode"",
                    ""Dscription"",
                    ""Quantity"",
                    ""UomCode"",
                    ""InvQty"",
                    ""UomCode2""
                From WTR1
                WHERE ""DocEntry"" = '{DocEntry}';");

            temp["TransferRows"] = context.XMLTOJSON(oRecSet.GetAsXML())["WTR1"];

            TransferDetail output = temp.ToObject<TransferDetail>();
            
            //Force Garbage Collector. Recommendation by InterLatin Dude. SDK Problem with memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(output);
        }

        // GET: api/InventoryTransfer/list
        [HttpGet("list/{date}")]
        public async Task<IActionResult> GetList(string date) {
            
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocDate"",
                    ""CANCELED"",
                    ""DocStatus""
                From OWTR Where ""DocDate"" = '" + date + "'");

            int rc = oRecSet.RecordCount;
            if (rc == 0) {
                return NotFound();
            }

            JToken tranferList = context.XMLTOJSON(oRecSet.GetAsXML())["OWTR"];

            return Ok(tranferList);
        }
        [HttpGet("lastUOM/{ItemCode}")]
        public async Task<IActionResult> GetLastUom(string ItemCode)
        {
            try
            {
                SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
                   Select
                   top 1  ""UomEntry"" 
                   from ""WTR1""
                   where ""ItemCode"" = '{ItemCode}'
                   order by ""DocDate"" desc");
            
            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                JToken LastUom = context.XMLTOJSON(oRecSet.GetAsXML())["WTR1"][0]["UomEntry"];

                return Ok(LastUom);
            }
            catch (Exception)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                return Ok("");
            }
        }

        // GET: api/InventoryTransfer/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id) {
            
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.StockTransfer transfer = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oStockTransfer);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery("Select * From OWTR WHERE \"DocNum\" = " + id);
            int rc = oRecSet.RecordCount;
            if (rc == 0) {
                return NotFound();
            }
            transfer.Browser.Recordset = oRecSet;
            transfer.Browser.MoveFirst();


            JToken temp = context.XMLTOJSON(transfer.GetAsXML());
            temp["OWTR"] = temp["OWTR"][0];
            temp["AdmInfo"]?.Parent.Remove();
            temp["WTR12"]?.Parent.Remove();
            temp["BTNT"]?.Parent.Remove();
            return Ok(temp);

        }
        [AllowAnonymous]
        [HttpGet("PedidoSugerido/{id}")]
        public async Task<IActionResult> PedidoSugerido(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);


            string p = $@"
            SELECT
T0.""Artículo"",
T0.""ItemName"",
T0.""WhsCode"",
T0.""V_ACUMULADA"",
T0.""Prom_Lun"",
T0.""Prom_Mar"",
T0.""Prom_Mie"",
T0.""Prom_Jue"",
T0.""Prom_Vie"",
T0.""Prom_Sab"",
T0.""Prom_Dom"",
T0.""OnHand"",
T0.""Unidad de Medida Base"",
T0.""UNIDADES x CAJA"",
T0.""STOCK x CAJAS"" ,
(CASE WHEN T0.""WhsCode"" IN('S09', '''S21', 'S32', 'S34', 'S38', 'S46', 'S48', 'S51', 'S52', 'S57') THEN
CASE WHEN WEEKDAY(CURRENT_DATE)= 1 THEN(T0.""Prom_Mar"" + T0.""Prom_Mie"" + T0.""Prom_Jue"" + T0.""Prom_Vie"")
ELSE
CASE WHEN WEEKDAY(CURRENT_DATE)= 3 THEN(T0.""Prom_Jue"" + T0.""Prom_Vie"" + T0.""Prom_Sab"" + T0.""Prom_Dom"")
ELSE
CASE WHEN WEEKDAY(CURRENT_DATE)= 5 THEN(T0.""Prom_Sab"" + T0.""Prom_Dom"" + T0.""Prom_Lun"" + T0.""Prom_Mar"") ELSE 0 END
END
END
ELSE
CASE WHEN T0.""WhsCode"" IN('S02', 'S03', 'S04', 'S08', 'S11', 'S14', 'S23', 'S28', 'S44', 'S54', 'S61', 'S64', 'S65') THEN
CASE WHEN WEEKDAY(CURRENT_DATE)= 0 THEN(""Prom_Lun"" + T0.""Prom_Mar"" + T0.""Prom_Mie"" + T0.""Prom_Jue"")
ELSE
CASE WHEN WEEKDAY(CURRENT_DATE)= 2 THEN(T0.""Prom_Mie"" + T0.""Prom_Jue"" + T0.""Prom_Vie"" + T0.""Prom_Sab"")
ELSE
CASE WHEN WEEKDAY(CURRENT_DATE)= 4 THEN(T0.""Prom_Vie"" + T0.""Prom_Sab"" + T0.""Prom_Dom"" + T0.""Prom_Lun"") ELSE 0 END
END
END
END
END) AS ""VENTA PROMEDIO HISTORICA"",
(CASE WHEN T0.""WhsCode"" IN('S09', 'S21', 'S32', 'S34', 'S38', 'S46', 'S48', 'S51', 'S52', 'S57') THEN
CASE WHEN WEEKDAY(CURRENT_DATE)= 1 AND((T0.""Prom_Mar"" + T0.""Prom_Mie"" + T0.""Prom_Jue"" + T0.""Prom_Vie"") - T0.""OnHand"") > 0 THEN(T0.""Prom_Mar"" + T0.""Prom_Mie"" + T0.""Prom_Jue"" + T0.""Prom_Vie"") - T0.""OnHand""
ELSE
CASE WHEN WEEKDAY(CURRENT_DATE)= 3 AND((T0.""Prom_Jue"" + T0.""Prom_Vie"" + T0.""Prom_Sab"" + T0.""Prom_Dom"") - T0.""OnHand"") > 0 THEN(T0.""Prom_Jue"" + T0.""Prom_Vie"" + T0.""Prom_Sab"" + T0.""Prom_Dom"") - T0.""OnHand""
ELSE
CASE WHEN WEEKDAY(CURRENT_DATE)= 5 AND((T0.""Prom_Sab"" + T0.""Prom_Dom"" + T0.""Prom_Lun"" + T0.""Prom_Mar"") - T0.""OnHand"") > 0 THEN(T0.""Prom_Sab"" + T0.""Prom_Dom"" + T0.""Prom_Lun"" + T0.""Prom_Mar"") - T0.""OnHand"" ELSE 0 END
END
END
ELSE
CASE WHEN T0.""WhsCode"" IN('S02', 'S03', 'S04', 'S08', 'S11', 'S14', 'S23', 'S28', 'S44', 'S54', 'S61', 'S64', 'S65') THEN
CASE WHEN WEEKDAY(CURRENT_DATE)= 0 AND((T0.""Prom_Lun"" + T0.""Prom_Mar"" + T0.""Prom_Mie"" + T0.""Prom_Jue"") - T0.""OnHand"") > 0 THEN(T0.""Prom_Lun"" + T0.""Prom_Mar"" + T0.""Prom_Mie"" + T0.""Prom_Jue"") - T0.""OnHand""
ELSE
CASE WHEN WEEKDAY(CURRENT_DATE)= 2 AND((T0.""Prom_Mie"" + T0.""Prom_Jue"" + T0.""Prom_Vie"" + T0.""Prom_Sab"") - T0.""OnHand"") > 0 THEN(T0.""Prom_Mie"" + T0.""Prom_Jue"" + T0.""Prom_Vie"" + T0.""Prom_Sab"") - T0.""OnHand""
ELSE
CASE WHEN WEEKDAY(CURRENT_DATE)= 4 AND((T0.""Prom_Vie"" + T0.""Prom_Sab"" + T0.""Prom_Dom"" + T0.""Prom_Lun"") - T0.""OnHand"") > 0 THEN(T0.""Prom_Vie"" + T0.""Prom_Sab"" + T0.""Prom_Dom"" + T0.""Prom_Lun"") - T0.""OnHand"" ELSE 0 END
END
END
END
END) AS ""PEDIDO SUGERIDO"",
(T0.""Prom_Lun"" + T0.""Prom_Mar"" + T0.""Prom_Mie"" + T0.""Prom_Jue"" + T0.""Prom_Vie"" + T0.""Prom_Sab"" + T0.""Prom_Dom"") AS ""VTA-PROM-SEM"",
(T0.""OnHand"" / ((T0.""Prom_Lun"" + T0.""Prom_Mar"" + T0.""Prom_Mie"" + T0.""Prom_Jue"" + T0.""Prom_Vie"" + T0.""Prom_Sab"" + T0.""Prom_Dom"")/ 7 )) as ""DIAS-INV""

FROM
(
SELECT
T1.""U_SO1_NUMEROARTICULO"" AS ""Artículo"",
T3.""ItemName"",
T2.""WhsCode"",
SUM(T1.""U_SO1_CANTIDAD"") AS ""V_ACUMULADA"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 0 then 1 else 0 end) as ""Lunes"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 0 then 1 else 0 end) / 8 as ""Prom_Lun"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 1 then 1 else 0 end) as ""Martes"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 1 then 1 else 0 end) / 8 as ""Prom_Mar"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 2 then 1 else 0 end) as ""Miercoles"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 2 then 1 else 0 end) / 8 as ""Prom_Mie"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 3 then 1 else 0 end) as ""Jueves"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 3 then 1 else 0 end) / 8 as ""Prom_Jue"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 4 then 1 else 0 end) as ""Viernes"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 4 then 1 else 0 end) / 8 as ""Prom_Vie"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 5 then 1 else 0 end) as ""Sabado"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 5 then 1 else 0 end) / 8 as ""Prom_Sab"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 6 then 1 else 0 end) as ""Domingo"",
sum(case when WEEKDAY(T0.""U_SO1_FECHA"") = 6 then 1 else 0 end) / 8 as ""Prom_Dom"",
SUM(T1.""U_SO1_CANTIDAD"" * T1.""U_SO1_IMPORTENETO"") AS ""Importe"" ,
T2.""OnHand"",
(case when T3.""InvntryUom"" = 'H87' then 'PZ'
when T3.""InvntryUom"" = 'XPK' then 'PQ'
when T3.""InvntryUom"" = 'XSA' then 'SC'
when T3.""InvntryUom"" = 'H87' then 'PZ'
when T3.""InvntryUom"" = 'KGM' then 'KG'
when T3.""InvntryUom"" = 'XBX' then 'CJ'
when T3.""InvntryUom"" = 'XBJ' then 'CB'
when T3.""InvntryUom"" = 'BLL' then 'GALON'
else T3.""InvntryUom"" end) AS ""Unidad de Medida Base"",
T5.""BaseQty"" AS ""UNIDADES x CAJA"",
T2.""OnHand"" / T5.""BaseQty"" AS ""STOCK x CAJAS""

FROM ""@SO1_01VENTA"" T0
INNER JOIN ""@SO1_01VENTADETALLE"" T1 ON T1.""U_SO1_FOLIO"" = T0.""Name""
INNER JOIN OITW T2 ON T2.""ItemCode"" = T1.""U_SO1_NUMEROARTICULO""
INNER JOIN OITM T3 ON T2.""ItemCode"" = T3.""ItemCode""
INNER JOIN OUGP T4 ON T3.""UgpEntry"" = T4.""UgpEntry""
INNER JOIN UGP1 T5 ON T4.""UgpEntry"" = T5.""UgpEntry"" AND T3.""PUoMEntry"" = T5.""UomEntry""
INNER JOIN OUOM T6 ON T3.""PUoMEntry"" = T6.""UomEntry""

WHERE T0.""U_SO1_FECHA"" >= ADD_DAYS(CURRENT_DATE, -56) AND T2.""ItemCode"" Like 'A040%'
AND T0.""U_SO1_FECHA"" <= CURRENT_DATE
AND T0.""U_SO1_SUCURSAL"" = '{id}'
AND T2.""WhsCode"" = '{id}'
AND T2.""OnHand"" > 0
GROUP BY T1.""U_SO1_NUMEROARTICULO"" , T3.""ItemName"", T2.""WhsCode"", T2.""OnHand"",T5.""BaseQty"",T3.""InvntryUom""
ORDER BY T1.""U_SO1_NUMEROARTICULO""
) T0
";
            try
            {
                oRecSet.DoQuery(p);
                if (oRecSet.RecordCount != 0)
                {
                    var invoice = context.FixedXMLTOJSON(oRecSet.GetFixedXML(SAPbobsCOM.RecordsetXMLModeEnum.rxmData));
                    List<JObject> Lista=JsonConvert.DeserializeObject<List<JObject>>(JsonConvert.SerializeObject(invoice));
                    JObject pe= (JObject)Lista[0];
                     String value= (string)pe.GetValue("Articulo");
                    var people = from pre in Lista

                                 select new
                                 {
                                    Articulo=pre.GetValue("Artículo").ToString(),
                                     Producto = pre.GetValue("ItemName").ToString(),
                                     AlmacenCodigo = pre.GetValue("WhsCode").ToString(),
                                     V_ACUMULADA = pre.GetValue("V_ACUMULADA").ToString(),
                                     Prom_Lun = pre.GetValue("Prom_Lun").ToString(),
                                     Prom_Mar = pre.GetValue("Prom_Mar").ToString(),
                                     Prom_Mie = pre.GetValue("Prom_Mie").ToString(),
                                     Prom_Jue = pre.GetValue("Prom_Jue").ToString(),
                                     Prom_Vie = pre.GetValue("Prom_Vie").ToString(),
                                     Prom_Sab= pre.GetValue("Prom_Sab").ToString(),
                                     Prom_Dom = pre.GetValue("Prom_Dom").ToString(),
                                     OnHand = pre.GetValue("OnHand").ToString(),
                                     Unidad_Medida_Base = pre.GetValue("Col13").ToString(),
                                     UNIDADESxCaja = pre.GetValue("Col14").ToString(),
                                     STOCKxCaja= pre.GetValue("Col15").ToString(),
                                     VENTA_PROMEDIO_HISTORICA = pre.GetValue("Col16").ToString(),
                                     PEDIDOSUGERIDO = pre.GetValue("Col17").ToString(),
                                     VTA_PROM_SEM= pre.GetValue("VTA-PROM-SEM").ToString(),
                                     DIAS_INV= pre.GetValue("DIAS-INV").ToString(),

                                
                                 };

                     var wb = new XLWorkbook();
                     var ws = wb.Worksheets.Add("Inserting Tables");

                     var tableWithPeople = ws.Cell(1, 1).InsertTable(people.AsEnumerable());

                      using (var stream = new System.IO.MemoryStream())
                    {
                        wb.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Sugerido.xlsx");
                    }
            }
                else
                {
                    return NotFound();
                }


            }
            catch (Exception ex)
            {

                return BadRequest(ex);
            }
        }
        /// <summary>
        /// Add a Transfer Document Linked to a Order Document.
        /// </summary>
        /// <param name="value">A Transfer Parameters</param>
        /// <returns>Message</returns>
        /// <response code="200">Transfer Added</response>
        /// <response code="400">Error</response>
        /// <response code="204">Document not Found</response>
        /// <response code="409">Transfer Added But Copy Failed</response>
        // POST: api/InventoryTransfer
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        //[Authorize]
        [HttpPost("SAP")]
        public async Task<IActionResult> InventoryTransferPost([FromBody] Transfer value)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.StockTransfer transferRequest = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryTransferRequest);
            SAPbobsCOM.StockTransfer transfer = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oStockTransfer);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (!transferRequest.GetByKey(value.DocEntry))
            {
                return NoContent();
            }

            oRecSet.DoQuery($@"
                Select
                    serie1.""SeriesName"",
                    serie1.""Series"",
                    serie1.""ObjectCode"",
                    serie2.""SeriesName""as s1,
                    serie2.""Series"" as s2,
                    serie2.""ObjectCode"" as s3
                From NNM1 serie1
                JOIN NNM1 serie2 ON serie1.""SeriesName"" = serie2.""SeriesName""
                Where serie1.""ObjectCode"" = 67 AND serie2.""Series"" = '{transferRequest.Series}';");

            if (oRecSet.RecordCount == 0)
            {
                return BadRequest("Error En Sucursal.");
            }
            /*if (value.TransferRows.Exists(x => x.BatchList.Exists(x => x.Code == "SI")))
            {
            SAPbobsCOM.CompanyService oCS = (SAPbobsCOM.CompanyService)context.oCompany.GetCompanyService();
            SAPbobsCOM.InventoryPostingsService oICS = (InventoryPostingsService)oCS.GetBusinessService(SAPbobsCOM.ServiceTypes.InventoryPostingsService);
            SAPbobsCOM.InventoryPosting oIC ;
            oIC = (InventoryPosting)oICS.GetDataInterface(SAPbobsCOM.InventoryPostingsServiceDataInterfaces.ipsInventoryPosting);
            DateTime dt = DateTime.Now;
            oIC.CountDate = DateTime.Now;
            
            foreach (var item in value.TransferRows)
            {
                if (item.BatchList.Exists(x => x.Code == "SI"))
                {
                    SAPbobsCOM.InventoryPostingLines oICLS = oIC.InventoryPostingLines;
                    SAPbobsCOM.InventoryPostingLine oICL = oICLS.Add();
                    oICL.ItemCode = item.ItemCode;

                    oICL.CountedQuantity = item.BatchList.Where(x => x.Code == "SI").Sum(x => x.Quantity);
                    oICL.UoMCountedQuantity = item.BatchList.Where(x => x.Code == "SI").Sum(x => x.Quantity);
                    oICL.UoMCode = item.UomCode;
                    oICL.WarehouseCode = transferRequest.FromWarehouse;
                    SAPbobsCOM.InventoryPostingBatchNumber batch = oICL.InventoryPostingBatchNumbers.Add();
                    batch.Quantity = -item.BatchList.Where(x => x.Code == "SI").Sum(x => x.Quantity);
                    batch.BatchNumber = "SI";

                    foreach (var lote in item.BatchList)
                    {
                        batch = oICL.InventoryPostingBatchNumbers.Add();
                        if (String.IsNullOrEmpty(lote.CodeBar)) { }
                        else
                        {
                            if (lote.Code == "SI")
                            {
                                batch.Quantity = lote.Quantity;
                                batch.BatchNumber = lote.CodeBar;
                            }
                            else
                            {
                                batch.Quantity = lote.Quantity;
                                batch.BatchNumber = lote.Code;
                            }
                        }
                    }
                }
            }
                SAPbobsCOM.InventoryPostingParams oICP = oICS.Add(oIC);

            }*/
            
            //int Serie = context.XMLTOJSON(oRecSet.GetAsXML())["NNM1"][0]["Series"].ToObject<int>();
            int Serie = (int)oRecSet.Fields.Item("Series").Value;

            transfer.DocDate = DateTime.Now;
            transfer.Series = Serie;

            for (int i = 0; i < value.TransferRows.Count; i++)
            {

                transfer.Lines.BaseEntry = transferRequest.DocEntry;
                transfer.Lines.BaseLine = value.TransferRows[i].LineNum;
                transfer.Lines.Quantity = value.TransferRows[i].Count;
                transfer.Lines.BaseType = SAPbobsCOM.InvBaseDocTypeEnum.InventoryTransferRequest;
                
                if (value.TransferRows[i].Pallet != String.Empty && value.TransferRows[i].Pallet != null)
                {
                    transfer.Lines.UserFields.Fields.Item("U_Tarima").Value = value.TransferRows[i].Pallet;
              }

                for (int k = 0; k < value.TransferRows[i].BatchList.Count; k++)
                {

                    transfer.Lines.BatchNumbers.BatchNumber = value.TransferRows[i].BatchList[k].Code;
                    transfer.Lines.BatchNumbers.Quantity = value.TransferRows[i].BatchList[k].Quantity;
                    transfer.Lines.BatchNumbers.Add();
                }

                transfer.Lines.Add();
            }


            StringBuilder Errors = new StringBuilder();
            if (transfer.Add() != 0)
            {
                Errors.AppendLine($"Documento Transferencia: ");
                Errors.AppendLine(context.oCompany.GetLastErrorDescription());
            }

            if (Errors.Length != 0)
            {
                string error = Errors.ToString();
                return BadRequest(error);
            }

                        sendMail(value,transfer,transferRequest);
            /*
            if (transferRequest.Lines.FromWarehouseCode != transferRequest.Lines.WarehouseCode)
            {
                return Ok();
            }
            */
            /*if (transferRequest.ToWarehouse == "TSR")
            {
                return Ok();
            }*/
            //Get Document Updated.
            transferRequest.GetByKey(value.DocEntry);
            if (transferRequest.Lines.FromWarehouseCode.Contains(transferRequest.Lines.WarehouseCode))
            {
                return Ok();
            }
            SAPbobsCOM.StockTransfer newRequest = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryTransferRequest);
            newRequest.FromWarehouse = transferRequest.FromWarehouse;
            newRequest.ToWarehouse = transferRequest.ToWarehouse;
            newRequest.Series = transferRequest.Series;
            newRequest.UserFields.Fields.Item("U_SO1_02NUMRECEPCION").Value = transferRequest.DocNum.ToString();

            for (int j = 0; j < transfer.Lines.Count; j++)
            {

                transfer.Lines.SetCurrentLine(j);
                transferRequest.Lines.SetCurrentLine(transfer.Lines.BaseLine);

                newRequest.Lines.ItemCode = transferRequest.Lines.ItemCode;
                newRequest.Lines.UoMEntry = transfer.Lines.UoMEntry;
                newRequest.Lines.UseBaseUnits = transferRequest.Lines.UseBaseUnits;
                newRequest.Lines.Quantity = transfer.Lines.Quantity;
                newRequest.Lines.FromWarehouseCode = transferRequest.Lines.WarehouseCode;
                newRequest.Lines.WarehouseCode = transferRequest.ToWarehouse;
                newRequest.Lines.Add();
            }

            Errors = new StringBuilder();
            if (newRequest.Add() != 0)
            {
                Errors.AppendLine($"Documento Copia: ");
                Errors.AppendLine(context.oCompany.GetLastErrorDescription());
            }

            if (Errors.Length != 0)
            {
                string error = Errors.ToString();
                return Conflict(error);
            }

            return Ok(newRequest);
        }
        [NonAction]
            public void sendMail(Transfer transferPOST,SAPbobsCOM.StockTransfer transferencia,SAPbobsCOM.StockTransfer transferrequest)
        {
            MailMessage message = new MailMessage(_configuration["CuentaAutorizacion"], $@"{transferrequest.ToWarehouse}-Pedidos@superchivas.com.mx")
            {
                Subject = "Transferencia en proceso",
                IsBodyHtml = true
            };
            Console.WriteLine($@"{transferrequest.ToWarehouse}-Pedidos@superchivas.com.mx");
            string Cabecera = $@"
<html>
<body>
<p>Solicitud: {transferrequest.DocNum}</p>
<p>Sucursal origen: {transferrequest.FromWarehouse}</p>
<p>Sucursal destino: {transferrequest.ToWarehouse}</p>
<p>Fecha y hora de creación: {transferencia.DocDate}</p>
<p>Productos enviados:</p>
<table style=""border-collapse: collapse; width: 100 %;"" border=""1"">
    <tbody>
    <tr>
    <td style = ""width: 25%;""> Código </td>
     <td style = ""width: 32.5284%;"" > Producto </td>
      <td style = ""width: 17.4716%;"" > Cantidad PU </td>
                </tr>
                ";
            foreach (var item in transferPOST.TransferRows)
            {
                Cabecera += $@"
                    <tr>
                    <td style = ""width: 25%;""> {item.ItemCode}</td>
                    <td style = ""width: 32.5284%;"" > {item.ItemName}</td>
                    <td style = ""width: 17.4716%;"" >{item.Count} {item.UomCode}</td>
                    </tr> ";
            }
            Cabecera += "" +
                "</tbody>" +
                "</table>" +
                "</body>" +
                "</html>";
            message.Body = Cabecera;
            var smtpClient = new SmtpClient(_configuration["smtpserver"])
            {
                Port = 587,
                Credentials = new NetworkCredential(_configuration["CuentaAutorizacion"], _configuration["PassAutorizacion"]),
                EnableSsl = true,
            };
            // Credentials are necessary if the server requires the client
            // to authenticate before it will send email on the client's behalf.

            try
            {
                smtpClient.Send(message);
                return ;
            }
            catch (Exception ex)
            {
                return;

            }
        }

        // POST: api/InventoryTransfer
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TransferOld value)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.StockTransfer request = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryTransferRequest);
            SAPbobsCOM.StockTransfer transfer = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oStockTransfer);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            if (request.GetByKey(value.order))
            {

                transfer.DocDate = DateTime.Now;

                oRecSet.DoQuery(@"
                    Select
                        serie1.""SeriesName"",
                        serie1.""Series"",
                        serie1.""ObjectCode"",
                        serie2.""SeriesName""as s1,
                        serie2.""Series"" as s2,
                        serie2.""ObjectCode"" as s3
                    From NNM1 serie1
                    JOIN NNM1 serie2 ON serie1.""SeriesName"" = serie2.""SeriesName""
                    Where serie1.""ObjectCode"" = 67 AND serie2.""Series"" = '" + request.Series + "'");
                oRecSet.MoveFirst();
                transfer.Series = context.XMLTOJSON(oRecSet.GetAsXML())["NNM1"][0]["Series"].ToObject<int>();

                for (int i = 0; i < value.products.Count; i++)
                {
                    //transfer.Lines.ItemCode = value.products[i].ItemCode;
                    //transfer.Lines.Quantity = value.products[i].Count;
                    //transfer.Lines.UoMEntry = value.products[i].UoMEntry;
                    //transfer.Lines.FromWarehouseCode = "S01";
                    // transfer.Lines.WarehouseCode = value.products[i].WarehouseCode;
                    transfer.Lines.BaseEntry = request.DocEntry;
                    transfer.Lines.BaseLine = value.products[i].Line;
                    transfer.Lines.Quantity = value.products[i].Count;
                    transfer.Lines.BaseType = SAPbobsCOM.InvBaseDocTypeEnum.InventoryTransferRequest;
                    transfer.Lines.UserFields.Fields.Item("U_Tarima").Value = value.products[i].Pallet;

                    for (int j = 0; j < value.products[i].batch.Count; j++)
                    {
                        //transfer.Lines.BatchNumbers.BaseLineNumber = transfer.Lines.LineNum;
                        transfer.Lines.BatchNumbers.BatchNumber = value.products[i].batch[j].name;
                        transfer.Lines.BatchNumbers.Quantity = value.products[i].batch[j].quantity;
                        transfer.Lines.BatchNumbers.Add();
                    }

                    transfer.Lines.Add();
                }

                int result = transfer.Add();
                if (result == 0)
                {
                    if (request.Lines.FromWarehouseCode == request.FromWarehouse)
                    {
                        request.GetByKey(value.order);

                        //for (int i = 0; i < request.Lines.Count; i++)
                        //{
                        //    request.Lines.SetCurrentLine(i);
                        //    if (request.Lines.RemainingOpenQuantity != 0) {
                        //        request.Lines.Quantity = request.Lines.Quantity - request.Lines.RemainingOpenQuantity;
                        //    }
                        //}

                        //int result3 = request.Update();
                        //if (result3 != 0)
                        //{
                        //    string error = context.oCompany.GetLastErrorDescription();
                        //    return BadRequest(new {id = 1,  error });
                        //}

                        try
                        {

                            SAPbobsCOM.StockTransfer newRequest = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryTransferRequest);

                            newRequest.FromWarehouse = request.FromWarehouse;
                            newRequest.ToWarehouse = request.ToWarehouse;
                            newRequest.Series = request.Series;

                            newRequest.UserFields.Fields.Item("U_SO1_02NUMRECEPCION").Value = request.DocNum.ToString();

                            for (int i = 0; i < value.products.Count; i++)
                            {
                                //request.Lines.SetCurrentLine(value.products[i].Line);

                                newRequest.Lines.ItemCode = value.products[i].ItemCode;

                                //newRequest.Lines.UoMEntry = request.Lines.UoMEntry;

                                newRequest.Lines.UoMEntry = value.products[i].UoMEntry;

                                //newRequest.Lines.UseBaseUnits = request.Lines.UseBaseUnits;

                                newRequest.Lines.UseBaseUnits = value.products[i].UseBaseUnits;

                                newRequest.Lines.Quantity = value.products[i].Count;
                                newRequest.Lines.FromWarehouseCode = request.Lines.WarehouseCode;
                                newRequest.Lines.WarehouseCode = request.ToWarehouse;
                                newRequest.Lines.Add();
                            }
                            int result2 = newRequest.Add();
                            if (result2 != 0)
                            {
                                string error = context.oCompany.GetLastErrorDescription();
                                Console.WriteLine(2);
                                Console.WriteLine(error);
                                Console.WriteLine(value);
                                Console.WriteLine(context.XMLTOJSON(newRequest.GetAsXML()));
                                return BadRequest(new { id = 2, error, value, va = context.XMLTOJSON(newRequest.GetAsXML()) });
                            }
                            return Ok(context.oCompany.GetNewObjectKey());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(6);
                            Console.WriteLine(ex);
                            Console.WriteLine(value);
                            return BadRequest(new { id = 5, ex.Message, value });
                        }
                    }
                }
                else
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { id = 3, error });
                }

                return Ok(new { value });

            }
            return BadRequest(new { error = "No Existe Documento" });
        }
        [HttpGet("ProteusInventory/{Transfer}")]
        public async Task<IActionResult> GetInventoryTransferProtheus(string Transfer)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            String Query = $@"
             SELECT
  RIGHT(T1.""WhsCode"",2) AS ""1"",
  CASE
    WHEN T1.""Quantity"" > 0 THEN
      CASE
        WHEN T1.""Quantity"" * T1.""LineTotal"" > 0 THEN
          '003'
        ELSE
          '002'
      END
    ELSE
      CASE
        WHEN T1.""Quantity"" * T1.""LineTotal"" < 0 THEN
          '601'
        ELSE
          '501'
      END
  END AS ""2"",
 RIGHT(T1.""ItemCode"", 7) AS ""3"",

T1.""Quantity"" * T1.""NumPerMsr"" AS ""4"",
  '01' AS ""5"",
  CONCAT(T2.""DocNum"", LPAD(T1.""LineNum"", 2, '00')) AS ""6"",
    TO_VARCHAR(T2.""DocDate"", 'DD/MM/YYYY') as ""7"",
 (T1.""StockPrice"" * (T1.""Quantity"" * T1.""NumPerMsr"")) AS ""11"",
  --T1.""LineTotal"" * IFNULL(NULLIF(T1.""Rate"", 0), 1) AS ""11"",
  'Transferencia SAP' AS ""12""
  FROM
    WTR1 T1 INNER JOIN
    OWTR T2
      ON T1.""DocEntry"" = T2.""DocEntry"" INNER JOIN
    NNM1 T3
      ON T2.""Series"" = T3.""Series"" INNER JOIN
    OITM T4
      ON T4.""ItemCode"" = T1.""ItemCode""
  WHERE
     T2.""DocNum""='{Transfer}'
  ORDER BY
    T3.""SeriesName""";
            oRecSet.DoQuery(Query);

            if (oRecSet.RecordCount == 0)
            {
                return NoContent();
            }

            JToken Transferencia = context.XMLTOJSON(oRecSet.GetAsXML())["WTR1"];

            //Force Garbage Collector. Recommendation by InterLatin Dude. SDK Problem with memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(Transferencia);
        }
    }
}
