using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class OrderController : ControllerBase
    {
        // GET: api/Order
        // Todas Las Ordenes
        [HttpGet]
        public async Task<IActionResult> Get()
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

            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From ORDR");
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false)
            {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["ORDR"] = temp["ORDR"][0];
                list.Add(temp);
                items.Browser.MoveNext();
            }
            return Ok(list);
        }


        // GET: api/Order/CRMList
        // Todas las Ordernes - Encabezado para lista CRM
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
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""CardName"",
                    contact.""CardFName"",
                    person.""SlpName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }

        // GET: api/Order/CRMList
        // Todas las Ordernes - Encabezado para lista CRM
        [HttpGet("CRMList/Sucursal/{id}")]
        public async Task<IActionResult> GetCRMSucursalList(string id)
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
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""CardName"",
                    contact.""CardFName"",
                    person.""SlpName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""
                Where warehouse.""WhsCode"" = '" + id +"'");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }


        // GET: api/Order/CRMList/Contact/C00000001
        // Todas las Ordernes - Encabezado para lista CRM filtrado por cliente
        [HttpGet("CRMList/Contact/{id}")]
        public async Task<IActionResult> GetCRMListCLient(string id)
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
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""Series"",
                    ord.""SlpCode"",
                    ord.""CardName"",
                    person.""SlpName"",
                    warehouse.""WhsCode"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                Where ord.""CardCode"" = '"+ id + "'");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }


        // GET: api/order/list
        // Ordenes Filtradas por dia
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

            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From ORDR Where \"DocDate\" = '" + date + "'");
            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false)
            {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["ORDR"] = temp["ORDR"][0];
                temp["RDR4"]?.Parent.Remove();
                temp["RDR12"]?.Parent.Remove();
                list.Add(temp);
                items.Browser.MoveNext();
            }

            return Ok(list);
        }

        // GET: api/Order/5
        // Orden Detalle
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

            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            if (items.GetByKey(id))
            {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["ORDR"] = temp["ORDR"][0];

                oRecSet.DoQuery(@"
                Select
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""Series"" = '" + temp["ORDR"]["Series"] + "'");

                oRecSet.MoveFirst();
                JToken series = context.XMLTOJSON(oRecSet.GetAsXML());
                temp["WHS"] = series["OWHS"][0];

                oRecSet.DoQuery("Select * From OSLP Where \"SlpCode\" = '" + temp["ORDR"]["SlpCode"] + "'");
                oRecSet.MoveFirst();
                JToken vendedor = context.XMLTOJSON(oRecSet.GetAsXML());
                temp["SELLER"] = vendedor["OSLP"][0];


                oRecSet.DoQuery("Select \"CardCode\", \"CardName\", \"Currency\", \"CardFName\"  From OCRD Where \"CardCode\" = '" + temp["ORDR"]["CardCode"] +"'");
                oRecSet.MoveFirst();
                temp["contact"] = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0];

                return Ok(temp);
            }
            return NotFound("No Existe Documento");
        }


        // GET: api/Order/Delivery/5
        // Orden Con informacion extra para la entrega
        [HttpGet("Delivery/{id}")]
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
                    ""CardName"",
                    ""CardCode""
                From ORDR WHERE ""DocNum"" = " + id);

            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }

            JToken order = context.XMLTOJSON(oRecSet.GetAsXML());
            order["AdmInfo"]?.Parent.Remove();
            order["ORDR"] = order["ORDR"][0];

            if (order["ORDR"]["DocStatus"].ToString() != "O")
            {
                return BadRequest("Documento Cerrado");
            }

            oRecSet.DoQuery(@"
                Select
                    ""LineStatus"",
                    ""LineNum"",
                    ""ItemCode"",
                    ""Dscription"",
                    ""UomEntry"",
                    ""WhsCode"",
                    ""UomCode"",
                    ""OpenQty""
                From RDR1 WHERE ""DocEntry"" = " + order["ORDR"]["DocEntry"]);

            order["RDR1"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            foreach (var pro in order["RDR1"])
            {
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

            return Ok(order);
        }

        public JToken limiteCredito(string CardCode, string Series, SAPContext context)
        {
            JToken result;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"CALL ""ValidaCreditoMXM"" ('" + CardCode + "','" + Series + "',0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null)
            {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoMXM'}");
            }

            oRecSet.DoQuery(@"CALL ""ValidaCreditoENS"" ('" + CardCode + "','" + Series + "',0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null)
            {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoENS'}");
            }

            oRecSet.DoQuery(@"CALL ""ValidaCreditoTJ"" ('" + CardCode + "','" + Series + "',0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null)
            {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoTJ'}");
            }

            oRecSet.DoQuery(@"CALL ""ValidaCreditoSLR"" ('" + CardCode + "','" + Series + "',0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null)
            {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoSLR'}");
            }
            return JObject.Parse(@"{ RESULT: 'False', AUTH: ''}");

        }

        public JToken facturasPendientes(string CardCode, string Series, SAPContext context)
        {
            JToken result;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                SELECT 'True' as Result, 'FacturasVencidasMXM' as Auth
                FROM Dummy
                WHERE '" + CardCode + @"' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE)
                AND  '" + Series + @"' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 1))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty)
            {
                return result;
            }
            
            oRecSet.DoQuery(@"
                SELECT 'True' as Result, 'FacturasVencidasENS' as Auth
                FROM Dummy
                WHERE '" + CardCode + @"' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE)
                AND  '" + Series + @"' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 4))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty)
            {
                return result;
            }

            oRecSet.DoQuery(@"
                SELECT 'True' as Result, 'FacturasVencidasTJ' as Auth
                FROM Dummy
                WHERE '" + CardCode + @"' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE)
                AND  '" + Series + @"' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 2))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty)
            {
                return result;
            }

            oRecSet.DoQuery(@"
                SELECT 'True' as Result, 'FacturasVencidasSLR' as Auth
                FROM Dummy
                WHERE '" + CardCode + @"' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE)
                AND  '" + Series + @"' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 3))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty)
            {
                return result;
            }
            return JObject.Parse(@"{ RESULT: 'False', AUTH: ''}");
        }

        public List<JToken> auth(string CardCode, string Series, SAPContext context)
        {
            List <JToken> result = new List<JToken>();
            JToken resultfact = facturasPendientes(CardCode, Series, context);
            JToken resultcredit = limiteCredito(CardCode, Series, context);
            result.Add(resultfact);
            result.Add(resultcredit);
            return result;
        }

        // POST: api/Order
        // Creacion de Orden
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Order value)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (!context.oCompany.Connected) {
                int code = context.oCompany.Connect();
                if (code != 0) {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }

            if(value.auth == 0) {
                List<JToken> resultAuth = auth(value.cardcode, value.series.ToString(), context);
                if (resultAuth[0]["RESULT"].ToString() == "True" || resultAuth[1]["RESULT"].ToString() == "True") {
                    return Conflict(resultAuth);
                }
            }

            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            SAPbobsCOM.Items items = (SAPbobsCOM.Items)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems);
            SAPbobsCOM.BusinessPartners contact = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);

            oRecSet.DoQuery(@"
                Select
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 17 AND serie.""Series"" = " + value.series);
            oRecSet.MoveFirst();
            string warehouse = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"][0]["WhsCode"].ToString();

            order.CardCode = value.cardcode;
            order.Series = value.series;
            order.DocCurrency = value.currency;
            order.DocDueDate = DateTime.Now.AddDays(1);
            order.PaymentGroupCode = value.payment;

            if (contact.GetByKey(value.cardcode)) {
                String temp = (String)contact.UserFields.Fields.Item("U_B1SYS_MainUsage").Value;
                if (temp != String.Empty) {
                    order.UserFields.Fields.Item("U_SO1_02USOCFDI").Value = temp;
                }
                temp = (String)contact.UserFields.Fields.Item("U_IL_MetPago").Value;
                if (temp != String.Empty) {
                    order.UserFields.Fields.Item("U_SO1_02METODOPAGO").Value = temp;
                }
                temp = (String)contact.UserFields.Fields.Item("U_IL_ForPago").Value;
                if (temp != String.Empty) {
                    order.UserFields.Fields.Item("U_SO1_02FORMAPAGO").Value = temp;
                }
            } else {
                string error = context.oCompany.GetLastErrorDescription();
                return BadRequest(new { error });
            }

            for (int i = 0; i < value.rows.Count; i++) {

                order.Lines.ItemCode = value.rows[i].code;
                order.Lines.WarehouseCode = warehouse;

                items.GetByKey(value.rows[i].code);

                for (int j = 0; j < items.PriceList.Count; j++) {
                    items.PriceList.SetCurrentLine(j);
                    if(items.PriceList.PriceList == 2) {
                        if (value.rows[i].uom == -2) {
                            order.Lines.UnitPrice = items.PriceList.Price;
                        } else {
                            order.Lines.UnitPrice = items.PriceList.Price * value.rows[i].equivalentePV;
                        }
                        order.Lines.Currency = items.PriceList.Currency;
                        break;
                    }
                }

                if (value.rows[i].uom == -2) {
                    order.Lines.UoMEntry = 6;
                    order.Lines.UserFields.Fields.Item("U_CjsPsVr").Value = value.rows[i].quantity;
                    order.Lines.Quantity = value.rows[i].quantity * value.rows[i].equivalentePV;
                } else {
                    order.Lines.Quantity = value.rows[i].quantity;
                    order.Lines.UoMEntry = value.rows[i].uom;
                }

                order.Lines.Add();
            }
            
            order.Comments = value.comments;
            int result = order.Add();
            if (result == 0) {
                string objtype = context.oCompany.GetNewObjectType();
                if (objtype == "112") {
                    return Ok(new { value = "Borrador" });
                }
                return Ok(new { value = context.oCompany.GetNewObjectKey() });
            } else {
                string error = context.oCompany.GetLastErrorDescription();
                return BadRequest(new { error });
            }
        }

        // PUT: api/Order/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateOrder value)
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

            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Items items = (SAPbobsCOM.Items)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            

            if (order.GetByKey(id))
            {
                oRecSet.DoQuery(@"
                    Select
                        warehouse.""WhsName"",
                        warehouse.""WhsCode"",
                        serie.""Series""
                    From OWHS warehouse
                    LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                    Where serie.""Series"" = '" + order.Series + "'");
                oRecSet.MoveFirst();
                string warehouse  = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"][0]["WhsCode"].ToString();
                order.Lines.Add();
                for (int i = 0; i < value.newProducts.Count; i++)
                {
                    order.Lines.ItemCode = value.newProducts[i].code;
                    order.Lines.WarehouseCode = warehouse;

                    items.GetByKey(value.newProducts[i].code);

                    for (int j = 0; j < items.PriceList.Count; j++)
                    {
                        items.PriceList.SetCurrentLine(j);
                        if (items.PriceList.PriceList == 2)
                        {
                            if (value.newProducts[i].uom == -2)
                            {
                                order.Lines.UnitPrice = items.PriceList.Price;
                            }
                            else
                            {
                                order.Lines.UnitPrice = items.PriceList.Price * value.newProducts[i].equivalentePV;
                            }
                            order.Lines.Currency = items.PriceList.Currency;
                            break;
                        }
                    }

                    if (value.newProducts[i].uom == -2)
                    {
                        order.Lines.UoMEntry = 6;
                        order.Lines.UserFields.Fields.Item("U_CjsPsVr").Value = value.newProducts[i].quantity;
                        order.Lines.Quantity = value.newProducts[i].quantity * value.newProducts[i].equivalentePV;
                    }
                    else
                    {
                        order.Lines.Quantity = value.newProducts[i].quantity;
                        order.Lines.UoMEntry = value.newProducts[i].uom;
                    }

                    order.Lines.Add();
                }
                

                for (int i = 0; i < value.ProductsChanged.Count; i++)
                {
                    order.Lines.SetCurrentLine(value.ProductsChanged[i].LineNum);
                    if (order.Lines.Quantity != value.ProductsChanged[i].quantity)
                    {
                        order.Lines.Quantity = value.ProductsChanged[i].quantity;
                    }

                    if (order.Lines.UoMEntry != value.ProductsChanged[i].uom)
                    {
                        order.Lines.UoMEntry = value.ProductsChanged[i].uom;
                        items.GetByKey(order.Lines.ItemCode);
                        for (int j = 0; j < items.PriceList.Count; j++)
                        {
                            items.PriceList.SetCurrentLine(j);
                            if (items.PriceList.PriceList == 2)
                            {
                                order.Lines.UnitPrice = items.PriceList.Price * value.ProductsChanged[i].equivalentePV;
                                order.Lines.Currency = items.PriceList.Currency;
                                break;
                            }
                        }
                    }
                }
                
                int result = order.Update();
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

        //// DELETE: api/ApiWithActions/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
