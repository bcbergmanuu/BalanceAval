using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Avalonia;

namespace BalanceAval.Models
{
    public class MeasurementSlot
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public DateTime Time { get; set; }

        [Required]
        public ICollection<MeasurementRow> MeasurementRows { get; set; }
    }

    public class Ellipse
    {
        public Point CenterPoint { get; set; }
    }
}