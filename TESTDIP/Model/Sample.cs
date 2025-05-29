using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTDIP.Model
{
    public class Sample
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public int MetalId { get; set; }
        public string Type { get; set; }
        public string Fraction { get; set; }
        public int? Repetition { get; set; }
        public string Value { get; set; }
        public DateTime SamplingDate { get; set; }
        public string AnalyticsNumber { get; set; }

        public Metal Metal { get; set; }
        public Location Location { get; set; }
    }
}