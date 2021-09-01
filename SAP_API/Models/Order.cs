using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAP_API.Models {
    public class SearchRequest {
        public int Draw { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public List<Column> columns { get; set; }
        public Search search { get; set; }
        public List<Order> order { get; set; }
    }

    public class Column {
        public string data { get; set; }
        public string name { get; set; }
        public bool searchable { get; set; }
        public bool orderable { get; set; }
        public Search search { get; set; }
    }

    public class Search {
        public string value { get; set; }
        public string regex { get; set; }
    }

    public class Order {
        public int column { get; set; }
        public string dir { get; set; }
    }

    public abstract class SearchDetail {
    }

    public class OrderSearchDetail : SearchDetail {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocDate { get; set; }
        public string DocStatus { get; set; }
        public string CardName { get; set; }
        public string CardFName { get; set; }
        public string SlpName { get; set; }
        public string WhsName { get; set; }
        public double DocTotal { get; set; }
        public string DocCur { get; set; }
        public string PymntGroup { get; set; }
    }

    public abstract class SearchResponse<T> where T : SearchDetail {
        public int draw { get; set; }

        public int recordsTotal { get; set; }

        public int recordsFiltered { get; set; }

        public IList<T> data { get; set; }
    }

    public class OrderSearchResponse : SearchResponse<OrderSearchDetail> {}

    public class OrderDetail {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocCur { get; set; }
        public double DocRate { get; set; }
        public double Total { get; set; }
        public string DocDate { get; set; }
        public string DocDueDate { get; set; }
        public string CancelDate { get; set; }
        public string DocStatus { get; set; }
        public string Comments { get; set; }
        public string DocTime { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardFName { get; set; }
        public string WhsName { get; set; }
        public string SlpName { get; set; }
        public int SlpCode { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string PymntGroup { get; set; }
        public List<OrderDetailRow> OrderRows { set; get; }
    }

    public class OrderDetailRow {
        public string ItemCode { get; set; }
        public string Dscription { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public double Quantity { get; set; }
        public string UomCode { get; set; }
        public double InvQty { get; set; }
        public string UomCode2 { get; set; }
        public double Total { get; set; }
        public string U_IL_PesProm { get; set; }
    }

    public class CreateOrder {
        public string cardcode { set; get; }
        public string currency { set; get; }
        public string comments { set; get; }
        public int series { set; get; }
        public int payment { set; get; }
        public string type { get; set; }
        //public double currencyRate { set; get; }
        public int priceList { set; get; }
        public int auth { set; get; }
        public DateTime date { set; get; }
        public List<OrderRow> rows { set; get; }

        public string idUsuario { get; set; }
    }

    public class CreateOrderRetail {
        public string cardcode { set; get; }
        public string currency { set; get; }
        public string address { set; get; }
        public string comments { set; get; }
        public int series { set; get; }
        public DateTime date { set; get; }
        public List<OrderRow> rows { set; get; }
    }

    public class OrderRow {
        public double quantity { set; get; }
        public string code { set; get; }
        public int uom { set; get; }
        public double equivalentePV { set; get; }
        public string currency { set; get; }
        public string meet { set; get; }
    }

    public class UpdateOrder {
        public List<OrderRow> newProducts { set; get; }
        public List<UpdateOrderRow> ProductsChanged { set; get; }
    }

    public class UpdateOrderRow {
        public int LineNum { set; get; }
        public double quantity { set; get; }
        public int uom { set; get; }
        public double equivalentePV { set; get; }
    }

    public class OrderAuth {
        public int ID { get; set; }

        [StringLength(15)]
        public string CardCode { get; set; }

        [StringLength(100)]
        public string CardName { get; set; }

        [StringLength(100)]
        public string CardFName { get; set; }

        [StringLength(10)]
        public string Currency { get; set; }

        public double CurrencyRate { get; set; }

        public int Payment { get; set; }

        public int Serie { get; set; }

        public int PriceList { get; set; }

        [StringLength(200)]
        public string Reason { get; set; }

        [StringLength(200)]
        public string Comments { get; set; }

        public DateTime OrderDate { get; set; }

        public Status AuthStatus { get; set; }

        public DateTime AuthDate { get; set; }

        public User? AuthUser { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }
    }

    public class OrderAuthRow {
        public int ID { get; set; }

        public double Quantity { get; set; }

        [StringLength(20)]
        public string ItemCode { get; set; }

        public int Uom { get; set; }

        public double EquivalentePV { get; set; }

        public OrderAuth Order { get; set; }
    }

}
