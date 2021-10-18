using System.ComponentModel.DataAnnotations;

namespace BalanceAval.Models
{

    public class CSVFormat
    {
        public string Z1 { get; set; }
        public string Z2 { get; set; }
        public string Z3 { get; set; }
        public string Z4 { get; set; }
        public string X1 { get; set; }

        public string X2 { get; set; }

        public string Y { get; set; }
    }

    public class MeasurementRow
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public double Z1 { get; set; }
        [Required]
        public double Z2 { get; set; }
        [Required]
        public double Z3 { get; set; }
        [Required]
        public double Z4 { get; set; }
        [Required]
        public double X1 { get; set; }
        [Required]
        public double X2 { get; set; }
        [Required]
        public double Y { get; set; }
        [Required]
        public MeasurementSlot MeasurementSlot { get; set; }
    }
}