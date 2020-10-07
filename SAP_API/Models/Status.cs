using System.ComponentModel.DataAnnotations;

namespace SAP_API.Models {
    // Class to Serialize Status. External DB.
    public class Status {
        public int ID { get; set; }

        [StringLength(30)]
        [Required]
        public string Label { get; set; }
    }
}
