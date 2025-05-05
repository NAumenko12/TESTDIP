using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTDIP.Model
{
    public class WindRoseData
    {
     
        public List<double> DirectionProbabilities { get; set; }

        public WindRoseData()
        {
            DirectionProbabilities = new List<double>
            {
                0.15, // С
                0.10, // СВ
                0.20, // В
                0.10, // ЮВ
                0.05, // Ю
                0.10, // ЮЗ
                0.25, // З
                0.05  // СЗ
            };
        }
        public double GetProbabilityForAngle(double angleRadians)
        {
            int N = DirectionProbabilities.Count;
            double sectorSize = 2 * Math.PI / N;

            
            while (angleRadians < 0) angleRadians += 2 * Math.PI;
            while (angleRadians >= 2 * Math.PI) angleRadians -= 2 * Math.PI;

           
            int i = (int)(angleRadians / sectorSize);
            if (i >= N) i = 0; 

            int i_next = (i + 1) % N;

            double factor = (angleRadians - i * sectorSize) / sectorSize;

            return DirectionProbabilities[i] + factor * (DirectionProbabilities[i_next] - DirectionProbabilities[i]);
        }
    }
}
