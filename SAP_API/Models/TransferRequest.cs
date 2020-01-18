using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class TransferRequest
    {
        public string comments { set; get; }
        public TransferRequestWarehouse fromwhs { set; get; }
        public TransferRequestWarehouse towhs { set; get; }
        public List<TransferRequestRow> rows { set; get; }
    }

    public class TransferRequestRow
    {
        public double quantity { set; get; }
        public string code { set; get; }
        public int uom { set; get; }
        public int uomBase { set; get; }
        public double equivalentePV { set; get; }
    }

    public class TransferRequestWarehouse
    {
        public string whstsrcode { set; get; }
        public string whscode { set; get; }
    }

    public class UpdateTransferRequest
    {
        public List<TransferRequestRow> newProducts { set; get; }
        public List<UpdateTransferRequestRow> ProductsChanged { set; get; }
    }

    public class UpdateTransferRequestRow
    {
        public int LineNum { set; get; }
        public double quantity { set; get; }
        public int uom { set; get; }
        public int uomBase { set; get; }
    }
}
