using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models {
    // Class to Receive Role Configuration.
    public class RoleDto {
        public string Name { get; set; }
        public List<string> Permissions { get; set; }
    }
}
