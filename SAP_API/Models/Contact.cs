using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class ContactToSell {
        public string CardCode { set; get; }
        public string CardName { set; get; }
        public string CardFName { set; get; }
        public int ListNum { set; get; }
        public int GroupNum { set; get; }
        public string PymntGroup { set; get; }
        public string PayMethCod { set; get; }
        public string Descript { set; get; }
        public string SlpName { set; get; }
        public string ListName { set; get; }
        public double Balance { set; get; }
        public List<ContactPaymentMethod> PaymentMethods { set; get; }
    }

    public class ContactPaymentMethod {
        public string PymCode { set; get; }
        public string Descript { set; get; }
    }

    public class ClientSearchDetail : SearchDetail {
        public string CardCode { get; set; }
        public string CardFName { get; set; }
        public string CardName { get; set; }
    }
    public class ClientSearchResponse : SearchResponse<ClientSearchDetail> { }
    public class ProviderSearchDetail : SearchDetail {
        public string CardCode { get; set; }
        public string CardFName { get; set; }
        public string CardName { get; set; }
        public string Currency { get; set; }
    }
    public class ProviderSearchResponse : SearchResponse<ProviderSearchDetail> { }
}
