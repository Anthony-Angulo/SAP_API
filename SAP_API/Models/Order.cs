using System;
using System.Collections.Generic;

namespace SAP_API.Models
{

    public class SearchRequest
    {
        public int Draw { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public List<Column> columns { get; set; }
        public Search search { get; set; }
        public List<Order> order { get; set; }
    }

    public class Column
    {
        public string data { get; set; }
        public string name { get; set; }
        public bool searchable { get; set; }
        public bool orderable { get; set; }
        public Search search { get; set; }
    }

    public class Search
    {
        public string value { get; set; }
        public string regex { get; set; }
    }

    public class Order
    {
        public int column { get; set; }
        public string dir { get; set; }
    }




    public abstract class SearchDetail
    {
    }

    public class OrderSearchDetail : SearchDetail
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocDate { get; set; }
        public string DocStatus { get; set; }
        public string CardName { get; set; }
        public string CardFName { get; set; }
        public string SlpName { get; set; }
        public string WhsName { get; set; }
    }

    public abstract class SearchResponse<T> where T : SearchDetail
    {
        public int Draw { get; set; }

        public int RecordsTotal { get; set; }

        public int RecordsFiltered { get; set; }

        public IList<T> Data { get; set; }
    }

    public class OrderSearchResponse : SearchResponse<OrderSearchDetail>
    {
    }

    public class CreateOrder
    {
        public string cardcode { set; get; }
        public string currency { set; get; }
        public string comments { set; get; }
        public int series { set; get; }
        public int payment { set; get; }
        //public double currencyRate { set; get; }
        //public int priceList { set; get; }
        public int auth { set; get; }
        //public DateTime date { set; get; }
        public List<OrderRow> rows { set; get; }
    }

    public class OrderRow
    {
        public double quantity { set; get; }
        public string code { set; get; }
        public int uom { set; get; }
        public double equivalentePV { set; get; }
    }

    public class UpdateOrder
    {
        public List<OrderRow> newProducts { set; get; }
        public List<UpdateOrderRow> ProductsChanged { set; get; }
    }

    public class UpdateOrderRow
    {
        public int LineNum { set; get; }
        public double quantity { set; get; }
        public int uom { set; get; }
        public double equivalentePV { set; get; }
    }


}
