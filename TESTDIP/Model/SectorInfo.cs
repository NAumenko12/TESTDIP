using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTDIP.Model
{
    public class SectorInfo
    {
        public List<GridPoint> Points { get; set; } = new List<GridPoint>();
        public double MaxConcentration { get; set; } = 0;
        public double MaxDistance { get; set; } = 0;
        public double TotalConcentration { get; set; } = 0;
        public double AverageConcentration { get; set; } = 0;
    }
}
