using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAP_API.Models {
    public class Inventory {
        public int ID { get; set; }

        public InventoryType Type { get; set; }

        public Status Status { get; set; }

        public Warehouse Warehouse { get; set; }

        public User User { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }

        public DateTime? DateClosed { get; set; }
    }

    public class InventoryProduct {
        public int ID { get; set; }

        [StringLength(20)]
        public string ItemCode { get; set; }

        [StringLength(50)]
        public string ItemName { get; set; }

        public double Quantity { get; set; }

        public double InvQuantity { get; set; }

        [StringLength(10)]
        public string NeedBatch { get; set; }

        [StringLength(10)]
        public string WeightType { get; set; }

        public User User { get; set; }

        public Inventory Inventory { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }
    }

    public class InventoryProductDetail {
        public int ID { get; set; }

        public double Quantity { get; set; }

        [StringLength(30)]
        public string Zone { get; set; }

        public User User { get; set; }

        public InventoryProduct InventoryProduct { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }
    }

    public class InventoryProductBatch {
        public int ID { get; set; }

        public double Quantity { get; set; }

        [StringLength(20)]
        public string Batch { get; set; }

        [StringLength(70)]
        public string CodeBar { get; set; }

        public InventoryProductDetail InventoryProductDetail { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }
    }

    public class InventoryType {
        public int ID { get; set; }

        [StringLength(30)]
        [Required]
        public string Label { get; set; }
    }
}
