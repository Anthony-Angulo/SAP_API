using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SAP_API.Models {

    public class Delivery {
        public int DocEntry { set; get; }
        public string whsCode { set; get; }
        public List<DeliveryRow> DeliveryRows { set; get; }
    }

    public class DeliveryRow
    {
        public int LineNum { set; get; }
        public List<DeliveryRowDetail> DeliveryRowDetailList { set; get; }
    }

    public class DeliveryOld
    {
        public int order { set; get; }
        public List<DeliveryRowOld> products { set; get; }
    }

    public class DeliveryRowOld
    {
        public string ItemCode { set; get; }
        public double Count { set; get; }
        public int Line { set; get; }
        public int UoMEntry { set; get; }
        public string WarehouseCode { set; get; }
        public string Pallet { set; get; }
        public List<DeliveryRowBatchOld> batch { set; get; }
    }

    public class DeliveryRowBatchOld
    {
        public double quantity { set; get; }
        public string name { set; get; }
        public DateTime expirationDate { set; get; }
    }

    public class DeliveryRowDetail {
        public double Count { set; get; }
        public string ItemCode { set; get; }
        public int UomEntry { set; get; }
        public List<DeliveryRowBatch> BatchList { set; get; }
    }

    public class DeliveryRowBatch {
        public double Quantity { set; get; }
        [Required]
        public string Code { set; get; }
    }
    ///

    public class DeliveryModelMASS {
        public string sucursal { set; get; }
        public string comments { set; get; }
        public DateTime date { set; get; }
        public List<OrderDeliveryRowMASS> product { set; get; }

    }
    public class OrderDeliveryRowMASS {
        public string ItemCode { set; get; }
        public double Count { set; get; }
        //public int UoMEntry { set; get; }
        public string AccCode { set; get; }
        public List<OrderDeliveryRowBatchMASS> batch { set; get; }
    }

    public class OrderDeliveryRowBatchMASS {
        public double Quantity { set; get; }
        public string BatchNum { set; get; }
    }

}
