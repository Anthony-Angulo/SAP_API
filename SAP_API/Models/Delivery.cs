using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class Delivery
    {
        public int order { set; get; }
        public List<DeliveryRow> products { set; get; }
    }
    public class DeliveryRow
    {
        public string ItemCode { set; get; }
        public double Count { set; get; }
        public int Line { set; get; }
        public int UoMEntry { set; get; }
        public string WarehouseCode { set; get; }
        public string Pallet { set; get; }
        public List<DeliveryRowBatch> batch { set; get; }
    }

    public class DeliveryRowBatch
    {
        public double quantity { set; get; }
        public string name { set; get; }
        public DateTime expirationDate { set; get; }
    }
    ///

    public class DeliveryModelMASS
    {
        public string sucursal { set; get; }
        public string comments { set; get; }
        public DateTime date { set; get; }
        public List<OrderDeliveryRowMASS> product { set; get; }

    }
    public class OrderDeliveryRowMASS
    {
        public string ItemCode { set; get; }
        public double Count { set; get; }
        //public int UoMEntry { set; get; }
        public string AccCode { set; get; }
        public List<OrderDeliveryRowBatchMASS> batch { set; get; }
    }

    public class OrderDeliveryRowBatchMASS
    {
        public double Quantity { set; get; }
        public string BatchNum { set; get; }
    }

}
