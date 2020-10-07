using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAP_API.Models {
    // Class to Serialize Log. External DB.
    public class Log {
        public int ID { get; set; }

        [StringLength(100)]
        public string Action { get; set; }

        public int Document { get; set; }

        public User User { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }
    }
}
