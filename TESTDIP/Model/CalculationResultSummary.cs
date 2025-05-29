using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTDIP.Model
{
    public class CalculationResultSummary
    {
        public int Id { get; set; }
        public int MetalId { get; set; }
        public string MetalName { get; set; }
        public int Year { get; set; }
        public DateTime CalculationDate { get; set; }
        public int PointCount { get; set; }
        public double MinConcentration { get; set; }
        public double MaxConcentration { get; set; }
        public double AvgConcentration { get; set; }
        public double MaxDistance { get; set; }
    }
}
