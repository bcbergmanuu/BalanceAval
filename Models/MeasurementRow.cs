using System.ComponentModel.DataAnnotations;

namespace BalanceAval.Models
{

    public class CSVFormat
    {
        public double Z1 { get; set; }
        public double Z2 { get; set; }
        public double Z3 { get; set; }
        public double Z4 { get; set; }
        public double X1 { get; set; }
        public double X2 { get; set; }
        public double Y { get; set; }
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