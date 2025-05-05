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
using System.Windows.Threading;

namespace TESTDIP;


public partial class MainWindow : Window
{
    private readonly PointLatLng ReferencePoint = new PointLatLng(67.923840, 32.840962);
    private readonly List<GMapPolygon> _pollutionPolygons = new List<GMapPolygon>();
    private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
    private GMapMarker _plantMarker;

    public MainWindow()
    {
        InitializeComponent();
        InitializeMap();
        AddPlantMarker();
        LoadLocations();

        // Задержка для инициализации карты
        Dispatcher.BeginInvoke(new Action(() =>
        {
            MapControl.Position = ReferencePoint;
            MapControl.Zoom = 14; 
        }), DispatcherPriority.Loaded);
    }

    private void InitializeMap()
    {
        MapControl.MapProvider = GMapProviders.GoogleMap;
        MapControl.MinZoom = 2;
        MapControl.MaxZoom = 18;
        MapControl.Zoom = 10; 
        MapControl.CanDragMap = true;
        MapControl.MouseDown += MapControl_MouseDown;

        MapControl.Position = ReferencePoint;
        MapControl.Zoom = 12; 
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
        try
        {
            MapControl.Markers.Clear();
            if (_plantMarker != null)
            {
                MapControl.Markers.Add(_plantMarker);
            }
            foreach (var location in locations)
            {
                if (location == null) continue;

                var marker = new GMapMarker(new PointLatLng(location.Latitude, location.Longitude))
                {
                    Shape = new Ellipse
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
            if (MapControl.Markers.Count > 0)
            {
                var rect = GetMarkersBoundingBox();
                MapControl.SetZoomToFitRect(rect);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка обновления карты: {ex.Message}");
        }
    }
    private RectLatLng GetMarkersBoundingBox()
    {
        double minLat = double.MaxValue;
        double maxLat = double.MinValue;
        double minLon = double.MaxValue;
        double maxLon = double.MinValue;

        foreach (var marker in MapControl.Markers)
        {
            if (marker.Position.Lat < minLat) minLat = marker.Position.Lat;
            if (marker.Position.Lat > maxLat) maxLat = marker.Position.Lat;
            if (marker.Position.Lng < minLon) minLon = marker.Position.Lng;
            if (marker.Position.Lng > maxLon) maxLon = marker.Position.Lng;
        }

        // Добавляем небольшую буферную зону
        const double buffer = 0.01; // ~1 км
        return new RectLatLng(
            maxLat + buffer,
            minLon - buffer,
            (maxLon - minLon) + 2 * buffer,
            (maxLat - minLat) + 2 * buffer
        );
    }

    private Brush GetDistanceColor(Location location)
    {
        double distance = CalculateDistance(ReferencePoint,
            new PointLatLng(location.Latitude, location.Longitude));

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
                if (dialog.SelectedLocation == null)
                {
                    MessageBox.Show("Не выбрана опорная точка");
                    return;
                }

                if (dialog.SelectedMetal == null)
                {
                    MessageBox.Show("Не выбран металл");
                    return;
                }

                CalculateAndDrawPollutionField(
                    dialog.SelectedLocation,
                    dialog.SelectedMetal,
                    dialog.SelectedYear);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии диалога расчета: {ex.Message}");
        }
    }
    private void LoadWindRoseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                Title = "Выберите файл с данными розы ветров"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var windRose = WindRoseLoader.LoadFromFile(openFileDialog.FileName);
                _windRoseData = windRose;

                MessageBox.Show("Данные розы ветров успешно загружены!");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки розы ветров: {ex.Message}");
        }
    }
    private WindRoseData _windRoseData = new WindRoseData();

