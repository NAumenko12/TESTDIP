using ClosedXML.Excel;
using GMap.NET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TESTDIP.DataBase;
using TESTDIP.Model;
using TESTDIP.ViewModel;

namespace TESTDIP.ViewModel
{
    public class ConcentrationCalculator
    {
        private readonly DatabaseHelper _dbHelper;

        public ConcentrationCalculator(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public List<GridPoint> CalculateField(PointLatLng sourcePoint,
                                     Location referencePoint,
                                     Metal metal,
                                     int year,
                                     double gridStepKm = 0.7,
                                     double areaSizeKm = 100.0)
        {
            var points = new List<GridPoint>();

            try
            {
                if (referencePoint == null || metal == null || _dbHelper == null)
                {
                    Console.WriteLine("Ошибка: Один из параметров равен null");
                    return points;
                }

                double? concentration = _dbHelper.GetMetalConcentration(
                    referencePoint.Id,
                    metal.Id,
                    year);

                if (!concentration.HasValue)
                {
                    Console.WriteLine($"Нет данных о концентрации для локации {referencePoint.Id}, металла {metal.Id}, года {year}");
                    return points;
                }

                if (string.IsNullOrWhiteSpace(referencePoint.DistanceFromSource))
                {
                    Console.WriteLine($"Ошибка: Расстояние от источника для локации {referencePoint.Id} не указано");
                    return points;
                }

                double Q0 = concentration.Value;
                double r0 = ParseStringToDouble(referencePoint.DistanceFromSource); 
                if (r0 <= 0)
                {
                    Console.WriteLine($"Ошибка: Некорректное расстояние от источника: {referencePoint.DistanceFromSource}");
                    return points;
                }
                double alpha = GetAlphaForMetal(metal.Name);
                double decayFactor = GetDecayFactor(metal.Name); 

                // Рассчитываем базовый параметр θ с учетом затухания
                double theta = Q0 * Math.Pow(r0, alpha) * decayFactor;

                Console.WriteLine($"Опорная точка: концентрация = {Q0}, расстояние = {r0} км");
                Console.WriteLine($"Параметры: α = {alpha}, коэффициент затухания = {decayFactor}, θ = {theta:F3}");

                // Преобразуем градусы в километры (примерно)
                double latStep = gridStepKm / 110.574;
                double lonStep = gridStepKm / (111.320 * Math.Cos(sourcePoint.Lat * Math.PI / 180));

                // Рассчитываем для сетки
                for (double lat = sourcePoint.Lat - areaSizeKm / 110.574;
                     lat <= sourcePoint.Lat + areaSizeKm / 110.574;
                     lat += latStep)
                {
                    for (double lon = sourcePoint.Lng - areaSizeKm / (111.320 * Math.Cos(lat * Math.PI / 180));
                         lon <= sourcePoint.Lng + areaSizeKm / (111.320 * Math.Cos(lat * Math.PI / 180));
                         lon += lonStep)
                    {
                        double r = CalculateDistance(sourcePoint, new PointLatLng(lat, lon));
                        if (r < 0.1) continue; 
                        if (r > areaSizeKm) continue; 

                        double lambda = GetCharacteristicLength(metal.Name); 
                        double Q = theta / Math.Pow(r, alpha) * Math.Exp(-r / lambda);

                        if (Q > Q0 * 10) 
                        {
                            Q = Q0 * Math.Pow(r0 / r, 0.5); 
                        }

                        points.Add(new GridPoint
                        {
                            Lat = lat,
                            Lon = lon,
                            Concentration = Math.Max(0.001, Q), 
                            DistanceFromSource = r
                        });
                    }
                }

                Console.WriteLine($"Создано {points.Count} точек расчета");

                var testPoint = points.FirstOrDefault(p => Math.Abs(p.DistanceFromSource - r0) < 1.0);
                if (testPoint != null)
                {
                    Console.WriteLine($"Проверка: на расстоянии {testPoint.DistanceFromSource:F1} км концентрация = {testPoint.Concentration:F3} мг/м³");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при расчете поля концентрации: {ex.Message}");
            }

            return points;
        }

        private double CalculateDistance(PointLatLng p1, PointLatLng p2)
        {
            double R = 6371;
            double dLat = (p2.Lat - p1.Lat) * Math.PI / 180.0;
            double dLon = (p2.Lng - p1.Lng) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(p1.Lat * Math.PI / 180.0) * Math.Cos(p2.Lat * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double GetAlphaForMetal(string metalName)
        {
            var alphaValues = new Dictionary<string, double>
            {
                ["Pb"] = 1.2,
                ["Cd"] = 1.1,
                ["Hg"] = 1.0,
                ["As"] = 1.15,
                ["Ni"] = 1.1
            };

            return alphaValues.TryGetValue(metalName, out var alpha) ? alpha : 1.1;
        }

        private double GetDecayFactor(string metalName)
        {
            var decayFactors = new Dictionary<string, double>
            {
                ["Pb"] = 0.8,
                ["Cd"] = 0.7,
                ["Hg"] = 0.6,
                ["As"] = 0.75,
                ["Ni"] = 0.7
            };

            return decayFactors.TryGetValue(metalName, out var factor) ? factor : 0.7;
        }

        private double GetCharacteristicLength(string metalName)
        { 
            var lengthValues = new Dictionary<string, double>
            {
                ["Pb"] = 50.0,
                ["Cd"] = 60.0,
                ["Hg"] = 80.0,
                ["As"] = 55.0,
                ["Ni"] = 60.0
            };

            return lengthValues.TryGetValue(metalName, out var length) ? length : 60.0;
        }

        public static double ParseStringToDouble(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0.0;

            try
            {
                // Удаляем все нецифровые символы, кроме точки и запятой
                string cleanInput = new string(input.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());

                // Заменяем запятые на точки для корректного парсинга
                cleanInput = cleanInput.Replace(",", ".");

                // Если строка пустая после очистки, возвращаем 0
                if (string.IsNullOrWhiteSpace(cleanInput)) return 0.0;

                // Пробуем разные форматы
                if (double.TryParse(cleanInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return Math.Round(result, 3);
                }

                // Если не удалось распарсить, пробуем извлечь первое число из строки
                var match = System.Text.RegularExpressions.Regex.Match(input, @"(\d+[.,]?\d*)");
                if (match.Success && double.TryParse(match.Groups[1].Value.Replace(",", "."),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    return Math.Round(result, 3);
                }

                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }
    }
}