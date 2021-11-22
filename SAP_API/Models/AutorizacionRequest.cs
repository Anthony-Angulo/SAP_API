using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class AutorizacionRequest
    {
        [Key]
        public int id { get; set; }
        [Required]
        public string Usuario { get; set; }
        [Required]
        public string Sucursal { get; set; }
        [Required]
        public string Cliente { get; set; }
        [Required]
        public string CardCode { get; set; }
        [Required]
        public string Producto { get; set; }
        [Required]
        public string ProductCode { get; set; }
        [Required]
        public string PrecioBase { get; set; }
        [Required]
        public string PrecioSolicitado { get; set; }
        [Required]
        public string CantidadBase { get; set; }
        [Required]
        public string Currency { get; set; }
        [Required]
        public sbyte Autorizado { get; set; }

        public DateTime Fecha { get; set; }

        [Required]
        public string USER_CODE { get; set; }

        [NotMapped]
        public string Cantidad { get; set; }

    }
}
