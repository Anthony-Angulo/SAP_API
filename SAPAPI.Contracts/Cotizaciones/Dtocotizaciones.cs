using System;
using System.Collections.Generic;

namespace SAP_API.Models
{
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

}
