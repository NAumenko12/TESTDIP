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
        private WindRoseData _windRose;

        public ConcentrationCalculator(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
            _windRose = new WindRoseData(); 
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

                double C0 = concentration.Value;
                if (Math.Abs(C0) < 0.001)
                {
                    Console.WriteLine($"Ошибка: Концентрация в опорной точке слишком мала: {C0}");
                    C0 = 0.001;
                }

                double r0 = ParseStringToDouble(referencePoint.DistanceFromSource);
                try
                {
                    r0 = ParseStringToDouble(referencePoint.DistanceFromSource);
                    if (r0 <= 0)
                    {
                        Console.WriteLine($"Ошибка: Некорректное расстояние от источника: {referencePoint.DistanceFromSource}");
                        r0 = 1.0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при преобразовании расстояния: {ex.Message}");
                    r0 = 1.0;
                }

                double alpha = GetAlphaForMetal(metal.Name);
                double latStep = gridStepKm / 110.574;
                double lonStep = gridStepKm / (111.320 * Math.Cos(sourcePoint.Lat * Math.PI / 180));
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
                        double dx = lon - sourcePoint.Lng;
                        double dy = lat - sourcePoint.Lat;
                        double phi = Math.Atan2(dy, dx);
                        double oppositePhi = phi + Math.PI;
                        if (oppositePhi >= 2 * Math.PI) oppositePhi -= 2 * Math.PI;
                        double windProbability = _windRose.GetProbabilityForAngle(oppositePhi);
                        if (windProbability < 0.01)
                        {
                            windProbability = 0.01; 
                        }
                        double baseConcentration = CalculateConcentration(C0, r0, r, alpha);
                        double adjustedConcentration = baseConcentration * windProbability * 8; 
                        if (adjustedConcentration < 0.001)
                        {
                            adjustedConcentration = 0; 
                            continue;
                        }
                        points.Add(new GridPoint
                        {
                            Lat = lat,
                            Lon = lon,
                            Concentration = adjustedConcentration
                        });
                    }
                }
                if (points.Count > 0)
                {
                    double minConc = points.Min(p => p.Concentration);
                    double maxConc = points.Max(p => p.Concentration);
                    double avgConc = points.Average(p => p.Concentration);

                    Console.WriteLine($"Рассчитано {points.Count} точек");
                    Console.WriteLine($"Минимальная концентрация: {minConc}");
                    Console.WriteLine($"Максимальная концентрация: {maxConc}");
                    Console.WriteLine($"Средняя концентрация: {avgConc}");
                }
                else
                {
                    Console.WriteLine("Не удалось рассчитать ни одной точки с ненулевой концентрацией");
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
            double R = 6371; // Радиус Земли в км
            double dLat = (p2.Lat - p1.Lat) * Math.PI / 180.0;
            double dLon = (p2.Lng - p1.Lng) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(p1.Lat * Math.PI / 180.0) * Math.Cos(p2.Lat * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        private double CalculateConcentration(double C0, double r0, double r, double alpha)
        {
            return C0 * Math.Pow(r0 / r, alpha) * Math.Exp(-(r - r0) * (r - r0) / (2 * r0 * r0));
        }

        // Преобразование строки в число с обработкой различных форматов
        public static double ParseStringToDouble(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0.0;

            try
            {
                string cleanInput = new string(input.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                cleanInput = cleanInput.Replace(",", ".");
                if (string.IsNullOrWhiteSpace(cleanInput)) return 0.0;
                if (double.TryParse(cleanInput, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var result))
                {
                    return Math.Round(result, 3);
                }
                var match = System.Text.RegularExpressions.Regex.Match(input, @"(\d+[.,]?\d*)");
                if (match.Success && double.TryParse(match.Groups[1].Value.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out result))
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
        private double GetAlphaForMetal(string metalName)
        {
            var alphaValues = new Dictionary<string, double>
            {
                ["Pb"] = 2.0,
                ["Cd"] = 1.8,
                ["Hg"] = 1.5,
                ["As"] = 1.7,
                ["Ni"] = 1.9
            };

            return alphaValues.TryGetValue(metalName, out var alpha) ? alpha : 1.8;
        }
        public void SetWindRoseData(WindRoseData windRose)
        {
            _windRose = windRose ?? new WindRoseData();
        }
    }
}