using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAP_API.Models
{
    public class InventoryProductBatch
    {
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
}
