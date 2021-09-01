using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class ProductDetail
    {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string Meet { get; set; }
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

    public class ProductPriceListDetail : SearchDetail
    {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public double Price { get; set; }
        public string UomCode { get; set; }
        public double Price2 { get; set; }
        public string UomCode2 { get; set; }
        public string Currency { get; set; }
    }

    public class ProductPriceListSearchResponse : SearchResponse<ProductPriceListDetail> { }

    public class ProductSearchDetail : SearchDetail
    {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
    }

    public class ProductSearchResponse : SearchResponse<ProductSearchDetail> { }

    public class ProductWithStockSearchDetail : SearchDetail
    {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public double OnHand { get; set; }
    }
    public class ProductoAgrupado
    {
        public string ItmsGrpNam { get; set; }

        public string ItmsGrpCod { get; set; }

        public List<ProductoParaCliente> Productos { get; set; }
    }
    public class ProductoParaCliente
    {
        public int idClientes_Productos { get; set; }

        public string label { get; set; }

        public string value { get; set; }

        public string ItemCode { get; set; }

        public string ItemName { get; set; }

        public string ItmsGrpNam { get; set; }

        public int status { get; set; }
    }

    public class ProductWithStockSearchResponse : SearchResponse<ProductWithStockSearchDetail> { }

    public class ProductToTransferDetail
    {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public double PesProm { get; set; }
        public double OnHand { get; set; }
        public List<UOMDetail> UOMList { get; set; }
    }

    public class UOMDetail
    {
        public int BaseUom { get; set; }
        public string BaseCode { get; set; }
        public int UomEntry { get; set; }
        public string UomCode { get; set; }
        public double BaseQty { get; set; }
    }

    public class Property
    {
        public string ItmsGrpNam { get; set; }
        public int ItmsTypCod { get; set; }
    }

    public class ProductLastSellPriceWMS
    {
        public string Currency { get; set; }
        public int IUoMEntry { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public double NumInSale { get; set; }
        public double Price { get; set; }
        public string QryGroup5 { get; set; }
        public string QryGroup6 { get; set; }
        public string QryGroup7 { get; set; }
        public string QryGroup8 { get; set; }
        public string QryGroup39 { get; set; }
        public int SUoMEntry { get; set; }
        public double U_IL_PesProm { get; set; }
    }

}
