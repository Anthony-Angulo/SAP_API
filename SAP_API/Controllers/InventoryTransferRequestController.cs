using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryTransferRequestController : ControllerBase
    {
        //// GET: api/InventoryTransferRequest
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        // GET: api/InventoryTransferRequest/list
        [HttpGet("list/{date}")]
        public async Task<IActionResult> GetList(string date)
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

        // GET: api/InventoryTransferRequest/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
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

        // GET: api/InventoryTransferRequest/Reception/5
        [HttpGet("Reception/{id}")]
        public async Task<IActionResult> GetReception(int id)
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


        // GET: api/InventoryTransferRequest/Detail/5
        [HttpGet("Detail/{id}/{doctype}")]
        public async Task<IActionResult> GetDetail(int id, string doctype)
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

            
            if (!doctype.Equals("DocEntry") && !doctype.Equals("DocNum"))
            {
                return BadRequest(new { error  = "Doc Type to Search Invalid"});
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
                docEntrys = docEntrys.Substring(0, docEntrys.Length-1);
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

            if (!context.oCompany.Connected)
            {
                int code = context.oCompany.Connect();
                if (code != 0)
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

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
                    newRequest.Lines.UoMEntry = 6;
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

            if (!context.oCompany.Connected)
            {
                int code = context.oCompany.Connect();
                if (code != 0)
                {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

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
