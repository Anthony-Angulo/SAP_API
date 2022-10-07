using System;
using System.ComponentModel.DataAnnotations;

namespace SAP_API.Models
{
    public class tiendas_ruta
    {
        [Key]
        public int Id { get; set; }

        public int IdTienda { get; set; }

        public int IdRuta { get; set; }

        public DateTime FechaAsignada { get; set; }

        public bool Activo { get; set; }

    }
}
