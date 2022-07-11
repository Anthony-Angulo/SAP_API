using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using SAP_API.Entities;
using SAP_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CorteXYZController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public CorteXYZController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public class Ledger{
            public string NoCorte { get; set; }
            public string Usuario { get; set; }

            public string Sucursal { get; set; }
            public List<Transfer> transfers { get; set; }
        }

        public class Transfer
        {
            public double CantidadEnviada { get; set; }
            public double CantidadReal { get; set; }
            public string Clave { get; set; }

            public DateTime Fecha { get; set; }

            public string Cuenta { get; set; }
        }
        public class TransferError : Transfer { 
        public string error { get; set; }
        }
        [HttpPost("Ledger")]
        public async Task<IActionResult> GetLedgers([FromBody] Ledger Ledger)
       {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            List<TransferError> transferenciasRechazadas = new List<TransferError>();
            List<String> CuentasDolares = _configuration["CuentasDolares"].Split(",").ToList();
            foreach (Transfer item in Ledger.transfers)
            {
                SAPbobsCOM.JournalEntries move = (SAPbobsCOM.JournalEntries)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oJournalEntries);
                move.Memo = $@"Usuario:{Ledger.Usuario}";
                try
                {   string CuentaSucursal = "";
                    if (int.Parse(Ledger.Sucursal.Substring(1)) > 100){
                        CuentaSucursal = @$"110-017-{Ledger.Sucursal.Substring(1)}-{Ledger.Sucursal.Substring(1)}" ;
                    }
                    else
                    {
                        CuentaSucursal = @$"110-017-0{Ledger.Sucursal.Substring(1)}-0{Ledger.Sucursal.Substring(1)}";
                    }
                    if(CuentasDolares.Exists(x=>x==item.Cuenta))
                    {
                        
                        DateTime fecha = item.Fecha;
                        move.Series = 19;
                        move.DueDate = fecha;
                        move.ReferenceDate = fecha;
                        move.TaxDate = fecha;
                        //move.StornoDate
                        //          move.VatDate
                        //Cuenta de cargo
                        move.Lines.ShortName = CuentaSucursal;
                        move.Lines.AccountCode = CuentaSucursal;
                        move.Lines.ContraAccount = item.Cuenta;
                        move.Lines.FCCurrency = "USV";
                        move.Lines.FCCredit = item.CantidadReal;
                        move.Lines.FCDebit= 0;

                        move.Lines.DueDate = fecha;
                        move.Lines.TaxDate = fecha;
                        move.Lines.ReferenceDate1 = fecha;
                        move.Lines.Add();
                        //Cuenta de abono
                        move.Lines.AccountCode = item.Cuenta;
                        move.Lines.ContraAccount = CuentaSucursal;
                        move.Lines.FCCurrency = "USV";
                        move.Lines.FCCredit= 0;
                        move.Lines.FCDebit = item.CantidadEnviada;
                        move.Lines.DueDate = fecha;
                        move.Lines.ReferenceDate1 = fecha;
                        move.Lines.TaxDate = fecha;
                        move.Lines.Add();
                        //Cuenta de abono diferencia
                        move.Lines.AccountCode = "810-200-004-000";
                        move.Lines.ContraAccount = CuentaSucursal;
                        move.Lines.FCCurrency = "USV";

                        if (item.CantidadReal - item.CantidadEnviada >= 0)
                        {
                            move.Lines.FCDebit= Math.Abs(item.CantidadReal - item.CantidadEnviada);
                            move.Lines.FCCredit = 0;
                        }
                        else
                        {
                            move.Lines.FCDebit = 0;
                            move.Lines.FCCredit = Math.Abs(item.CantidadReal - item.CantidadEnviada);
                        }
                        move.Lines.DueDate = fecha;
                        move.Lines.ReferenceDate1 = fecha;
                        move.Lines.TaxDate = fecha;
                        move.Lines.Add();

                    }
                    else
                    {
                        
                        DateTime fecha = item.Fecha;
                        move.Series = 19;
                        move.DueDate = fecha;
                        move.ReferenceDate = fecha;
                        move.TaxDate = fecha;
                        //move.StornoDate
                        //          move.VatDate
                        //Cuenta de cargo
                        move.Lines.ShortName = CuentaSucursal;
                        move.Lines.AccountCode = CuentaSucursal;
                        move.Lines.ContraAccount = item.Cuenta;
                        move.Lines.Credit = item.CantidadReal;
                        move.Lines.Debit = 0;

                        move.Lines.DueDate = fecha;
                        move.Lines.TaxDate = fecha;
                        move.Lines.ReferenceDate1 = fecha;
                        move.Lines.Add();
                        //Cuenta de abono
                        move.Lines.AccountCode = item.Cuenta;
                        move.Lines.ContraAccount = CuentaSucursal;
                        move.Lines.Credit = 0;
                        move.Lines.Debit = item.CantidadEnviada;
                        move.Lines.DueDate = fecha;
                        move.Lines.ReferenceDate1 = fecha;
                        move.Lines.TaxDate = fecha;
                        move.Lines.Add();
                        //Cuenta de abono diferencia
                        move.Lines.AccountCode = "810-200-004-000";
                        move.Lines.ContraAccount = CuentaSucursal;
                        if (item.CantidadReal - item.CantidadEnviada >= 0)
                        {
                            move.Lines.Debit = Math.Abs(item.CantidadReal - item.CantidadEnviada);
                            move.Lines.Credit = 0;
                        }
                        else
                        {
                            move.Lines.Debit = 0;
                            move.Lines.Credit = Math.Abs(item.CantidadReal - item.CantidadEnviada);
                        }
                        move.Lines.DueDate = fecha;
                        move.Lines.ReferenceDate1 = fecha;
                        move.Lines.TaxDate = fecha;
                        move.Lines.Add();

                    }
                    int lRetCode = 0;

                    lRetCode = move.Add();
                    if (lRetCode != 0)
                    {

                        transferenciasRechazadas.Add(new TransferError { Clave = item.Clave, CantidadEnviada = item.CantidadEnviada, CantidadReal = item.CantidadReal, Fecha = item.Fecha, error = context.oCompany.GetLastErrorDescription() });
                    }
                    else
                    {

                    _context.TrasladosVirtuales.Add(
                        new TrasladosVirtuales
                        {
                            Cantidad = item.CantidadEnviada.ToString(),
                            CantidadReal = item.CantidadReal.ToString(),
                            ClaveTransaccion = item.Clave,
                            NoCorte = Ledger.NoCorte,
                            Usuario = Ledger.Usuario,
                            CuentaDestino=item.Cuenta
                        });
                    _context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    transferenciasRechazadas.Add(new TransferError { Clave = item.Clave, CantidadEnviada = item.CantidadEnviada, CantidadReal = item.CantidadReal, Fecha = item.Fecha, error = ex.Message });
                }
              
                            

            }
            if (transferenciasRechazadas.Count == 0)
            {
                return Ok("Transferencias realizas con exito");

            }
            else
            {

                return BadRequest(transferenciasRechazadas);

            }

        }
        [HttpGet("{NoCorte}")]
        public async Task<IActionResult> GetNoCorte([FromRoute] string NoCorte)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //string query2 = $@"SELECT * FROM ""@SO1_01DEVOLUCION"" LIMIT 5";
            string query =$@"


