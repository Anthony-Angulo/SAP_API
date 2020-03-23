using System;
using System.Collections.Generic;

namespace SAP_API.Models
{
    public class PurchaseOrderDelivery {
        public int order { set; get; }
        public List<PurchaseOrderDeliveryRow> products { set; get; }

    }
    public class PurchaseOrderDeliveryRow {
        public string ItemCode { set; get; }
        public double Count { set; get; }
        public int Line { set; get; }
        public int Group { set; get; }
        public int UoMEntry { set; get; }
        public string WarehouseCode { set; get; }
        public string ItemType { set; get; }
        public List<PurchaseOrderDeliveryRowBatch> batch { set; get; }
    }

    public class PurchaseOrderDeliveryRowBatch {
        public double quantity { set; get; }
        public string name { set; get; }
        public string code { set; get; }
        public string attr1 { set; get; }
        public string pedimento { set; get; }
        public DateTime expirationDate { set; get; }
    }
}
