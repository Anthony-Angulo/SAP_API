using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAP_API.Models
{
    public class InventoryProductDetail
    {
        public int ID { get; set; }

        public double Quantity { get; set; }

        [StringLength(30)]
        public string Zone { get; set; }

        public User User { get; set; }

        public InventoryProduct InventoryProduct { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }
    }
}
