using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAP_API.Models
{
    public class QR_ALMACENES
    {
        [Key]
        public int id { get; set; }

        public string data { get; set; }
        [Column(TypeName = "decimal(10,7)")]
        public decimal Latitud { get; set; }
        [Column(TypeName = "decimal(10,7)")]

        public decimal Longitud { get; set; }

        public string IdUsuario { get; set; }

        public int Almacen { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