    private void CalculateAndDrawPollutionField(Location referencePoint, Metal metal, int year)
    {
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
            var calculator = new ConcentrationCalculator(_dbHelper);
            if (_windRoseData != null)
            {
                calculator.SetWindRoseData(_windRoseData);
            }
            var grid = calculator.CalculateField(
                ReferencePoint,
                referencePoint,
                metal,
                year,
                gridStepKm: 0.5, 
                areaSizeKm: 200.0);
            if (grid == null || grid.Count == 0)
            {
                MessageBox.Show("Нет данных для построения поля. Проверьте:\n" +
                              "1. Наличие данных для выбранного года и металла\n" +
                              "2. Корректность опорной точки");
                return;
            }
            double minConcentration = grid.Min(p => p.Concentration);
            double maxConcentration = grid.Max(p => p.Concentration);
            double avgConcentration = grid.Average(p => p.Concentration);

            Console.WriteLine($"Диапазон концентраций: мин={minConcentration}, макс={maxConcentration}, средняя={avgConcentration}");
            if (maxConcentration < 0.001)
            {
                MessageBox.Show("Рассчитанные концентрации слишком малы. Проверьте входные данные.");
                return;
            }
            double[] levels;
            if (maxConcentration - minConcentration < 0.1)
            {
                levels = new double[]
                {
                minConcentration,
                minConcentration + (maxConcentration - minConcentration) * 0.2,
                minConcentration + (maxConcentration - minConcentration) * 0.4,
                minConcentration + (maxConcentration - minConcentration) * 0.6,
                minConcentration + (maxConcentration - minConcentration) * 0.8,
                maxConcentration
                };
            }
            else
            {
                levels = new double[6];
                for (int i = 0; i < 6; i++)
                {
                    double factor = i / 5.0;
                    factor = Math.Pow(factor, 0.5); 
                    levels[i] = minConcentration + (maxConcentration - minConcentration) * factor;
                }
            }
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = Math.Round(levels[i], 3);
            }
            DrawHeatMap(grid, levels);
            ZoomToPollutionArea(grid);
            ShowLegend(levels, metal.Name);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при построении поля: {ex.Message}\n\nСтек вызовов: {ex.StackTrace}");
        }
    }
    private void DrawHeatMap(List<GridPoint> grid, double[] levels)
    {
        try
        {
            
            Console.WriteLine("Уровни концентрации:");
            foreach (var level in levels)
            {
                Console.WriteLine($"  {level}");
            }

            
            for (int i = levels.Length - 1; i >= 0; i--)
            {
                double level = levels[i];

                
                var pointsAboveLevel = grid.Where(p => p.Concentration >= level).ToList();

                Console.WriteLine($"Для уровня {level} найдено {pointsAboveLevel.Count} точек");

                if (pointsAboveLevel.Count < 3) continue;

                
                var polygons = CreateContourPolygons(pointsAboveLevel, level, levels.Min(), levels.Max());

                foreach (var polygon in polygons)
                {
                    _concentrationPolygons.Add(polygon);
                    MapControl.Markers.Add(polygon);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при отрисовке тепловой карты: {ex.Message}");
        }
    }
    private List<GMapPolygon> CreateContourPolygons(List<GridPoint> points, double level, double minLevel, double maxLevel)
    {
        var result = new List<GMapPolygon>();

        try
        {
            
            var convexHull = ComputeConvexHull(points.Select(p => new PointLatLng(p.Lat, p.Lon)).ToList());

            if (convexHull.Count < 3) return result;

            
            var polygon = new GMapPolygon(convexHull)
            {
                Shape = new System.Windows.Shapes.Path
                {
                    Stroke = new System.Windows.Media.SolidColorBrush(GetColorForLevel(level, minLevel, maxLevel)),
                    StrokeThickness = 2,
                    Fill = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(150,
                        GetColorForLevel(level, minLevel, maxLevel).R,
                        GetColorForLevel(level, minLevel, maxLevel).G,
                        GetColorForLevel(level, minLevel, maxLevel).B)),
                    ToolTip = $"Уровень {level.ToString("F3", new System.Globalization.CultureInfo("ru-RU")).TrimEnd('0').TrimEnd(',')} мг/м³"
                },
                Tag = level
            };

            result.Add(polygon);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при создании полигонов: {ex.Message}");
        }

        return result;
    }
    private List<PointLatLng> ComputeConvexHull(List<PointLatLng> points)
    {
        if (points.Count < 3) return points;

        
        var p0 = points.OrderBy(p => p.Lat).ThenBy(p => p.Lng).First();

        
        var sortedPoints = points.OrderBy(p =>
        {
            double dx = p.Lng - p0.Lng;
            double dy = p.Lat - p0.Lat;
            return Math.Atan2(dy, dx);
        }).ToList();

        
        var hull = new List<PointLatLng>();
        hull.Add(sortedPoints[0]);
        hull.Add(sortedPoints[1]);

        for (int i = 2; i < sortedPoints.Count; i++)
        {
            while (hull.Count > 1 && !IsLeftTurn(hull[hull.Count - 2], hull[hull.Count - 1], sortedPoints[i]))
            {
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(sortedPoints[i]);
        }

        return hull;
    }

    
    private bool IsLeftTurn(PointLatLng p1, PointLatLng p2, PointLatLng p3)
    {
        return ((p2.Lng - p1.Lng) * (p3.Lat - p1.Lat) - (p2.Lat - p1.Lat) * (p3.Lng - p1.Lng)) > 0;
    }

    
    private void ShowLegend(double[] levels, string metalName)
    {
        LegendGrid.Children.Clear();

        var legendPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Vertical,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
            Margin = new System.Windows.Thickness(10),
            Opacity = 0.9,
            MinWidth = 200
        };

 
        legendPanel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = $"Концентрация {metalName} (мг/м³)",
            FontWeight = System.Windows.FontWeights.Bold,
            Margin = new System.Windows.Thickness(0, 0, 0, 10),
            TextAlignment = System.Windows.TextAlignment.Center
        });

     
        legendPanel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "С учетом розы ветров",
            FontStyle = System.Windows.FontStyles.Italic,
            Margin = new System.Windows.Thickness(0, 0, 0, 10),
            TextAlignment = System.Windows.TextAlignment.Center
        });

       
        foreach (var level in levels.OrderByDescending(l => l))
        {
            var stack = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new System.Windows.Thickness(5)
            };

            stack.Children.Add(new System.Windows.Shapes.Rectangle
            {
                Width = 30,
                Height = 15,
                Fill = new System.Windows.Media.SolidColorBrush(GetColorForLevel(level, levels.Min(), levels.Max())),
                Margin = new System.Windows.Thickness(0, 0, 10, 0),
                StrokeThickness = 1,
                Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black)
            });

           
            var culture = new System.Globalization.CultureInfo("ru-RU");
            string formattedValue = level.ToString("F3", culture);

           
            if (formattedValue.Contains(","))
            {
                formattedValue = formattedValue.TrimEnd('0').TrimEnd(',');
                if (formattedValue == "") formattedValue = "0";
            }

            stack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = formattedValue,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            });

            legendPanel.Children.Add(stack);
        }

        
        legendPanel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = $"Дата расчета: {DateTime.Now.ToShortDateString()}",
            Margin = new System.Windows.Thickness(0, 10, 0, 0),
            FontSize = 10,
            TextAlignment = System.Windows.TextAlignment.Center
        });

        
        var border = new System.Windows.Controls.Border
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
            BorderThickness = new System.Windows.Thickness(1),
            Child = legendPanel,
            CornerRadius = new System.Windows.CornerRadius(5),
            Padding = new System.Windows.Thickness(10),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                ShadowDepth = 3,
                Opacity = 0.3,
                BlurRadius = 5
            }
        };

        LegendGrid.Children.Add(border);

        
        System.Windows.Controls.Grid.SetColumn(border, 1);
        System.Windows.Controls.Grid.SetRow(border, 0);
    }

    private System.Windows.Media.Color GetColorForLevel(double level, double minLevel, double maxLevel)
    {
        
        double normalizedLevel = Math.Max(0, Math.Min(1, (level - minLevel) / (maxLevel - minLevel)));

        if (normalizedLevel < 0.5)
        {
           
            byte r = (byte)(255 * normalizedLevel * 2);
            byte g = 255;
            byte b = 0;
            return System.Windows.Media.Color.FromRgb(r, g, b);
        }
        else
        {
           
            byte r = 255;
            byte g = (byte)(255 * (1 - (normalizedLevel - 0.5) * 2));
            byte b = 0;
            return System.Windows.Media.Color.FromRgb(r, g, b);
        }
    }

    /*private static List<PointLatLng> FindContourPoints(List<GridPoint> grid, double targetLevel)
    {
        var contourPoints = new List<PointLatLng>();

        try
        {
            // Проверка на null
            if (grid == null || grid.Count == 0)
            {
                return contourPoints;
            }

            // Простой алгоритм - берем точки, близкие к целевому уровню
            var closePoints = grid
                .Where(p => p != null && Math.Abs(p.Concentration - targetLevel) < targetLevel * 0.5)
                .OrderBy(p => Math.Abs(p.Concentration - targetLevel))
                .Take(100)
                .ToList();

            if (closePoints.Count < 3) return contourPoints;

            // Сортируем точки по углу относительно центра
            var centerLat = closePoints.Average(p => p.Lat);
            var centerLon = closePoints.Average(p => p.Lon);

            return closePoints
                .OrderBy(p => Math.Atan2(p.Lat - centerLat, p.Lon - centerLon))
                .Select(p => new PointLatLng(p.Lat, p.Lon))
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при поиске контурных точек: {ex.Message}");
            return contourPoints;
        }
    }*/

    /*private GMapPolygon CreateContourPolygon(List<PointLatLng> points, Color color, double level)
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
    }*/

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

   /* private void ShowLegendAlternative(double[] levels)
    {
        LegendGrid.Children.Clear();

        var legendPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Background = new SolidColorBrush(Colors.White),
            Margin = new Thickness(10),
            Opacity = 0.9
        };

        // Заголовок
        legendPanel.Children.Add(new TextBlock
        {
            Text = "Концентрация (мг/м³)",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        });

        // Элементы
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

        // Добавляем границу для красоты
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Colors.Black),
            BorderThickness = new Thickness(1),
            Child = legendPanel
        };

        LegendGrid.Children.Add(border);
    }
    */
   
    
    /*private Color GetColorForLevel(double level)
    {
        double ratio = Math.Min(level / 5.0, 1.0);
        return Color.FromRgb(
            (byte)(255 * ratio),
            (byte)(255 * (1 - ratio)),
            0);
    }*/

    private void ClearConcentrationFields()
    {
        foreach (var poly in _concentrationPolygons)
        {
            MapControl.Markers.Remove(poly);
        }
        _concentrationPolygons.Clear();

        
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