using System;
using System.Collections.Generic;

namespace SAP_API.Models
{
    public class cotizaciones
    {
        public int Id { get; set; }
        public string CardCode { get; set; }

        public string CardName { get; set; }
        public string CardFName { get; set; }

        public string Currency { get; set; }
        public Double CurrencyRate { get; set; }

        public int Payment { get; set; }

        public int Series { get; set; }
        public int PriceList { get; set; }
        public string Comments{ get; set; }
        public DateTime Date { get; set; }=DateTime.Now;

       public ICollection<cotizacionrows> rows { get; set; }

    }
    public class Dtocotizaciones
    {
        public string CardCode { get; set; }

        public string CardName { get; set; }
        public string CardFName { get; set; }

        public string Currency { get; set; }
        public Double CurrencyRate { get; set; }

        public int Payment { get; set; }

        public int Series { get; set; }
        public int PriceList { get; set; }
        public string Comments { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        public ICollection<cotizacionrows> rows { get; set; }

    }

    public class cotizacionrows {
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
