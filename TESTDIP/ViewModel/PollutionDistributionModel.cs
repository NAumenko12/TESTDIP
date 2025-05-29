using DocumentFormat.OpenXml.Bibliography;
using GMap.NET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TESTDIP.DataBase;
using TESTDIP.Model;

namespace TESTDIP.ViewModel
{
    public class PollutionDistributionModel
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly List<WindDirection> _windProfile;

        public PollutionDistributionModel(DatabaseHelper dbHelper, List<WindDirection> windProfile)
        {
            _dbHelper = dbHelper;
            _windProfile = windProfile;
        }

        public List<GridPoint> CalculatePollutionField(PointLatLng sourcePoint,
                                      Location referencePoint,
                                      Metal metal,
                                      int year,
                                      double gridStepKm = 0.7,
                                      double areaSizeKm = 100.0)
        {
            var points = new List<GridPoint>();

            try
            {
                // Получаем данные опорной точки
                double Q0 = GetBaseConcentration(referencePoint, metal, year);
                double r0 = ParseDistance(referencePoint.DistanceFromSource);

                // Рассчитываем угол опорной точки относительно источника
                double phi0 = CalculateAngle(sourcePoint, new PointLatLng(referencePoint.Latitude, referencePoint.Longitude));
                double P0 = CalculateWindProbability(phi0);

                // ИСПРАВЛЕНИЕ: Добавляем коэффициенты для реалистичности
                double alpha = GetAlphaForMetal(metal.Name);
                double decayFactor = GetDecayFactor(metal.Name);
                double lambda = GetCharacteristicLength(metal.Name);

                // Рассчитываем параметр θ с учетом затухания
                //double theta = Q0 * Math.Pow(r0, alpha) * decayFactor / P0;
                double theta = Q0 * r0 / P0; // Убираем лишние коэффициенты

                Console.WriteLine($"Опорная точка: Q0={Q0}, r0={r0}, φ0={phi0:F1}°, P0={P0:F3}");
                Console.WriteLine($"Параметры: α={alpha}, затухание={decayFactor}, λ={lambda}, θ={theta:F3}");

                // Преобразуем километры в градусы
                double latStep = gridStepKm / 110.574;
                double lonStep = gridStepKm / (111.320 * Math.Cos(sourcePoint.Lat * Math.PI / 180));


                Console.WriteLine($"Опорная точка: Q0={Q0}, r0={r0}, φ0={phi0:F1}°, P0={P0:F3}");
                Console.WriteLine($"Параметр θ = {theta:F3}");

                // В цикле расчета:
                for (double lat = sourcePoint.Lat - areaSizeKm / 110.574;
                     lat <= sourcePoint.Lat + areaSizeKm / 110.574;
                     lat += latStep)
                {
                    for (double lon = sourcePoint.Lng - areaSizeKm / (111.320 * Math.Cos(lat * Math.PI / 180));
                         lon <= sourcePoint.Lng + areaSizeKm / (111.320 * Math.Cos(lat * Math.PI / 180));
                         lon += lonStep)
                    {
                        var targetPoint = new PointLatLng(lat, lon);
                        double r = CalculateDistance(sourcePoint, targetPoint);

                        if (r < 0.1) continue; // Избегаем деления на ноль
                        if (r > areaSizeKm) continue; // Ограничиваем область расчета

                        double phi = CalculateAngle(sourcePoint, targetPoint);
                        double P = CalculateWindProbability(phi);

                        // ИСПРАВЛЕНИЕ: Используем простую формулу из документа Q = θ·P(φ) / r
                        double Q = theta * P / r;

                        points.Add(new GridPoint
                        {
                            Lat = lat,
                            Lon = lon,
                            Concentration = Math.Max(0.001, Q), // Минимальная концентрация
                            DistanceFromSource = r
                        });
                    }
                }

                Console.WriteLine($"Создано {points.Count} точек с учетом розы ветров");

                // Проверяем результат на опорном расстоянии
                var testPoint = points.FirstOrDefault(p => Math.Abs(p.DistanceFromSource - r0) < 1.0);
                if (testPoint != null)
                {
                    Console.WriteLine($"Проверка: на расстоянии {testPoint.DistanceFromSource:F1} км концентрация = {testPoint.Concentration:F3} мг/м³");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при расчете поля загрязнения: {ex.Message}");
            }

            return points;
        }

        // Добавляем те же методы, что и в ConcentrationCalculator
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

        // Остальные методы остаются без изменений...
        private double CalculateDistance(PointLatLng p1, PointLatLng p2)
        {
            const double R = 6371;
            double dLat = (p2.Lat - p1.Lat) * Math.PI / 180;
            double dLon = (p2.Lng - p1.Lng) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(p1.Lat * Math.PI / 180) * Math.Cos(p2.Lat * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double CalculateAngle(PointLatLng source, PointLatLng target)
        {
            double dx = target.Lng - source.Lng;
            double dy = target.Lat - source.Lat;
            double phi = Math.Atan2(dy, dx) * 180 / Math.PI;
            phi = (phi + 360) % 360;
            return phi;
        }

        private double CalculateWindProbability(double phi)
        {
            if (_windProfile == null || _windProfile.Count == 0)
                return 1.0;

            var sortedDirections = _windProfile
                .Select(w => new {
                    Direction = w,
                    Distance = Math.Min(Math.Abs(w.AngleDegrees - phi), 360 - Math.Abs(w.AngleDegrees - phi))
                })
                .OrderBy(x => x.Distance)
                .Take(2)
                .ToList();

            if (sortedDirections.Count == 1)
                return sortedDirections[0].Direction.Weight;

            var dir1 = sortedDirections[0].Direction;
            var dir2 = sortedDirections[1].Direction;

            double phi1 = dir1.AngleDegrees;
            double phi2 = dir2.AngleDegrees;
            double p1 = dir1.Weight;
            double p2 = dir2.Weight;

            if (Math.Abs(phi1 - phi2) > 180)
            {
                if (phi1 < phi2) phi1 += 360;
                else phi2 += 360;
                if (phi < 180) phi += 360;
            }

            if (Math.Abs(phi2 - phi1) < 0.001) return p1;

            double t = (phi - phi1) / (phi2 - phi1);
            double result = p1 * (1 - t) + p2 * t;

            return Math.Max(0.001, result);
        }

        private double GetBaseConcentration(Location point, Metal metal, int year)
        {
            double? conc = _dbHelper.GetMetalConcentration(point.Id, metal.Id, year);
            if (!conc.HasValue)
                throw new Exception($"Нет данных для {metal.Name} в точке {point.Name} за {year} год");
            return conc.Value;
        }

        private double ParseDistance(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new Exception("Расстояние не указано");

            try
            {
                string cleanInput = new string(input.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                cleanInput = cleanInput.Replace(",", ".");

                if (double.TryParse(cleanInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    return result;

                throw new Exception($"Не удалось преобразовать расстояние: {input}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при обработке расстояния '{input}': {ex.Message}");
            }
        }
    }
}