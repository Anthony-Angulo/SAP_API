﻿using System.Collections.Generic;

namespace SAP_API.Models
{
    public class TransferRequestSearchDetail : SearchDetail {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocDate { get; set; }
        public string DocStatus { get; set; }
        public string Filler { get; set; }
        public string ToWhsCode { get; set; }
    }

    public class TransferRequestSearchResponse : SearchResponse<TransferRequestSearchDetail> { }

    public class TransferRequest {
        public string comments { set; get; }
        public TransferRequestWarehouse fromwhs { set; get; }
        public TransferRequestWarehouse towhs { set; get; }
        public List<TransferRequestRow> rows { set; get; }
    }

    public class TransferRequestRow {
        public double quantity { set; get; }
        public string code { set; get; }
        public int uom { set; get; }
        public int uomBase { set; get; }
        public double equivalentePV { set; get; }
    }

    public class TransferRequestWarehouse {
        public string whstsrcode { set; get; }
        public string whscode { set; get; }
    }

    public class UpdateTransferRequest {
        public List<TransferRequestRow> newProducts { set; get; }
        public List<UpdateTransferRequestRow> ProductsChanged { set; get; }
    }

    public class UpdateTransferRequestRow {
        public int LineNum { set; get; }
        public double quantity { set; get; }
        public int uom { set; get; }
        public int uomBase { set; get; }
    }
}
