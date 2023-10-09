using System.ComponentModel.DataAnnotations;

namespace SAP_API.Models {

    // Class to Serialize Department. External DB.
    public class Department {
        public int ID { get; set; }

        [StringLength(30)]
        [MinLength(5)]
        [Required]
        public string Name { get; set; }
    }

    // Class to Receive Department Configuration.
    public class DepartmentDto {
        [Required]
        public string Name { get; set; }
    }
}
