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
                // Проверка входных параметров
                if (referencePoint == null || metal == null || _dbHelper == null)
                {
                    Console.WriteLine("Ошибка: Один из параметров равен null");
                    return points;
                }

                // Получаем концентрацию в опорной точке
                double? concentration = _dbHelper.GetMetalConcentration(
                    referencePoint.Id,
                    metal.Id,
                    year);

                if (!concentration.HasValue)
                {
                    Console.WriteLine($"Нет данных о концентрации для локации {referencePoint.Id}, металла {metal.Id}, года {year}");
                    return points;
                }

                // Проверяем, что строка с расстоянием не пустая
                if (string.IsNullOrWhiteSpace(referencePoint.DistanceFromSource))
                {
                    Console.WriteLine($"Ошибка: Расстояние от источника для локации {referencePoint.Id} не указано");
                    return points;
                }

                double C0 = concentration.Value;
                double r0 = ParseStringToDouble(referencePoint.DistanceFromSource);

                // Безопасное преобразование строки в число
                try
                {
                    r0 = ParseStringToDouble(referencePoint.DistanceFromSource);
                    if (r0 <= 0)
                    {
                        Console.WriteLine($"Ошибка: Некорректное расстояние от источника: {referencePoint.DistanceFromSource}");
                        return points;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при преобразовании расстояния: {ex.Message}");
                    return points;
                }

                double alpha = GetAlphaForMetal(metal.Name);

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

                        double C = CalculateConcentration(C0, r0, r, alpha);
                        points.Add(new GridPoint { Lat = lat, Lon = lon, Concentration = C });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при расчете поля концентрации: {ex.Message}");
                // Возвращаем пустой список в случае ошибки
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
        private double CalculateConcentration(double C0, double r0, double r, double alpha)
        {
            return C0 * Math.Pow(r0 / r, alpha) * Math.Exp(-(r - r0) * (r - r0) / (2 * r0 * r0));
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
        private double GetAlphaForMetal(string metalName)
        {
            // Коэффициенты затухания для разных металлов
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
    }
}
