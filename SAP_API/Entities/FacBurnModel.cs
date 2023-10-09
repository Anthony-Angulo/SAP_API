using System;
using System.ComponentModel.DataAnnotations;
using static SAP_API.Entities.ApplicationDbContext;

namespace SAP_API.Entities
{
    public class FacBurn
    {
        [Key]
        public int DocEntry { get; set; }

        public int DocNum { get; set; }
        public string UserName { get; set; }

        public int Series { get; set; }

        public DateTime DateBurn { get; set; }

        public DateTime DocDate { get; set; }
    }

    public class FacturasNuevas
    {
        public int NumeroDeDocumento { get; }
        public Series Series { get; }
        public DateTime FechaQuemado { get; }
        public DateTime FechaDocumento { get; }
        public string Usuario { get; }
        public FacturasNuevas(int numeroDeDocumento, Series series, DateTime fechaQuemado, DateTime fechaDocumento, string usuario)
        {
            NumeroDeDocumento = numeroDeDocumento;
            Series = series;
            FechaQuemado = fechaQuemado;
            FechaDocumento = fechaDocumento;
            Usuario = usuario;
        }

    }
}