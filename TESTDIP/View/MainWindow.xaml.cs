using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using GMap.NET;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TESTDIP.Model;
using Location = TESTDIP.Model.Location;
using TESTDIP.DataBase;
using TESTDIP.View;
using System.IO;
using TESTDIP.ViewModel;

namespace TESTDIP;


public partial class MainWindow : Window
{
    private readonly PointLatLng ReferencePoint = new PointLatLng(67.923840, 32.840962);
    private readonly List<GMapPolygon> _pollutionPolygons = new List<GMapPolygon>();
    private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
    private GMapMarker _plantMarker;
    private Metal _lastUsedMetal;
    private int _lastUsedYear;
    private List<GridPoint> _lastCalculationResults = new List<GridPoint>();

    public MainWindow()
    {
        InitializeComponent();
        InitializeMap();
        LoadLocations();
        AddPlantMarker();
    }

    private void InitializeMap()
    {
        MapControl.MapProvider = GMapProviders.GoogleMap;
        MapControl.Position = ReferencePoint;
        MapControl.MinZoom = 2;
        MapControl.MaxZoom = 18;
        MapControl.Zoom = 10;
        MapControl.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
        MapControl.CanDragMap = true;
        MapControl.MouseDown += MapControl_MouseDown;
    }

    private void AddPlantMarker()
    {

        _plantMarker = new GMapMarker(ReferencePoint)
        {
            Shape = new Image
            {
                Source = new BitmapImage(new Uri("C:\\Users\\natac\\source\\repos\\TESTDIP\\TESTDIP\\recources\\zavod.ico")),
                Width = 32,
                Height = 32,
                ToolTip = "Комбинат"
            },
            Offset = new System.Windows.Point(-16, -16)
        };

        MapControl.Markers.Add(_plantMarker);
    }

    public void LoadLocations()
    {
        var locations = _dbHelper.GetLocations();
        UpdateMap(locations);
    }



    private void UpdateMap(List<Location> locations)
    {
        var plantMarker = MapControl.Markers.FirstOrDefault(m => m == _plantMarker);
        var markersToKeep = MapControl.Markers.OfType<GMapPolygon>().ToList();

        MapControl.Markers.Clear();

        foreach (var polygon in markersToKeep)
        {
            MapControl.Markers.Add(polygon);
        }
        if (plantMarker != null)
        {
            MapControl.Markers.Add(plantMarker);
        }

        foreach (var location in locations)
        {
            var marker = new GMapMarker(new PointLatLng(location.Latitude, location.Longitude))
            {
                Shape = new System.Windows.Shapes.Ellipse
                {
                    Fill = GetDistanceColor(location),
                    Width = 10,
                    Height = 10,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                },
                Tag = location
            };

            ToolTipService.SetToolTip(marker.Shape, location.SiteNumber);

            MapControl.Markers.Add(marker);
        }

        MapControl.ZoomAndCenterMarkers(null);
    }

    private Brush GetDistanceColor(Location location)
    {
        double distance = CalculateDistance(ReferencePoint,
            new PointLatLng(location.Latitude, location.Longitude));

        if (distance <= 4)
            return Brushes.Transparent;

        return distance <= 10 ? Brushes.Red :
               distance <= 40 ? Brushes.Orange :
               Brushes.Green;
    }
    private void MapControl_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var clickPoint = e.GetPosition(MapControl);
        var gClickPoint = new GPoint((int)clickPoint.X, (int)clickPoint.Y);
        var clickPosition = MapControl.FromLocalToLatLng((int)gClickPoint.X, (int)gClickPoint.Y);

