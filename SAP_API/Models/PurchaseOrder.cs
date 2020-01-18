using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class PurchaseOrder
    {
        public string cardcode { set; get; }
        public string comments { set; get; }
        public int series { set; get; }
        public double currencyrate { set; get; }
        public string numatcard { set; get; }
        public List<PurchaseOrderRow> rows { set; get; }
        
    }

    public class PurchaseOrderRow
    {
        public double quantity { set; get; }
        public string code { set; get; }
        public double price { set; get; }
    }

}
