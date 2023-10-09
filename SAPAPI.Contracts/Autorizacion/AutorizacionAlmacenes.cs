using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAP_API.Models
{
    public class AutorizacionAlmacenes
    {
        [Key]
        public int id { get; set; }
        [Required]
        public string Usuario { get; set; }
        [Required]
        public string Sucursal { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string WhsDestino { get; set; }
        [Required]
        public string WhsOrigen { get; set; }
        [Required]
        public DateTime Fecha { get; set; }

        [NotMapped]
        public string Comentario { get; set; }
        public sbyte Autorizado { get; set; }

    }
}
