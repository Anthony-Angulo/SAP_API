using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class ProductDetail {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public double PesProm { get; set; }
        public int SUoMEntry { get; set; }
        public int PriceList { get; set; }
        public string Currency { get; set; }
        public double Price { get; set; }
        public int UomEntry { get; set; }
        public string PriceType { get; set; }
        public string WhsCode { get; set; }
        public double OnHand { get; set; }
        public List<UOMDetail> UOMList { get; set; }
    }

    public class ProductPriceListDetail : SearchDetail{
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public double Price { get; set; }
        public string UomCode { get; set; }
        public double Price2 { get; set; }
        public string UomCode2 { get; set; }
        public string Currency { get; set; }
    }

    public class ProductPriceListSearchResponse : SearchResponse<ProductPriceListDetail> { }

    public class ProductToTransferDetail {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public double PesProm { get; set; }
        public double OnHand { get; set; }
        public List<UOMDetail> UOMList { get; set; }
    }

    public class UOMDetail {
        public int BaseUom { get; set; }
        public string BaseCode { get; set; }
        public int UomEntry { get; set; }
        public string UomCode { get; set; }
        public double BaseQty { get; set; }
    }
}
