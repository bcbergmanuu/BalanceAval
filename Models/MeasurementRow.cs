using System;
using System.Collections.Generic;
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
        public MeasurementSlot MeasurementSlot { get; set; }
    }

    public class MeasurementSlot
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public DateTime Time { get; set; }

        [Required]
        public ICollection<MeasurementRow> MeasurementRows { get; set; }
    }
}