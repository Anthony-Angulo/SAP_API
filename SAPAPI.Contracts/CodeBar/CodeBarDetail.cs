using System.ComponentModel.DataAnnotations;

namespace SAP_API.Models {

    // Class to Serialize Codebar Composition. External DB.
    public class CodeBarDetail {
        public int ID { get; set; }

        [StringLength(15)]
        public string ItemCode { get; set; }

        [StringLength(50)]
        public string OriginLocation { get; set; }

        public int UoM { get; set; }
        public int BarcodeLength { get; set; }
        public int WeightLength { get; set; }
        public int WeightPosition { get; set; }
        public bool HasDecimal { get; set; }
        public int GTinLength { get; set; }
        public int GTinPosition { get; set; }

        [StringLength(30)]
        public string GTIN { get; set; }
    }
}
