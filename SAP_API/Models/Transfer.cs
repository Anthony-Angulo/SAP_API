using SAPbobsCOM;
using System.Collections.Generic;

namespace SAP_API.Models
{
    public class TransferSearchDetail : SearchDetail {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocDate { get; set; }
        public string Filler { get; set; }
        public string ToWhsCode { get; set; }
    }

    public class TransferSearchResponse : SearchResponse<TransferSearchDetail> { }


    public class TransferDetail {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocDate { get; set; }
        public string DocDueDate { get; set; }
        public string CancelDate { get; set; }
        public string Comments { get; set; }
        public string Filler { get; set; }
        public string ToWhsCode { get; set; }
        public List<TransferDetailRow> TransferRows { set; get; }
    }

    public class TransferDetailRow {
        public string ItemCode { get; set; }
        public string Dscription { get; set; }
        public double Quantity { get; set; }
        public string UomCode { get; set; }
        public double InvQty { get; set; }
        public string UomCode2 { get; set; }
    }

    //public class TransferRow {
    //    public string ItemCode { set; get; }
    //    public double Count { set; get; }
    //    public int Line { set; get; }
    //    public int UoMEntry { set; get; }
    //    public SAPbobsCOM.BoYesNoEnum UseBaseUnits { set; get; }
    //    public string Pallet { set; get; }
    //    public List<TranferRowBatch> batch { set; get; }
    //}

    public class TranferRowBatch {
        public double quantity { set; get; }
        public string name { set; get; }
    }

    public class Transfer {
        public int DocEntry { set; get; }
        public List<TransferRow> TransferRows { set; get; }
    }

    public class TransferOld
    {
        public int order { set; get; }
        public List<TransferRowOld> products { set; get; }
    }

    public class TransferRowOld
    {
        public string ItemCode { set; get; }
        public double Count { set; get; }
        public int Line { set; get; }
        public int UoMEntry { set; get; }
        public SAPbobsCOM.BoYesNoEnum UseBaseUnits { set; get; }
        public string WarehouseCode { set; get; }
        public string Pallet { set; get; }
        public List<TranferRowBatchOld> batch { set; get; }
    }
    public class TransferRow {
        public int LineNum { set; get; }
        public string Pallet { get; set; }
        public double Count { get; set; }
                public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string UomCode { get; set; }

        public List<TransferRowBatch> BatchList { set; get; }
        
    }

    public class TranferRowBatchOld
    {
        public double quantity { set; get; }
        public string name { set; get; }
    }

    public class TransferRowBatch {
        public double Quantity { set; get; }
        public string Code { set; get; }

        public string CodeBar { get; set; }
    }

}
