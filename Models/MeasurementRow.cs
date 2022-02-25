using System;
using System.ComponentModel.DataAnnotations;

namespace BalanceAval.Models
{

    public class SerialRow
    {
        public int Z1 { get; set; }
        public int Z2 { get; set; }
        public int Z3 { get; set; }
        public int Z4 { get; set; }
        public int X1 { get; set; }
        public int X2 { get; set; }
        public int Y { get; set; }
    }

    public class CSVFormat
    {
        public float Z1 { get; set; }
        public float Z2 { get; set; }
        public float Z3 { get; set; }
        public float Z4 { get; set; }
        public float X1 { get; set; }
        public float X2 { get; set; }
        public float Y { get; set; }
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