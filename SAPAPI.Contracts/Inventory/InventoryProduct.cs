using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAP_API.Models
{
    public class InventoryProduct
    {
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
}
