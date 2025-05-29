using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTDIP.Model
{
    public class WindProfileHelper
    {
        /// <summary>
        /// Получение профиля ветра для 16 направлений (роза ветров)
        /// </summary>
        public List<WindDirection> GetDefaultWindProfile16Rumbs()
        {
            return new List<WindDirection>
            {
                new WindDirection { AngleDegrees = 0,    Weight = 0.08 },  // С
                new WindDirection { AngleDegrees = 22.5, Weight = 0.06 },  // ССВ
                new WindDirection { AngleDegrees = 45,   Weight = 0.05 },  // СВ
                new WindDirection { AngleDegrees = 67.5, Weight = 0.04 },  // ВСВ
                new WindDirection { AngleDegrees = 90,   Weight = 0.06 },  // В
                new WindDirection { AngleDegrees = 112.5,Weight = 0.08 },  // ВЮВ
                new WindDirection { AngleDegrees = 135,  Weight = 0.12 },  // ЮВ
                new WindDirection { AngleDegrees = 157.5,Weight = 0.15 },  // ЮЮВ
                new WindDirection { AngleDegrees = 180,  Weight = 0.18 },  // Ю
                new WindDirection { AngleDegrees = 202.5,Weight = 0.15 },  // ЮЮЗ
                new WindDirection { AngleDegrees = 225,  Weight = 0.12 },  // ЮЗ
                new WindDirection { AngleDegrees = 247.5,Weight = 0.08 },  // ЗЮЗ
                new WindDirection { AngleDegrees = 270,  Weight = 0.06 },  // З
                new WindDirection { AngleDegrees = 292.5,Weight = 0.04 },  // ЗСЗ
                new WindDirection { AngleDegrees = 315,  Weight = 0.05 },  // СЗ
                new WindDirection { AngleDegrees = 337.5,Weight = 0.06 }   // ССЗ
            };
        }
    }
}
