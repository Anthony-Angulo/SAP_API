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
    public class InventoryTransferController : ControllerBase
    {
        //// GET: api/InventoryTransfer
        //[HttpGet]
        //public async Task<IActionResult> Get()
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

        //    SAPbobsCOM.StockTransfer items = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oStockTransfer);
        //    SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
        //    List<Object> list = new List<Object>();

        //    oRecSet.DoQuery("Select * From OWTR");
        //    items.Browser.Recordset = oRecSet;
        //    items.Browser.MoveFirst();

        //    while (items.Browser.EoF == false)
        //    {
        //        JToken temp = context.XMLTOJSON(items.GetAsXML());
        //        list.Add(temp);
        //        items.Browser.MoveNext();
        //    }
        //    return Ok(list);
        //}

        // GET: api/InventoryTransfer/list
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
                From OWTR Where ""DocDate"" = '" + date + "'");

            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }

            JToken tranferList = context.XMLTOJSON(oRecSet.GetAsXML())["OWTR"];

            return Ok(tranferList);
        }

        // GET: api/InventoryTransfer/5
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

            SAPbobsCOM.StockTransfer transfer = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oStockTransfer);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery("Select * From OWTR WHERE \"DocNum\" = " + id);
            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
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

        // POST: api/InventoryTransfer
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Transfer value)
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
                        //    if (request.Lines.RemainingOpenQuantity != 0)
                        //    {
                        //        request.Lines.Quantity = request.Lines.Quantity - request.Lines.RemainingOpenQuantity;
                        //    }
                        //}

                        //int result3 = request.Update();
                        //if (result3 != 0)
                        //{
                        //    string error = context.oCompany.GetLastErrorDescription();
                        //    return BadRequest(new {id = 1,  error });
                        //}

                        SAPbobsCOM.StockTransfer newRequest = (SAPbobsCOM.StockTransfer)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryTransferRequest);

                        newRequest.FromWarehouse = request.FromWarehouse;
                        newRequest.ToWarehouse = request.ToWarehouse;
                        newRequest.Series = request.Series;

                        newRequest.UserFields.Fields.Item("U_SO1_02NUMRECEPCION").Value = request.DocNum.ToString();

                        for (int i = 0; i < value.products.Count; i++)
                        {
                            request.Lines.SetCurrentLine(value.products[i].Line);
                            newRequest.Lines.ItemCode = value.products[i].ItemCode;
                            newRequest.Lines.UoMEntry = request.Lines.UoMEntry;
                            newRequest.Lines.UseBaseUnits = request.Lines.UseBaseUnits;
                            newRequest.Lines.Quantity = value.products[i].Count;
                            newRequest.Lines.FromWarehouseCode = request.Lines.WarehouseCode;
                            newRequest.Lines.WarehouseCode = request.ToWarehouse;
                            newRequest.Lines.Add();
                        }
                        int result2 = newRequest.Add();
                        if (result2 != 0)
                        {
                            string error = context.oCompany.GetLastErrorDescription();
                            return BadRequest(new { id = 2, error , value ,va = context.XMLTOJSON(newRequest.GetAsXML())});
                        }
                        return Ok(context.oCompany.GetNewObjectKey());
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
