using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTDIP.Model
{
    public class Calculation
    {
        public int Id { get; set; }
        public int MetalId { get; set; }
        public int Year { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Concentration { get; set; }
        public DateTime CalculationDate { get; set; }

        public required Metal Metal { get; set; }
    }
}
