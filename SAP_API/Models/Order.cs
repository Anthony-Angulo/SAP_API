using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class Order
    {
        public string cardcode { set; get; }
        public string currency { set; get; }
        public string comments { set; get; }
        public int series { set; get; }
        public int payment { set; get; }
        public int auth { set; get; }
        public List<OrderRow> rows { set; get; }
    }

    public class OrderRow
    {
        public double quantity { set; get; }
        public string code { set; get; }
        public int uom { set; get; }
        public double equivalentePV { set; get; }
    }

    public class UpdateOrder
    {
        public List<OrderRow> newProducts { set; get; }
        public List<UpdateOrderRow> ProductsChanged { set; get; }
    }

    public class UpdateOrderRow
    {
        public int LineNum { set; get; }
        public double quantity { set; get; }
        public int uom { set; get; }
        public double equivalentePV { set; get; }
    }


}
