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

    public MainWindow()
    {
        InitializeComponent();
        InitializeMap();
        LoadLocations();
        LoadMetalsComboBox();
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

    private void LoadMetalsComboBox()
    {
        MetalComboBox.Items.Clear();
        MetalComboBox.Items.Add("Выбор металла");

        foreach (var metal in _dbHelper.GetMetals())
        {
            MetalComboBox.Items.Add(new ComboBoxItem
            {
                Content = metal.Name,
                Tag = metal.Id
            });
        }

        MetalComboBox.SelectedIndex = 0;
    }

    private void UpdateMap(List<Location> locations)
    {
        var plantMarker = MapControl.Markers.FirstOrDefault(m => m == _plantMarker);
        var markersToKeep = MapControl.Markers.OfType<GMapPolygon>().ToList();

        MapControl.Markers.Clear();

        // Восстанавливаем полигоны и маркер завода
        foreach (var polygon in markersToKeep)
        {
            MapControl.Markers.Add(polygon);
        }
        if (plantMarker != null)
        {
            MapControl.Markers.Add(plantMarker);
        }

        // Добавляем маркеры локаций (без изменений во внешнем виде)
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

            // Добавляем ToolTip с названием
            ToolTipService.SetToolTip(marker.Shape, location.SiteNumber);

            MapControl.Markers.Add(marker);
        }

        MapControl.ZoomAndCenterMarkers(null);
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
                // Проверяем, что все необходимые данные выбраны
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


    private void CalculateAndDrawPollutionField(Location referencePoint, Metal metal, int year)
    {
        try
        {
            // Проверяем входные параметры
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

            // Очищаем предыдущие полигоны
            ClearConcentrationFields();

            // Создаем экземпляр калькулятора
            var calculator = new ConcentrationCalculator(_dbHelper);

            // Получаем сетку с концентрациями
            var grid = calculator.CalculateField(
                ReferencePoint,
                referencePoint,
                metal,
                year,
                gridStepKm: 0.7,
                areaSizeKm: 200.0);

            // Проверка на null перед проверкой Count
            if (grid == null || grid.Count == 0)
            {
                MessageBox.Show("Нет данных для построения поля. Проверьте:\n" +
                              "1. Наличие данных для выбранного года и металла\n" +
                              "2. Корректность опорной точки");
                return;
            }

            // Уровни концентрации для изолиний
            var levels = new[] { 0.1, 0.5, 1.0, 2.0, 5.0 };

            // Создаем полигоны для каждого уровня
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

            // Центрируем карту на области загрязнения
            ZoomToPollutionArea(grid);

            // Показываем легенду
            ShowLegendAlternative(levels);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при построении поля: {ex.Message}\n\nСтек вызовов: {ex.StackTrace}");
        }
    }


    private static List<PointLatLng> FindContourPoints(List<GridPoint> grid, double targetLevel)
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

    private Color GetColorForLevel(double level)
    {
        double ratio = Math.Min(level / 5.0, 1.0);
        return Color.FromRgb(
            (byte)(255 * ratio),
            (byte)(255 * (1 - ratio)),
            0);
    }

    private void ClearConcentrationFields()
    {
        foreach (var poly in _concentrationPolygons)
        {
            MapControl.Markers.Remove(poly);
        }
        _concentrationPolygons.Clear();

        // Очищаем легенду
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
            // Очищаем полигоны концентрации
            ClearConcentrationFields();

            // Сбрасываем легенду
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
                // Создаем RenderTargetBitmap для рендеринга карты
                var renderBitmap = new RenderTargetBitmap(
                    (int)MapControl.ActualWidth,
                    (int)MapControl.ActualHeight,
                    96d, 96d, PixelFormats.Pbgra32);

                renderBitmap.Render(MapControl);

                // Выбираем кодировщик в зависимости от выбранного формата
                BitmapEncoder encoder;
                if (saveFileDialog.FilterIndex == 1)
                    encoder = new PngBitmapEncoder();
                else
                    encoder = new JpegBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                // Сохраняем файл
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