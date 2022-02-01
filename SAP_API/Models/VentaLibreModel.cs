using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class VentaLibreModel
    {
        [Key]
        public int id { get; set; }

        [Required]
        public int IsFree { get; set; }

        [Required]
        public string idUsuario { get; set; }
    }
}
