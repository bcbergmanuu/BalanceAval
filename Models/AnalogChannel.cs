using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanceAval.Models
{
    public class AnalogChannel
    {
        public string Name { get; set; }
        public List<double> Values { get; set; }
    }

    public class CsvRow
    {
        public double X1 { get; set; }
        public double X2 { get; set; }
        public double X3 { get; set; }
        public double X4 { get; set; }
    }
}
