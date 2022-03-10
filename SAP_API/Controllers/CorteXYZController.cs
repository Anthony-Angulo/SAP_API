using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public CorteXYZController(ApplicationDbContext context)
        {
            _context = context;
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
        }
        public class TransferError : Transfer { 
        public string error { get; set; }
        }
        [HttpPost("Ledger")]
        public async Task<IActionResult> GetLedgers([FromBody] Ledger Ledger)
        {
            return Ok();
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.ChartOfAccounts request = (SAPbobsCOM.ChartOfAccounts)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oChartOfAccounts);
            SAPbobsCOM.JournalEntries move = (SAPbobsCOM.JournalEntries)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oJournalEntries);
            List<TransferError> transferenciasRechazadas = new List<TransferError>();
            foreach (Transfer item in Ledger.transfers)
            {
                string CuentaCargo = "";
                if (item.Clave == "EF")
                {
                    CuentaCargo = "110-005-001-000";
                }
                else if (item.Clave == "US")
                {
                    CuentaCargo = "110-005-002-000";
                }
                else if (item.Clave == "TD")
                {
                    CuentaCargo = "110-005-004-000";
                }

                else if (item.Clave == "TC")
                {
                    CuentaCargo = "110-005-003-000";
                }

                else if (item.Clave == "VL")
                {
                    CuentaCargo = "110-005-007-001";
                }
                DateTime fecha = item.Fecha;
                move.Series = 19;
                move.DueDate = fecha;
                move.ReferenceDate = fecha;
                move.TaxDate = fecha;
                //move.StornoDate
                //          move.VatDate
                //Cuenta de cargo
                move.Lines.ShortName = "110-001-002-001";
                move.Lines.AccountCode = "110-001-002-001";
                move.Lines.ContraAccount = CuentaCargo;
                move.Lines.Credit = item.CantidadReal;
                move.Lines.Debit = 0;
                move.Lines.DueDate = fecha;
                move.Lines.TaxDate = fecha;
                move.Lines.ReferenceDate1 = fecha;
                move.Lines.Add();
                //Cuenta de abono
                move.Lines.AccountCode = CuentaCargo;
                move.Lines.ContraAccount = "110-001-002-001";
                move.Lines.Credit = 0;
                move.Lines.Debit = item.CantidadEnviada;
                move.Lines.DueDate = fecha;
                move.Lines.ReferenceDate1 = fecha;
                move.Lines.TaxDate = fecha;
                move.Lines.Add();
                //Cuenta de abono diferencia
                move.Lines.AccountCode = "810-200-004-000";
                move.Lines.ContraAccount = "110-001-002-001";
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
                int lRetCode = 0;
                try
                {
                    lRetCode = move.Add();
                    _context.TrasladosVirtuales.Add(
                        new TrasladosVirtuales {
                            Cantidad = item.CantidadEnviada.ToString(),
                            CantidadReal = item.CantidadReal.ToString(),
                            ClaveTransaccion = item.Clave,
                            NoCorte = Ledger.NoCorte,
                            Usuario = Ledger.Usuario
                        });
                    _context.SaveChanges();
                }

                catch (Exception)
                {
                    transferenciasRechazadas.Add(new TransferError { Clave = item.Clave, CantidadEnviada = item.CantidadEnviada, CantidadReal = item.CantidadReal, Fecha = item.Fecha, error = context.oCompany.GetLastErrorDescription() });
                }
                if (lRetCode != 0) {

                    transferenciasRechazadas.Add(new TransferError{ Clave=item.Clave,CantidadEnviada=item.CantidadEnviada,CantidadReal=item.CantidadReal,Fecha=item.Fecha,error=context.oCompany.GetLastErrorDescription()});
                }
                            

            }
            if (transferenciasRechazadas.Count != 0)
            {
                return Ok("Transferencias realizas con exito");

            }
            else
            {

            }
            {
                return BadRequest(new { Trasnfers=transferenciasRechazadas});

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
