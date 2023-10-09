using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAP_API.Models
{
    public class Inventory
    {
        public int ID { get; set; }

        public InventoryType Type { get; set; }

        public Status Status { get; set; }

        public Warehouse Warehouse { get; set; }

        public User User { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }

        public DateTime? DateClosed { get; set; }
    }
}
