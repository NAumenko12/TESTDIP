using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTDIP.Model
{
    public class GridPoint
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Concentration { get; set; }

        // Добавленные свойства
        public double DistanceFromSource { get; set; }
        public double NormalizedConc { get; set; }
    }
}