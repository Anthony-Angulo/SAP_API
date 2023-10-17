using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using SAP_API.Entities;
using SAP_API.Models;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesBurnedController : ControllerBase
    {
        public ApplicationDbContext _context { get; set; }

        public InvoicesBurnedController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            var facturasQuemadas = new List<FacturasNuevas>();

            facturasQuemadas = (from facturas in _context.FacBurn.AsEnumerable()
                                where facturas.DateBurn.Date >= DateTime.Now.Date
                                orderby facturas.Series
                                select new FacturasNuevas(
facturas.DocNum,
(ApplicationDbContext.Series)facturas.Series,
facturas.DateBurn,
facturas.DocDate,
facturas.UserName
                                )).ToList();
            Recordset oRecSet = (Recordset)context.oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            string query = @"SELECT T2.""Remark"" as ""Sucursal"",T2.""SeriesName"" as ""Serie"",T0.""DocNum"" as ""Folio"",TO_NVARCHAR(T0.""DocDate"", 'DD-MM-YYYY') as ""Fecha"" FROM  ""OINV"" T0,""NNM1"" T2 WHERE T0.""DocDate"" >=  CURRENT_DATE AND T0.""CANCELED""!='C' and  T0.""GroupNum"" in ('8','19') AND T2.""Series""=T0.""Series"" order by T2.""Remark""";
            oRecSet.DoQuery(query);
            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }
            else
            {

                List<FacturasSap> facturasSaps = context.XMLTOJSON(oRecSet.GetAsXML())["OINV"].ToObject<List<FacturasSap>>();
                List<FacturasSap> facturasNoQuemadas = new List<FacturasSap>();
                if (facturasSaps == null) { }
                else
                {
                    facturasNoQuemadas = facturasSaps.Where(f1 => facturasQuemadas.All(f2 => f2.NumeroDeDocumento != f1.Folio)).ToList();

                }

                return Ok(facturasNoQuemadas);
            }
        }
        [HttpGet("Sucursal")]

        public IActionResult GetBySucursal([FromQuery] int Sucursal)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            var facturasQuemadas = new List<FacturasNuevas>();

            facturasQuemadas = (from facturas in _context.FacBurn.AsEnumerable()
                                where facturas.DateBurn.Date >= DateTime.Now.Date && facturas.Series == Sucursal
                                orderby facturas.Series
                                select new FacturasNuevas(
facturas.DocNum,
(ApplicationDbContext.Series)facturas.Series,
facturas.DateBurn,
facturas.DocDate,
facturas.UserName
                                )).ToList();
            Recordset oRecSet = (Recordset)context.oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            string query = $@"SELECT T2.""Remark"" as ""Sucursal"",T2.""SeriesName"" as ""Serie"",T0.""DocNum"" as ""Folio"",TO_NVARCHAR(T0.""DocDate"", 'DD-MM-YYYY') as ""Fecha"" FROM  ""OINV"" T0,""NNM1"" T2 WHERE T0.""DocDate"" >=  CURRENT_DATE AND T0.""CANCELED""!='C' and  T0.""GroupNum"" in ('8','19') AND T2.""Series""=T0.""Series"" AND T2.""Series""='{Sucursal}' order by T2.""Remark""";
            oRecSet.DoQuery(query);
            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }
            else
            {

                List<FacturasSap> facturasSaps = context.XMLTOJSON(oRecSet.GetAsXML())["OINV"].ToObject<List<FacturasSap>>();
                List<FacturasSap> facturasNoQuemadas = new List<FacturasSap>();
                if (facturasSaps == null) { }
                else
                {

                    facturasNoQuemadas = facturasSaps.Where(f1 => facturasQuemadas.All(f2 => f2.NumeroDeDocumento != f1.Folio)).ToList();

                }

                return Ok(facturasNoQuemadas);
            }
        }
        [HttpGet("GetByDate")]
        public IActionResult GetByDateSucursal([FromQuery] string InitialDate, [FromQuery] string FinalDate, [FromQuery] int Sucursal)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            CultureInfo provider = CultureInfo.InvariantCulture;
            var facturasQuemadas = new List<FacturasNuevas>();
            DateTime FechaInicial = DateTime.ParseExact(InitialDate, "dd/MM/yyyy", provider);
            DateTime FechaFinal = DateTime.ParseExact(FinalDate, "dd/MM/yyyy", provider);

            facturasQuemadas = (from facturas in _context.FacBurn.AsEnumerable()
                                where (facturas.DateBurn.Date >= FechaInicial && facturas.DateBurn.Date <= FechaFinal)
                                && facturas.Series == Sucursal
                                orderby facturas.Series
                                select new FacturasNuevas(
facturas.DocNum,
(ApplicationDbContext.Series)facturas.Series,
facturas.DateBurn,
facturas.DocDate,
facturas.UserName
                                )).ToList();
            Recordset oRecSet = (Recordset)context.oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            string query = $@"SELECT T2.""Remark"" as ""Sucursal"",T2.""SeriesName"" as ""Serie"",
T0.""DocNum"" as ""Folio"",
TO_NVARCHAR(T0.""DocDate"", 'DD-MM-YYYY') as ""Fecha"" 
FROM  ""OINV"" T0,""NNM1"" T2 
WHERE 
T0.""DocDate"" >=  TO_DATE('{InitialDate.Replace("/", "")}','DDMMYYYY')
AND T0.""DocDate"" <=  TO_DATE('{FinalDate.Replace("/", "")}','DDMMYYYY')
AND T0.""CANCELED""!='C' 
AND T0.""GroupNum"" in ('8','19') 
AND T2.""Series""=T0.""Series"" 
AND T2.""Series""='{Sucursal}'
order by T2.""Remark""";
            oRecSet.DoQuery(query);
            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }
            else
            {

                List<FacturasSap> facturasSaps = context.XMLTOJSON(oRecSet.GetAsXML())["OINV"].ToObject<List<FacturasSap>>();
                List<FacturasSap> facturasNoQuemadas = new List<FacturasSap>();
                if (facturasSaps == null) { }
                else
                {
                    facturasNoQuemadas = facturasSaps.Where(f1 => facturasQuemadas.All(f2 => f2.NumeroDeDocumento != f1.Folio)).ToList();

                }

                return Ok(facturasNoQuemadas.OrderBy(x => x.Fecha));
            }
        }
    }

    internal class FacturasSap
    {
        public string Sucursal { get; set; }
        public string Serie { get; set; }
        public int Folio { get; set; }
        public string Fecha { get; set; }

        public override string ToString()
        {
            return $"La factura con numero: {this.Folio} no esta quemada ";
        }
    }
}


