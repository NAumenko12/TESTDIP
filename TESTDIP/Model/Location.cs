using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTDIP.Model
{
    public class Location
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SiteNumber { get; set; }
        public string DistanceFromSource { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public List<Sample> Samples { get; set; } = new List<Sample>();
    }
}
