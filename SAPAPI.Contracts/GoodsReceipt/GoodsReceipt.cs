using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class GoodsReceiptSearchDetail
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocDate { get; set; }
        public string DocStatus { get; set; }
        public string WhsName { get; set; }
    }

}
