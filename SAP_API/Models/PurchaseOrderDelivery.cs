using System;
using System.Collections.Generic;

namespace SAP_API.Models
{

    public class PurchaseDeliveryDetail {

        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocCur { get; set; }
        public double Total { get; set; }
        public string DocDate { get; set; }
        public string DocDueDate { get; set; }
        public string CancelDate { get; set; }
        public string DocStatus { get; set; }
        public string Comments { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardFName { get; set; }
        public string WhsName { get; set; }
        public List<PurchaseDeliveryDetailRow> PurchaseDeliveryRows { set; get; }

    }

    public class PurchaseDeliveryDetailRow {
        public string ItemCode { get; set; }
        public string Dscription { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public double Quantity { get; set; }
        public string UomCode { get; set; }
        public double InvQty { get; set; }
        public string UomCode2 { get; set; }
        public double Total { get; set; }
    }

    public class PurchaseOrderDelivery {
        public int order { set; get; }
        public string pedimento { set; get; }
        public List<PurchaseOrderDeliveryRow> products { set; get; }

    }
    public class PurchaseOrderDeliveryRow {
        public string ItemCode { set; get; }
        public double Count { set; get; }
        public int Line { set; get; }
        public int Group { set; get; }
        public int UoMEntry { set; get; }
        public string UoMCode { set; get; }
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
