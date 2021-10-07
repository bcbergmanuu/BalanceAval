using System;
using System.ComponentModel.DataAnnotations;

namespace BalanceAval.Models
{
    public class MeasurementRow
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public double X1 { get; set; }
        [Required]
        public double X2 { get; set; }
        [Required]
        public double X3 { get; set; }
        [Required]
        public double X4 { get; set; }
        [Required]
        public int Measurement { get; set; }
    }

    public class MeasurementSlot
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public DateTime Time { get; set; }
    }
}