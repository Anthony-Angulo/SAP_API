using System;

namespace SAP_API.Models
{
    public class cotizacionrows
    {
        public int Id { get; set; }
        public Double Quantity { get; set; }

        public string Code { get; set; }

        public int Uom { get; set; }

        public Double EquivalentePV { get; set; }

        public int cotizacionesId { get; set; }

        public string Price { get; set; }
        public string UomDescripcion { get; set; }

        public string Currency { get; set; }
        public string Descripcion { get; set; }

        public string SelectedCurrency { get; set; }
        public cotizaciones cotizaciones { get; set; }
    }

}
