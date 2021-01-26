using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models {
    // Class to Serialize User. External DB.
    public class User : IdentityUser {

        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        public int SAPID { get; set; }

        public bool Active { get; set; }

        public Warehouse Warehouse { get; set; }

        public Department Department { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }

        public string Active_Burn { get; set; }

        public int Serie { get; set; }
    }
}
