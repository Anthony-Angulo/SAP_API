using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class ClientsProducts
    {
        [Key]
        public int idClientes_Productos { get; set; }

        public string ClientCode { get; set; }

        public string ProductCode { get; set; }

        public string ProductDescription { get; set; }

        public string ProductGroup { get; set; }

        public int status { get; set; }
    }
    public class ProductosPreferidos
    {
        public string ItemCode { get; set; }

        public string ItemName { get; set; }

        public string ItmsGrpNam { get; set; }

        public int status { get; set; }
    }
}
