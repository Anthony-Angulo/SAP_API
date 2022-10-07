using System.ComponentModel.DataAnnotations;

namespace SAP_API.Models
{
    public class tarima
    {
        [Key]
        public int id { get; set; }

        public string ubicacion { get; set; }
    }
}
