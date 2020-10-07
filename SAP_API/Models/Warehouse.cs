using System.ComponentModel.DataAnnotations;

namespace SAP_API.Models {
    // Class to Serialize Warehouse. External DB.
    public class Warehouse {
        public int ID { get; set; }

        [StringLength(10)]
        [MinLength(3)]
        [Required]
        public string WhsCode { get; set; }

        [StringLength(30)]
        [MinLength(5)]
        [Required]
        public string WhsName { get; set; }

        public bool Active { get; set; }

        public bool ActiveCRM { get; set; }
    }
    // Class to Receive Warehouse Configuration.
    public class WarehouseDto {

        [Required]
        public string WhsCode { get; set; }

        [Required]
        public string WhsName { get; set; }

        [Required]
        public bool Active { get; set; }

        [Required]
        public bool ActiveCRM { get; set; }
    }
}
