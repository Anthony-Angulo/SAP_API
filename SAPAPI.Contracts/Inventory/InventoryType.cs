using System.ComponentModel.DataAnnotations;

namespace SAP_API.Models
{
    public class InventoryType {
        public int ID { get; set; }

        [StringLength(30)]
        [Required]
        public string Label { get; set; }
    }
}
