using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class PurchaseOrderDeliveryReturn {
        public int order { set; get; }
        public List<PurchaseOrderDeliveryReturnRow> products { set; get; }
    }
    public class PurchaseOrderDeliveryReturnRow
    {
        public string ItemCode { set; get; }
        public double Count { set; get; }
        public int Line { set; get; }
        //public int UoMEntry { set; get; }
        //public string WarehouseCode { set; get; }
        public List<PurchaseOrderDeliveryReturnRowBatch> batch { set; get; }
    }

    public class PurchaseOrderDeliveryReturnRowBatch
    {
        public double quantity { set; get; }
        public string name { set; get; }
    }
}
