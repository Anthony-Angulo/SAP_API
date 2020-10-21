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

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryTransferController : ControllerBase {

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

            if (transferRequest.Lines.FromWarehouseCode != transferRequest.FromWarehouse)
            {
                return Ok();
            }

            //Get Document Updated.
            transferRequest.GetByKey(value.DocEntry);

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

            return Ok();
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

    }
}