        foreach (var marker in MapControl.Markers)
        {
            var gMarkerPoint = MapControl.FromLatLngToLocal(marker.Position);
            if (Math.Abs(gMarkerPoint.X - gClickPoint.X) < 10 && Math.Abs(gMarkerPoint.Y - gClickPoint.Y) < 10)
            {
                if (marker.Tag is Location location)
                {
                    var samples = _dbHelper.GetSamplesForLocation(location.Id);
                    var samplesWindow = new SamplesWindow(location, samples);
                    samplesWindow.ShowDialog();
                    break;
                }
            }
        }
    }
    private void AddPointButton_Click(object sender, RoutedEventArgs e)
    {
        var addLocationWindow = new AddLocationWindow();
        if (addLocationWindow.ShowDialog() == true && addLocationWindow.Location != null)
        {
            _dbHelper.AddLocation(addLocationWindow.Location);

            LoadLocations();
        }
    }
    private double CalculateDistance(PointLatLng point1, PointLatLng point2)
    {
        const double EarthRadiusKm = 6371;
        double lat1 = ToRadians(point1.Lat);
        double lon1 = ToRadians(point1.Lng);
        double lat2 = ToRadians(point2.Lat);
        double lon2 = ToRadians(point2.Lng);

        double dLat = lat2 - lat1;
        double dLon = lon2 - lon1;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }
    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
    private readonly List<GMapPolygon> _concentrationPolygons = new List<GMapPolygon>();

    private void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new CalculateDialog(_dbHelper, ReferencePoint);
            if (dialog.ShowDialog() == true)
            {
                CalculateAndDrawPollutionField(
                    dialog.SelectedLocation,
                    dialog.SelectedMetal,
                    dialog.SelectedYear,
                    dialog.UseWindRose);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}");
        }
    }


    private void CalculateAndDrawPollutionField(Location referencePoint, Metal metal, int year, bool useWindRose)
    {
        _lastUsedMetal = metal;
        _lastUsedYear = year;

        try
        {
            if (referencePoint == null)
            {
                MessageBox.Show("Не выбрана опорная точка");
                return;
            }

            if (metal == null)
            {
                MessageBox.Show("Не выбран металл");
                return;
            }

            ClearConcentrationFields();
            List<GridPoint> grid;

            if (useWindRose)
            {
                var windProfile = new WindProfileHelper().GetDefaultWindProfile16Rumbs();
                var calculator = new PollutionDistributionModel(_dbHelper, windProfile);

                grid = calculator.CalculatePollutionField(
                    ReferencePoint,
                    referencePoint,
                    metal,
                    year,
                    gridStepKm: 1.0,
                    areaSizeKm: 100.0);
            }
            else
            {
                var calculator = new ConcentrationCalculator(_dbHelper);
                grid = calculator.CalculateField(
                    ReferencePoint,
                    referencePoint,
                    metal,
                    year,
                    gridStepKm: 0.7,
                    areaSizeKm: 100.0);
            }

            _lastCalculationResults = grid ?? new List<GridPoint>();

            if (grid == null || grid.Count == 0)
            {
                MessageBox.Show("Нет данных для построения поля. Проверьте:\n" +
                                "1. Наличие данных для выбранного года и металла\n" +
                                "2. Корректность опорной точки");
                return;
            }

            if (useWindRose)
            {
                // ИСПОЛЬЗУЕМ НОВУЮ ФУНКЦИЮ с переменной длиной лепестков
                CreateWindRosePetals(grid);
            }
            else
            {
                // Старая логика для обычных изолиний
                double[] levels = new[] { 0.1, 0.5, 1.0, 2.0, 5.0 };
                foreach (var level in levels)
                {
                    var contourPoints = FindContourPoints(grid, level);
                    if (contourPoints != null && contourPoints.Count > 2)
                    {
                        var polygon = CreateContourPolygon(contourPoints, GetColorForLevel(level), level);
                        _concentrationPolygons.Add(polygon);
                        MapControl.Markers.Add(polygon);
                    }
                }
            }

            ZoomToPollutionArea(grid);
            ShowWindRoseLegend(useWindRose, grid);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при построении поля: {ex.Message}\n\nСтек вызовов: {ex.StackTrace}");
        }
    }
    private Dictionary<int, List<GridPoint>> GroupPointsBySectors(List<GridPoint> grid, double centerLat, double centerLon)
    {
        var sectors = new Dictionary<int, List<GridPoint>>();

        foreach (var point in grid.Where(p => p.Concentration > 0.001))
        {
            // Вычисляем угол от центра к точке
            double deltaLat = point.Lat - centerLat;
            double deltaLon = point.Lon - centerLon;
            double angleRad = Math.Atan2(deltaLon, deltaLat);
            double angleDeg = angleRad * 180.0 / Math.PI;

            // Нормализуем угол к диапазону 0-360
            if (angleDeg < 0) angleDeg += 360;

            // Определяем сектор (0-15, каждый сектор = 22.5°)
            int sector = (int)Math.Round(angleDeg / 22.5) % 16;

            if (!sectors.ContainsKey(sector))
                sectors[sector] = new List<GridPoint>();

            sectors[sector].Add(point);
        }

        return sectors;
    }
    private void CreateWindRosePetals(List<GridPoint> grid)
    {
        try
        {
            var centerLat = ReferencePoint.Lat;
            var centerLon = ReferencePoint.Lng;

            var windProfile = new WindProfileHelper().GetDefaultWindProfile16Rumbs();
            var sectors = GroupPointsBySectors(grid, centerLat, centerLon);
            for (int sectorIndex = 0; sectorIndex < 16; sectorIndex++)
            {
                var windDirection = windProfile[sectorIndex];
                var sectorPoints = sectors.ContainsKey(sectorIndex) ? sectors[sectorIndex] : new List<GridPoint>();

                CreateVariableLengthPetal(sectorIndex, windDirection, sectorPoints, centerLat, centerLon);
            }

            Console.WriteLine($"Создано {_concentrationPolygons.Count} лепестков розы ветров с переменной длиной");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при создании лепестков: {ex.Message}");
        }
    }

    private void CreateVariableLengthPetal(int sectorIndex, WindDirection windDirection,
        List<GridPoint> sectorPoints, double centerLat, double centerLon)
    {
        try
        {
            double sectorAngle = sectorIndex * 22.5;
            double halfSectorWidth = 11.25; 

            double leftAngle = (sectorAngle - halfSectorWidth) * Math.PI / 180.0;
            double rightAngle = (sectorAngle + halfSectorWidth) * Math.PI / 180.0;
            double baseMaxDistance = 100.0; 

            double windLengthFactor = windDirection.Weight / 0.18; 

            double dataFactor = 1.0;
            if (sectorPoints.Any())
            {
                double avgConcentration = sectorPoints.Average(p => p.Concentration);
                double maxConcentration = sectorPoints.Max(p => p.Concentration);
                dataFactor = 0.5 + (avgConcentration / maxConcentration) * 0.5; 
            }
            double maxPetalLength = baseMaxDistance * windLengthFactor * dataFactor;
            maxPetalLength = Math.Max(5.0, maxPetalLength); 

            Console.WriteLine($"Сектор {sectorIndex} ({GetWindDirectionName(sectorIndex)}): " +
                            $"Weight={windDirection.Weight:F3}, Length={maxPetalLength:F1} км");
            for (int zone = 0; zone < 5; zone++)
            {
                double innerRadius = (zone * maxPetalLength) / 5.0;
                double outerRadius = ((zone + 1) * maxPetalLength) / 5.0;
                var zonePoints = sectorPoints
                    .Where(p => p.DistanceFromSource >= innerRadius && p.DistanceFromSource < outerRadius)
                    .ToList();

                double avgConcentration = zonePoints.Any() ? zonePoints.Average(p => p.Concentration) : 0.001;

                var petalPoints = CreatePetalZonePoints(centerLat, centerLon, leftAngle, rightAngle, innerRadius, outerRadius);

                if (petalPoints.Count > 2)
                {
                    var color = GetRedYellowGreenColor(zone, avgConcentration);
                    var polygon = CreatePetalPolygon(petalPoints, color, avgConcentration, sectorIndex, zone);
                    _concentrationPolygons.Add(polygon);
                    MapControl.Markers.Add(polygon);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при создании лепестка сектора {sectorIndex}: {ex.Message}");
        }
    }
    private Color GetRedYellowGreenColor(int zone, double concentration)
    {
        
        double normalizedZone = (double)zone / 4.0;
        double intensity = Math.Max(0.3, Math.Min(1.0, Math.Log10(concentration * 1000 + 1) / 3.0));

        byte red, green, blue;

        if (normalizedZone <= 0.5)
        {
            
            double t = normalizedZone * 2; 
            red = 255;
            green = (byte)(255 * t * intensity);
            blue = 0;
        }
        else
        {
            double t = (normalizedZone - 0.5) * 2; 
            red = (byte)(255 * (1 - t) * intensity);
            green = (byte)(255 * intensity);
            blue = 0;
        }

        return Color.FromRgb(red, green, blue);
    }


    private List<PointLatLng> CreatePetalZonePoints(double centerLat, double centerLon,
        double leftAngle, double rightAngle, double innerRadius, double outerRadius)
    {
        var points = new List<PointLatLng>();

        double latStep = 1.0 / 110.574;
        double lonStep = 1.0 / (111.320 * Math.Cos(centerLat * Math.PI / 180));

        if (innerRadius > 0.1)
        {
            for (double angle = leftAngle; angle <= rightAngle; angle += 0.1)
            {
                double lat = centerLat + (innerRadius * latStep) * Math.Sin(angle);
                double lon = centerLon + (innerRadius * lonStep) * Math.Cos(angle);
                points.Add(new PointLatLng(lat, lon));
            }
        }

        for (double angle = rightAngle; angle >= leftAngle; angle -= 0.1)
        {
            double lat = centerLat + (outerRadius * latStep) * Math.Sin(angle);
            double lon = centerLon + (outerRadius * lonStep) * Math.Cos(angle);
            points.Add(new PointLatLng(lat, lon));
        }

        if (points.Count > 0 && innerRadius > 0.1)
        {
            points.Add(points[0]);
        }

        return points;
    }

    private GMapPolygon CreatePetalPolygon(List<PointLatLng> points, Color color, double concentration, int sector, int zone)
    {
        var polygon = new GMapPolygon(points);

        var gradientBrush = new RadialGradientBrush();

        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(160, color.R, color.G, color.B), 0.0));
        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(120, color.R, color.G, color.B), 0.5));
        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(80, color.R, color.G, color.B), 1.0));

        polygon.Shape = new System.Windows.Shapes.Path
        {
            Stroke = new SolidColorBrush(Color.FromArgb(120, color.R, color.G, color.B)),
            StrokeThickness = 0.5,
            Fill = gradientBrush,
            ToolTip = $"Направление: {GetWindDirectionName(sector)}\n" +
                     $"Зона: {zone + 1} (от центра)\n" +
                     $"Концентрация: {concentration:F4} мг/м³"
        };

        polygon.Tag = new { Sector = sector, Zone = zone, Concentration = concentration };

        return polygon;
    }
    private string GetWindDirectionName(int sector)
    {
        string[] directions = {
                "С", "ССВ", "СВ", "ВСВ", "В", "ВЮВ", "ЮВ", "ЮЮВ",
                "Ю", "ЮЮЗ", "ЮЗ", "ЗЮЗ", "З", "ЗСЗ", "СЗ", "ССЗ"
            };
        return directions[sector % 16];
    }

    private void ShowWindRoseLegend(bool isWindRose, List<GridPoint> grid)
    {
        LegendGrid.Children.Clear();

        var legendPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Background = new SolidColorBrush(Colors.White),
            Margin = new Thickness(10),
            Opacity = 0.9
        };

        if (isWindRose)
        {
            legendPanel.Children.Add(new TextBlock
            {
                Text = "Роза ветров - Концентрация",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            string[] zoneNames = { "Центр (красный)", "Зона 2", "Средняя (желтый)", "Зона 4", "Край (зеленый)" };

            for (int zone = 0; zone < 5; zone++)
            {
                var color = GetRedYellowGreenColor(zone, 1.0); 

                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                stack.Children.Add(new Rectangle
                {
                    Width = 20,
                    Height = 10,
                    Fill = new SolidColorBrush(color),
                    Margin = new Thickness(0, 0, 5, 0)
                });

                stack.Children.Add(new TextBlock
                {
                    Text = zoneNames[zone],
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 10
                });

                legendPanel.Children.Add(stack);
            }
            legendPanel.Children.Add(new TextBlock
            {
                Text = "Длина лепестка ∝ сила ветра",
                FontSize = 9,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 5, 0, 0)
            });
        }
        else
        {
            
            ShowLegendAlternative(new[] { 0.1, 0.5, 1.0, 2.0, 5.0 });
            return;
        }

        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Colors.Black),
            BorderThickness = new Thickness(1),
            Child = legendPanel
        };

        LegendGrid.Children.Add(border);
    }


    private static List<PointLatLng> FindContourPoints(List<GridPoint> grid, double targetLevel)
    {
        var contourPoints = new List<PointLatLng>();

        try
        {
            if (grid == null || grid.Count == 0)
            {
                return contourPoints;
            }

            
            var tolerance = Math.Max(targetLevel * 0.4, 0.01); 
            var candidatePoints = grid
                .Where(p => p != null &&
                       p.Concentration >= targetLevel * 0.7 &&
                       p.Concentration <= targetLevel * 1.3)
                .OrderBy(p => Math.Abs(p.Concentration - targetLevel))
                .Take(200) 
                .ToList();

            if (candidatePoints.Count < 4)
            {
                
                candidatePoints = grid
                    .Where(p => p != null && p.Concentration > 0)
                    .OrderBy(p => Math.Abs(p.Concentration - targetLevel))
                    .Take(50)
                    .ToList();
            }

            if (candidatePoints.Count < 3) return contourPoints;

            
            var centerLat = candidatePoints.Average(p => p.Lat);
            var centerLon = candidatePoints.Average(p => p.Lon);

            
            var sectorPoints = new Dictionary<int, GridPoint>();

            foreach (var point in candidatePoints)
            {
                
                double deltaLat = point.Lat - centerLat;
                double deltaLon = point.Lon - centerLon;
                double angleRad = Math.Atan2(deltaLon, deltaLat); 
                double angleDeg = angleRad * 180.0 / Math.PI;

                
                if (angleDeg < 0) angleDeg += 360;

                
                int sector = (int)Math.Round(angleDeg / 22.5) % 16;

               
                if (!sectorPoints.ContainsKey(sector))
                {
                    sectorPoints[sector] = point;
                }
                else
                {
                    var existing = sectorPoints[sector];
                    double existingDistance = Math.Sqrt(
                        Math.Pow(existing.Lat - centerLat, 2) +
                        Math.Pow(existing.Lon - centerLon, 2));
                    double currentDistance = Math.Sqrt(
                        Math.Pow(point.Lat - centerLat, 2) +
                        Math.Pow(point.Lon - centerLon, 2));

                   
                    if (Math.Abs(point.Concentration - targetLevel) < Math.Abs(existing.Concentration - targetLevel) ||
                        (Math.Abs(point.Concentration - existing.Concentration) < targetLevel * 0.1 && currentDistance > existingDistance))
                    {
                        sectorPoints[sector] = point;
                    }
                }
            }
            var orderedPoints = new List<PointLatLng>();

            for (int sector = 0; sector < 16; sector++)
            {
                if (sectorPoints.ContainsKey(sector))
                {
                    var point = sectorPoints[sector];
                    orderedPoints.Add(new PointLatLng(point.Lat, point.Lon));
                }
                else
                {
                    var prevPoint = FindNearestSectorPoint(sectorPoints, sector, -1);
                    var nextPoint = FindNearestSectorPoint(sectorPoints, sector, 1);

                    if (prevPoint != null && nextPoint != null)
                    {
                        var interpolated = new PointLatLng(
                            (prevPoint.Lat + nextPoint.Lat) / 2,
                            (prevPoint.Lng + nextPoint.Lng) / 2);
                        orderedPoints.Add(interpolated);
                    }
                    else if (prevPoint != null)
                    {
                        orderedPoints.Add(prevPoint);
                    }
                    else if (nextPoint != null)
                    {
                        orderedPoints.Add(nextPoint);
                    }
                }
            }
            if (orderedPoints.Count > 0 && orderedPoints.Count < 16)
            {
                contourPoints = CreateCircularContour(centerLat, centerLon, candidatePoints, targetLevel);
            }
            else
            {
                contourPoints = orderedPoints;
            }

            Console.WriteLine($"Создан контур розы ветров: {contourPoints.Count} точек для уровня {targetLevel:F3}");
            return contourPoints;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при создании контура: {ex.Message}");
            return contourPoints;
        }
    }
    private static PointLatLng FindNearestSectorPoint(Dictionary<int, GridPoint> sectorPoints, int targetSector, int direction)
    {
        for (int i = 1; i < 8; i++)
        {
            int checkSector = (targetSector + direction * i + 16) % 16;
            if (sectorPoints.ContainsKey(checkSector))
            {
                var point = sectorPoints[checkSector];
                return new PointLatLng(point.Lat, point.Lon);
            }
        }
        return default(PointLatLng); 
    }

    private static List<PointLatLng> CreateCircularContour(double centerLat, double centerLon,
        List<GridPoint> points, double targetLevel)
    {
        var contour = new List<PointLatLng>();
        var relevantPoints = points
            .Where(p => Math.Abs(p.Concentration - targetLevel) < targetLevel * 0.5)
            .ToList();

        if (!relevantPoints.Any()) return contour;

        double avgRadius = relevantPoints.Average(p =>
            Math.Sqrt(Math.Pow(p.Lat - centerLat, 2) + Math.Pow(p.Lon - centerLon, 2)));
        for (int i = 0; i < 16; i++)
        {
            double angle = i * 22.5 * Math.PI / 180.0;
            double lat = centerLat + avgRadius * Math.Cos(angle);
            double lon = centerLon + avgRadius * Math.Sin(angle);
            contour.Add(new PointLatLng(lat, lon));
        }

        return contour;
    }
    public bool HasCalculationData => _concentrationPolygons.Count > 0;

    private void ShowCalculationDataBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!HasCalculationData || _lastCalculationResults.Count == 0)
        {
            MessageBox.Show("Сначала выполните расчет концентраций", "Нет данных",
                           MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
         
            var dialog = new CalculationDataWindow(
                _lastCalculationResults,
                _lastUsedMetal,
                _lastUsedYear,
                _dbHelper);

            dialog.Owner = this;
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private GMapPolygon CreateContourPolygon(List<PointLatLng> points, Color color, double level)
    {
        var polygon = new GMapPolygon(points)
        {
            Shape = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(80, color.R, color.G, color.B)),
                ToolTip = $"Уровень {level:0.0} мг/м³"
            },
            Tag = level
        };

        return polygon;
    }

    private void ZoomToPollutionArea(List<GridPoint> grid)
    {
        if (grid.Count == 0) return;

        double minLat = grid.Min(p => p.Lat);
        double maxLat = grid.Max(p => p.Lat);
        double minLon = grid.Min(p => p.Lon);
        double maxLon = grid.Max(p => p.Lon);

        var rect = new RectLatLng(maxLat, minLon, maxLon - minLon, maxLat - minLat);
        MapControl.SetZoomToFitRect(rect);
    }

    private void ShowLegendAlternative(double[] levels)
    {
        LegendGrid.Children.Clear();

        var legendPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Background = new SolidColorBrush(Colors.White),
            Margin = new Thickness(10),
            Opacity = 0.9
        };

  
        legendPanel.Children.Add(new TextBlock
        {
            Text = "Концентрация (мг/м³)",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        });

        foreach (var level in levels.OrderByDescending(l => l))
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            stack.Children.Add(new Rectangle
            {
                Width = 20,
                Height = 10,
                Fill = new SolidColorBrush(GetColorForLevel(level)),
                Margin = new Thickness(0, 0, 5, 0)
            });

            stack.Children.Add(new TextBlock
            {
                Text = level.ToString("0.00"),
                VerticalAlignment = VerticalAlignment.Center
            });

            legendPanel.Children.Add(stack);
        }

        
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Colors.Black),
            BorderThickness = new Thickness(1),
            Child = legendPanel
        };

        LegendGrid.Children.Add(border);
    }

    private Color GetColorForLevel(double level)
    {
        
        if (level < 0.1) return Colors.Green;
        if (level < 0.5) return Colors.YellowGreen;
        if (level < 1.0) return Colors.Yellow;
        if (level < 2.0) return Colors.Orange;
        return Colors.Red;
    }

    private void ClearConcentrationFields()
    {
        foreach (var poly in _concentrationPolygons)
        {
            MapControl.Markers.Remove(poly);
        }
        _concentrationPolygons.Clear();

        _lastCalculationResults.Clear();

        LegendGrid.Children.Clear();
    }

    private void GoToStatsButton_Click(object sender, RoutedEventArgs e)
    {
        var statsWindow = new StatisticsWindow();

        statsWindow.Show();
    }
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {

            ClearConcentrationFields();
            LegendGrid.Children.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сбросе: {ex.Message}");
        }
    }
    private void ViewAllCalculationResultsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var resultsWindow = new TESTDIP.View.CalculationResultsWindow();
            resultsWindow.Owner = this;
            resultsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии окна результатов: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG Image|*.png|JPEG Image|*.jpg",
            Title = "Сохранить карту как изображение"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                var renderBitmap = new RenderTargetBitmap(
                    (int)MapControl.ActualWidth,
                    (int)MapControl.ActualHeight,
                    96d, 96d, PixelFormats.Pbgra32);

                renderBitmap.Render(MapControl);
                BitmapEncoder encoder;
                if (saveFileDialog.FilterIndex == 1)
                    encoder = new PngBitmapEncoder();
                else
                    encoder = new JpegBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                MessageBox.Show("Карта успешно экспортирована!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте карты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}