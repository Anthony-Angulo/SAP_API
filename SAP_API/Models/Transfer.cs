using System.Collections.Generic;

namespace SAP_API.Models
{
    public class Transfer {
        public int order { set; get; }
        public List<TransferRow> products { set; get; }

    }
    public class TransferRow {
        public string ItemCode { set; get; }
        public double Count { set; get; }
        public int Line { set; get; }
        public int UoMEntry { set; get; }
        public SAPbobsCOM.BoYesNoEnum  UseBaseUnits { set; get; }
        public string WarehouseCode { set; get; }
        public string Pallet { set; get; }
        public List<TranferRowBatch> batch { set; get; }
    }

    public class TranferRowBatch {
        public double quantity { set; get; }
        public string name { set; get; }
    }
}
