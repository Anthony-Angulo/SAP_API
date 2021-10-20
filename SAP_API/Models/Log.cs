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
    public class LogFacturacion
    {
        public int id { get; set; }
        public string user { get; set; }
        public string Productdsc { get; set; }
        public string ProductCode { get; set; }
        public string CantidadBase { get; set; }
        public string PrecioBase{ get; set; }
        public string MonedaBase{ get; set; }
        public string PrecioIntroducido { get; set; }
        public string MonedaIntroducida { get; set; }
        public string serie { get; set; }
        public string warehouseextern{ get; set; }
        public string TipoCambio { get; set; }
        public DateTime fecha { get; set; }
    }
}
