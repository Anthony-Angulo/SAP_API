using System;
using System.ComponentModel.DataAnnotations;

namespace SAP_API.Models
{
    public class rutas
    {
        [Key]
        public int id { get; set; }

        public string Nombre { get; set; }

        public string IdUsuarioAsignado { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

    }
}
