using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_API.Models
{
    public class TrasladosVirtuales
    {
        [Key]
        public int id { get; set; }
        [Required]
        public string NoCorte { get; set; }

        [Required]
        public string ClaveTransaccion { get; set; }
        [Required]
        public string Cantidad { get; set; }
        [Required]
        public string CantidadReal { get; set; }
        [Required]
        public string Usuario { get; set; }
    }
}