SELECT
  T0.U_SO1_SUCURSAL,
T0.""FdP"",
T0.""Forma_de_Pago"" as ""Forma_de_Pago"",
SUM(T0.""Importe_MXN"") as ""Importe_MXN"",
SUM(T0.""Importe_USV"") as ""Importe_USV"",
SUM(T0.""Total_MXN"") as ""Total_MXN""
FROM
(
SELECT
O.U_SO1_SUCURSAL,
C.U_SO1_CODIGOFP ""FdP"",
FP.""Name"" ""Forma_de_Pago"",
CAST(
CASE
WHEN FP.U_SO1_MONEDA = 'MXN' THEN
C.U_SO1_MONTONETO
ELSE
0
END AS DECIMAL(10, 2)
) ""Importe_MXN"",
CAST(
CASE
WHEN FP.U_SO1_MONEDA = 'USV' THEN
C.U_SO1_MONTOCOBRADO / C.U_SO1_TIPOCAMBIO
ELSE
0
END AS DECIMAL(10, 2)
) ""Importe_USV"",
CAST(
CASE
WHEN FP.U_SO1_MONEDA = 'MXN' THEN
C.U_SO1_MONTONETO
ELSE
C.U_SO1_MONTOCOBRADO
END AS DECIMAL(10, 2)
) ""Total_MXN""
FROM
""@SO1_01VENTACOBRO"" C JOIN
""@SO1_01FORMAPAGO"" FP ON
FP.""Code"" = C.U_SO1_CODIGOFP JOIN
""@SO1_01VENTA"" O ON
O.""Name"" = C.U_SO1_FOLIO AND
O.U_SO1_TIPO IN('CA', 'CR') JOIN
""@SO1_01SUCURSAL"" S ON
S.""Code"" = O.U_SO1_SUCURSAL JOIN
OCRD ON
OCRD.""CardCode"" = O.U_SO1_CLIENTE JOIN
""@SO1_01CORTECAJA"" CC ON
CC.""Name"" = O.U_SO1_FOLIOCORTEX
WHERE
CC.U_SO1_FOLIOCIERREZ = '{NoCorte}'
UNION ALL
--Con esa parte decremento el equivalente en pesos del cambio en dólares
--porque dan el cambio de dólares en pesos cuando reciben dólares.
SELECT
O.U_SO1_SUCURSAL,
'EF' ""FdP"",
'Efectivo' ""Forma_de_Pago"",
CAST(-(C.U_SO1_MONTOCOBRADO - C.U_SO1_MONTONETO) AS DECIMAL(10, 2)) ""Importe_MXN"",
CAST(0 AS DECIMAL(10, 2)) ""Importe_USV"",
CAST(-(C.U_SO1_MONTOCOBRADO - C.U_SO1_MONTONETO) AS DECIMAL(10, 2)) ""Total_MXN""
FROM
""@SO1_01VENTACOBRO"" C JOIN
""@SO1_01FORMAPAGO"" FP ON
FP.""Code"" = C.U_SO1_CODIGOFP JOIN
""@SO1_01VENTA"" O ON
O.""Name"" = C.U_SO1_FOLIO AND
O.U_SO1_TIPO IN('CA', 'CR') JOIN
""@SO1_01SUCURSAL"" S ON
S.""Code"" = O.U_SO1_SUCURSAL JOIN
OCRD ON
OCRD.""CardCode"" = O.U_SO1_CLIENTE JOIN
""@SO1_01CORTECAJA"" CC ON
CC.""Name"" = O.U_SO1_FOLIOCORTEX
WHERE
CC.U_SO1_FOLIOCIERREZ = '{NoCorte}' AND
FP.U_SO1_MONEDA = 'USV'
UNION ALL
SELECT
O.U_SO1_SUCURSAL,
C.U_SO1_CODIGOFP ""FdP"",
FP.""Name"" ""Forma_de_Pago"",
CAST(
CASE
WHEN FP.U_SO1_MONEDA = 'MXN' THEN
- C.U_SO1_MONTONETO
ELSE
0
END AS DECIMAL(10, 2)
) ""Importe_MXN"",
CAST(
CASE
WHEN FP.U_SO1_MONEDA = 'USV' THEN
- C.U_SO1_MONTONETO / C.U_SO1_TIPOCAMBIO
ELSE
0
END AS DECIMAL(10, 2)
) ""Importe_USV"",
CAST(-C.U_SO1_MONTONETO AS DECIMAL(10, 2)) ""Total_MXN""
FROM
""@SO1_01DEVOLUCIONPAG"" C JOIN
""@SO1_01FORMAPAGO"" FP ON
FP.""Code"" = C.U_SO1_CODIGOFP JOIN
""@SO1_01DEVOLUCION"" O ON
O.""Name"" = C.U_SO1_FOLIO JOIN
""@SO1_01SUCURSAL"" S ON
S.""Code"" = O.U_SO1_SUCURSAL JOIN
OCRD ON
OCRD.""CardCode"" = O.U_SO1_CLIENTE JOIN
""@SO1_01CORTECAJA"" CC ON
CC.""Name"" = O.U_SO1_FOLIOCORTEX
WHERE
CC.U_SO1_FOLIOCIERREZ = '{NoCorte}'
) T0
GROUP BY
  T0.U_SO1_SUCURSAL,
T0.""FdP"",
T0.""Forma_de_Pago""";
            oRecSet.DoQuery(query);
            if (oRecSet.RecordCount == 0)
            {
                return NoContent();
            }

            //JToken row = context.XMLTOJSON(oRecSet.GetAsXML());


            try
            {
                //return Ok(context.XMLTOJSON(oRecSet.GetAsXML()));

                return Ok(new { Corte = context.FixedXMLTOJSON(oRecSet.GetFixedXML(SAPbobsCOM.RecordsetXMLModeEnum.rxmData)),Movimientos=_context.TrasladosVirtuales.Where(x=>x.NoCorte== NoCorte).ToList() });

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
