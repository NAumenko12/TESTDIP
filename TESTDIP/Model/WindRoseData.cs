using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TESTDIP.DataBase;
using WeatherDataParser;

namespace TESTDIP.Model
{
    class WindRoseData
    {
        private Statistics _statistics;
        private Dictionary<decimal, decimal> windRose;
        private decimal[] windRoseArray;
        private readonly DatabaseHelper _dbHelper;
        public WindRoseData(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;

            string connectionString = @"Data Source=""C:\Users\natac\OneDrive\Рабочий стол\АналитикаПогода\WeatherAnalitycs\bin\Debug\net8.0-windows7.0\WeatherDatabases"";Foreign Keys=True";


            _statistics = new Statistics(
                new DateTime(2020, 1, 04),
                new DateTime(2020, 4, 09),
                22212,
                connectionString
            );

            windRose = _statistics.GetPercentageWindRose(false, 16);
            windRoseArray = windRose.Values.ToArray();
        }
    }
}
