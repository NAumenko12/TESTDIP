using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTDIP.Model
{
    public class WindRose
    {
        public Dictionary<double, double> Directions { get; set; } // Направление (градусы) -> Вероятность (%)

        public WindRose()
        {
            Directions = new Dictionary<double, double>
        {
            { 0, 10 },   // Север
            { 45, 15 },  // Северо-восток
            { 90, 20 },  // Восток
            { 135, 15 }, // Юго-восток
            { 180, 10 }, // Юг
            { 225, 10 }, // Юго-запад
            { 270, 10 }, // Запад
            { 315, 10 }  // Северо-запад
        };
        }
    }
}
