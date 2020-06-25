using System.Collections.Generic;

namespace SAP_API.Models
{
    public class PurchaseOrderSearchDetail : SearchDetail {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocDate { get; set; }
        public string DocStatus { get; set; }
        public string CardName { get; set; }
        public string CardFName { get; set; }
        public string WhsName { get; set; }
    }

    public class PurchaseOrderSearchResponse : SearchResponse<PurchaseOrderSearchDetail> { }

    // WMS DETAIL
    public class PurchaseOrderDetail {

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
        public List<PurchaseOrderDetailRow> PurchaseOrderRows { set; get; }

    }

    public class PurchaseOrderDetailRow {
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


    public class PurchaseOrder {
        public string cardcode { set; get; }
        public string comments { set; get; }
        public int series { set; get; }
        public double currencyrate { set; get; }
        public string numatcard { set; get; }
        public List<PurchaseOrderRow> rows { set; get; }
        
    }

    public class PurchaseOrderRow {
        public double quantity { set; get; }
        public string code { set; get; }
        public double price { set; get; }
    }

    public class PurchaseOrderCopy {
        public int DocNumBase { set; get; }

    }


}
