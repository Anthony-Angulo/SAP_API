using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Entities;
using SAP_API.Models;
using System;
using System.Linq;


namespace SAP_API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContactController : ControllerBase {

        private readonly ApplicationDbContext _context;
        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Get Client List to CRM web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>ClientSearchResponse</returns>
        /// <response code="200">ClientSearchResponse(SearchResponse)</response>
        // POST: api/Contact/Clients/Search
        [ProducesResponseType(typeof(ClientSearchResponse), StatusCodes.Status200OK)]
        [Authorize]
        [HttpPost("Clients/Search")]
        public async Task<IActionResult> GetClients([FromBody] SearchRequest request)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            List<string> where = new List<string>();

            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(\"CardCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(\"CardFName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(\"CardName\") Like LOWER('%{request.columns[2].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY \"CardCode\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY \"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY \"CardName\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY \"CardCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select ""CardCode"", ""CardName"", ""CardFName""
                From OCRD Where ""CardType"" = 'C' AND ""CardCode"" NOT LIKE '%-D'";

            if (where.Count != 0) {
                query += " AND " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"].ToObject<List<ClientSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                 From OCRD Where ""CardType"" = 'C' AND ""CardCode"" NOT LIKE '%-D' ";

            if (where.Count != 0) {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0]["COUNT"].ToObject<int>();

            ClientSearchResponse respose = new ClientSearchResponse {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };

            return Ok(respose);
        }

        /// <summary>
        /// Get Provider List to CRM web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>ProviderSearchResponse</returns>
        /// <response code="200">ProviderSearchResponse(SearchResponse)</response>
        // POST: api/Contact/Providers/Search
        [ProducesResponseType(typeof(ProviderSearchResponse), StatusCodes.Status200OK)]
        [HttpPost("Providers/Search")]
        public async Task<IActionResult> GetProviders([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();

            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(\"CardCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(\"CardName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(\"CardFName\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty) {
                where.Add($"LOWER(\"Currency\") Like LOWER('%{request.columns[3].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY \"CardCode\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY \"CardName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY \"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 3) {
                orderby = $" ORDER BY \"Currency\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY \"CardCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select ""CardCode"", ""CardName"", ""CardFName"", ""Currency""
                From OCRD Where ""CardType"" = 'S' ";

            if (where.Count != 0) {
                query += " AND " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"].ToObject<List<ProviderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                 From OCRD Where ""CardType"" = 'S' ";

            if (where.Count != 0) {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0]["COUNT"].ToObject<int>();

            ProviderSearchResponse respose = new ProviderSearchResponse {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };

            return Ok(respose);
        }

        /// <summary>
        /// Get Client Info to CRM 
        /// </summary>
        /// <param name="CardCode">CardCode. A String that serve as Client identifier.</param>
        /// <returns>A Client Basic Info</returns>
        /// <response code="200">Returns Client Info</response>
        /// <response code="204">No Client Found</response>
        // GET: api/Contact/CRMClientToSell/:CardCode
        [ProducesResponseType(typeof(ContactToSell), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("CRMClientToSell/{CardCode}")]
        public async Task<IActionResult> GetCRMClientToSell(string CardCode) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    contact.""ListNum"",
                    paymentTerm.""GroupNum"",
                    paymentTerm.""PymntGroup"",
                    paymentMethod.""PayMethCod"",
                    paymentMethod.""Descript"",
                    ""SlpName"",
                    ""Balance"",
                    ""ListName""
                From OCRD contact
                JOIN OSLP seller ON contact.""SlpCode"" = seller.""SlpCode""
                JOIN OCTG paymentTerm ON paymentTerm.""GroupNum"" = contact.""GroupNum""
                JOIN OPLN priceList ON priceList.""ListNum"" = contact.""ListNum""
                LEFT JOIN OPYM paymentMethod ON paymentMethod.""PayMethCod"" = contact.""PymCode""
                Where ""CardCode"" = '" + CardCode + "'");

            if (oRecSet.RecordCount == 0) {
                return NoContent();
            }

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0];

            oRecSet.DoQuery(@"
                Select
                    paymentMethodCardCode.""PymCode"",
                    paymentMethod.""Descript""
                From CRD2 paymentMethodCardCode
                JOIN OPYM paymentMethod ON paymentMethod.""PayMethCod"" = paymentMethodCardCode.""PymCode""
                Where ""CardCode"" = '" + CardCode  + "'");

            temp["PaymentMethods"] = context.XMLTOJSON(oRecSet.GetAsXML())["CRD2"];

            ContactToSell ContactOutput = temp.ToObject<ContactToSell>();

            //Force Garbage Collector. Recommendation by InterLatin Dude. SDK Problem with memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(ContactOutput);
        }

        // TODO: Class To Serialize Result.
        /// <summary>
        /// Get Provider Info to CRM 
        /// </summary>
        /// <param name="CardCode">CardCode. A String that serve as Provider identifier.</param>
        /// <returns>A Provider Basic Info</returns>
        /// <response code="200">Returns Provider Info</response>
        /// <response code="204">No Provider Found</response>
        // GET: api/Contact/CRMProviderToBuy/:CardCode
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("CRMProviderToBuy/{CardCode}")]
        public async Task<IActionResult> GetCRMProviderToBuy(string CardCode) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    ""Currency""
                From OCRD
                Where ""CardCode"" = '" + CardCode + "'");

            if (oRecSet.RecordCount == 0) {
                return NoContent();
            }

            JToken contact = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0];

            //Force Garbage Collector. Recommendation by InterLatin Dude. SDK Problem with memory.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(contact);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        [HttpGet("CRM/{id}")]
        public async Task<IActionResult> GetCRMID(string id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.BusinessPartners items = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);
            SAPbobsCOM.SalesPersons seller = (SAPbobsCOM.SalesPersons)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oSalesPersons);
            SAPbobsCOM.PaymentTermsTypes payment = (SAPbobsCOM.PaymentTermsTypes)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPaymentTermsTypes);
            SAPbobsCOM.UserTable sboTable = (SAPbobsCOM.UserTable)context.oCompany.UserTables.Item("SO1_01FORMAPAGO");

            JToken pagos = context.XMLTOJSON(sboTable.GetAsXML())["OCRD"];

            if (items.GetByKey(id)) {

                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["OCRD"] = temp["OCRD"][0];

                if (seller.GetByKey(temp["OCRD"]["SlpCode"].ToObject<int>())) {
                    JToken temp2 = context.XMLTOJSON(seller.GetAsXML());
                    temp["OSLP"] = temp2["OSLP"][0];
                }
                if (payment.GetByKey(temp["OCRD"]["GroupNum"].ToObject<int>())) {
                    JToken temp3 = context.XMLTOJSON(payment.GetAsXML());
                    temp["OCTG"] = temp3["OCTG"][0];
                }

                temp["PAGO"] = pagos;
                return Ok(temp);
            }
            return NotFound("No Existe Contacto");
        }

        // GET: api/Contact/APPCRM/200
        [HttpGet("APPCRM/{id}")]
        public async Task<IActionResult> GetAPPCRM(int id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery($@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    ""Address"",
                    ""ZipCode"",
                    ""Country"",
                    ""Block"",
                    ""GroupNum"",
                    ""ListNum""
                From OCRD employeeSales
                JOIN OHEM employee ON ""SlpCode"" = ""salesPrson""
                Where ""CardType"" = 'C' AND ""empID"" = {id} AND ""CardCode"" NOT LIKE '%-D'");

            if (oRecSet.RecordCount == 0) {
                return Ok(new List<string>());
            }

            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(contacts);
        }

        // GET: api/Contact/APPCRM
        [HttpGet("APPCRM")]
        public async Task<IActionResult> GetAPPCRMs() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    ""Address"",
                    ""ZipCode"",
                    ""Country"",
                    ""Block"",
                    ""GroupNum"",
                    ""ListNum""
                    From OCRD Where ""CardType"" = 'C' AND ""CardCode"" NOT LIKE '%-D'");

            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(contacts);
        }

        [HttpGet("{CardCode}")]
        public async Task<IActionResult> Get(string CardCode) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.BusinessPartners Bp = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);

            if (!Bp.GetByKey(CardCode)) {
                return NoContent();
            }
            JToken output = context.XMLTOJSON(Bp.GetAsXML());
            return Ok(output);

        }
        [HttpPost("ProductosPreferidos/{CardCode}")]
        public IActionResult AddProductsCliente([FromRoute] string CardCode, [FromBody] List<ProductosPreferidos> Productos)
        {
            List<ClientsProducts> ProductosCliente = _context.Clientes_Productos.Where(x => x.ClientCode == CardCode).ToList();
            Productos = Productos.Where(x => ProductosCliente.Find(y => y.ClientCode == CardCode && x.ItemCode == y.ProductCode) == null).ToList();
            List<ClientsProducts> ProductosParaAgregar=new List<ClientsProducts>();
            foreach (var item in Productos)
            {
                if (item.status == 0)
                    item.status = 3;
                ProductosParaAgregar.Add(new ClientsProducts() { ClientCode = CardCode, ProductCode = item.ItemCode, ProductDescription = item.ItemName, ProductGroup = item.ItmsGrpNam, status = item.status });
               
            }
            try
            {
                _context.Clientes_Productos.AddRange(ProductosParaAgregar);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException);
            }

            return Ok();
        }
        [HttpGet("ProductosBorrarMasivo/{CardCode}")]
        public IActionResult BorrarProductsCliente([FromRoute] string CardCode)
        {
            var ProductosCliente = _context.Clientes_Productos.Where(x => x.ClientCode == CardCode);
            _context.RemoveRange(ProductosCliente);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException);
            }
            return Ok();
        }
        [HttpPost("ProductosPreferidosBorrar/{CardCode}")]
        public IActionResult BorrarProductsCliente([FromRoute] string CardCode, [FromBody] List<ProductosPreferidos> productos)
        {
            foreach (var item in productos)
            {
                var Producto = _context.Clientes_Productos.Where(x => x.ClientCode == CardCode && x.ProductCode == item.ItemCode).FirstOrDefault();
                if (Producto != null)
                    _context.Clientes_Productos.Remove(Producto);
            }
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException);
            }
            return Ok();
        }
        [HttpGet("Productos/{CardCode}")]
        public IActionResult GetProductClienteAsync([FromRoute] string CardCode)
        {
            var result = _context.Clientes_Productos.Where(x => x.ClientCode == CardCode).Select(x => new { id = x.idClientes_Productos, ItemCode = x.ProductCode, ItemName = x.ProductDescription, ItmsGrpNam = x.ProductGroup, Combined = x.ProductCode + " | " + x.ProductDescription, status = x.status });
            return Ok(result);
        }
        [HttpGet("Productos/{CardCode}/{Grupo}")]
        public IActionResult GetProductClienteAgrupadoAsync([FromRoute] string CardCode, [FromRoute] string Grupo)
        {
            if (Grupo != "TODOS")
            {
                var result = _context.Clientes_Productos.OrderByDescending(x => x.status).Where(x => x.ClientCode == CardCode && x.ProductGroup == Grupo).Select(x => new { id = x.idClientes_Productos, ItemCode = x.ProductCode, ItemName = x.ProductDescription, ItmsGrpNam = x.ProductGroup, Combined = x.ProductCode + " | " + x.ProductDescription, status = x.status });
                return Ok(result);
            }
            else
            {
                var result = _context.Clientes_Productos.OrderByDescending(x => x.status).Where(x => x.ClientCode == CardCode).Select(x => new { id = x.idClientes_Productos, ItemCode = x.ProductCode, ItemName = x.ProductDescription, ItmsGrpNam = x.ProductGroup, Combined = x.ProductCode + " | " + x.ProductDescription, status = x.status });
                return Ok(result);
            }
        }
        [HttpGet("ProductosVenta/{CardCode}")]
        public IActionResult GetProductClienteVentaAsync([FromRoute] string CardCode)
        {
            List<ProductosListBox> productosLists = new List<ProductosListBox>();
            var result = _context.Clientes_Productos.Where(x => x.ClientCode == CardCode)
                .GroupBy(x => new { x.ProductGroup }).
                Select(g => new { label = g.Key.ProductGroup }).ToList();
            foreach (var item in result)
            {
                var Productos = _context.Clientes_Productos.Where(x => x.ClientCode == CardCode && x.ProductGroup == item.label).OrderByDescending(x => x.status)
                    .Select(x => new ProductoParaCliente { idClientes_Productos = x.idClientes_Productos, label = x.ProductCode + " | " + x.ProductDescription, value = x.ProductCode, ItemCode = x.ProductCode, ItemName = x.ProductDescription, ItmsGrpNam = x.ProductGroup, status = x.status }).ToList<ProductoParaCliente>();
                productosLists.Add(new ProductosListBox() { label = item.label, items = Productos });

            }
            return Ok(productosLists);
        }
        [HttpGet("ProductosVentaPreferidos/{CardCode}")]
        public IActionResult GetProductClienteVentaPreferidosAsync([FromRoute] string CardCode)
        {
            List<ProductosListBox> productosLists = new List<ProductosListBox>();
            var result = _context.Clientes_Productos.Where(x => x.ClientCode == CardCode)
                .GroupBy(x => new { x.ProductGroup }).
                Select(g => new { label = g.Key.ProductGroup }).ToList();
            foreach (var item in result)
            {
                var Productos = _context.Clientes_Productos.Where(x => x.ClientCode == CardCode && x.ProductGroup == item.label && x.status == 4)
                    .Select(x => new ProductoParaCliente { idClientes_Productos = x.idClientes_Productos, label = x.ProductCode + " | " + x.ProductDescription, value = x.ProductCode, ItemCode = x.ProductCode, ItemName = x.ProductDescription, ItmsGrpNam = x.ProductGroup }).ToList<ProductoParaCliente>();
                productosLists.Add(new ProductosListBox() { label = item.label, items = Productos });

            }
            return Ok(productosLists);
        }
        [HttpGet("CambiarStatusProducto/{id}/{Status}")]
        public IActionResult CambiarStatusProducto([FromRoute] int id,
                                                   [FromRoute] int Status)
        {
            ClientsProducts ClientsProducts = _context.Clientes_Productos.Where(x => x.idClientes_Productos == id).FirstOrDefault();

            if (ClientsProducts == null)
                return NotFound();
            ClientsProducts.status = Status;
            try
            {
                _context.SaveChanges();
                return Ok();

            }
            catch (Exception)
            {
                return BadRequest();
                throw;
            }
        }
        [HttpPost("CargaMasivaProductos")]
        public IActionResult CargaMasivaProductos([FromBody] List<ProductosMasivo> productosMasivos)
        {
            List<ClientsProducts> products = new List<ClientsProducts>();
            List<ProductosMasivo> ListadoFiltrado = new List<ProductosMasivo>();
            products = _context.Clientes_Productos.Where(x => productosMasivos.Find(y => y.CardCode == x.ClientCode && y.ItemCode == x.ProductCode) != null).ToList();
            ListadoFiltrado = productosMasivos.Where(x => products.Find(y => y.ClientCode == x.CardCode && y.ProductCode == x.ItemCode) == null).ToList();

            List<ClientsProducts> productsCambio = ListadoFiltrado.Select(product => new ClientsProducts
            {
                ClientCode = product.CardCode,
                ProductCode = product.ItemCode,
                ProductDescription = product.ItemName,
                ProductGroup = product.ItmsGrpNam,
                status = product.status == 0 ? 3 : product.status
            }).ToList();
            foreach (ClientsProducts item in products)
            {
                var ProductoMasivo = productosMasivos.Find(x => x.ItemCode == item.ProductCode && x.CardCode == item.ClientCode);
                if (ProductoMasivo == null) { }
                else
                {
                    if (item.status != ProductoMasivo.status) item.status = ProductoMasivo.status;
                }
            }
            _context.Clientes_Productos.AddRange(productsCambio);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {

                return BadRequest(ex);
            }
            return Ok();

        }
        [HttpGet("ProductosTrimestre/{CardCode}")]
        public IActionResult GetProductosTrimestre([FromRoute] string CardCode)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            JToken invoice;
            oRecSet.DoQuery($@"
               select T0.""ItemCode"" from ""INV1"" T0,
                 ""OINV"" T1 where 
                T0.""DocDate"" >= ADD_DAYS(TO_DATE(CURRENT_DATE, 'YYYY-MM-DD'), -90)
                AND T0.""DocDate"" <= CURRENT_DATE
                and T0.""BaseCard"" = '{CardCode}'
                AND T1.""CANCELED"" != 'C'
                AND T0.""DocEntry"" = T1.""DocEntry""
                GROUP BY T0.""ItemCode"",T0.""BaseCard""
                ORDER BY T0.""BaseCard""");
            if (oRecSet.RecordCount == 0)
            {
                return Ok();   // Handle no Existing Invoice
            }
            invoice = context.XMLTOJSON(oRecSet.GetAsXML())["INV1"];
            return Ok(invoice);
        }

        public class ProductosMasivo
        {
            public string CardCode { get; set; }

            public string ItemCode { get; set; }

            public string ItemName { get; set; }

            public string ItmsGrpNam { get; set; }

            public int status { get; set; }
        }
        public class ProductosListBox
        {
            public string label { get; set; }

            public List<ProductoParaCliente> items { get; set; }
        }

    }
}
