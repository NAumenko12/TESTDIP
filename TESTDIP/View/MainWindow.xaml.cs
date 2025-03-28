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
            double distance = CalculateDistance(ReferencePoint, new PointLatLng(location.Latitude, location.Longitude));
            Brush markerColor = distance <= 10 ? Brushes.Red :
                              distance <= 40 ? Brushes.Orange :
                              Brushes.Green;

            var marker = new GMapMarker(new PointLatLng(location.Latitude, location.Longitude))
            {
                Shape = new System.Windows.Shapes.Ellipse
                {
                    Fill = markerColor,
                    Width = 10,
                    Height = 10,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                },
                Tag = location
            };

            MapControl.Markers.Add(marker);
        }
        MapControl.ZoomAndCenterMarkers(null);
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
    private void ShowPollutionFieldCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (ShowPollutionFieldCheckBox.IsChecked == true)
        {
            DrawPollutionFields();
        }
        else
        {
            ClearPollutionFields();
        }
    }

    private void DrawPollutionFields()
    {
        ClearPollutionFields();
        var redZone = CreatePollutionZone(ReferencePoint, 10, Color.FromArgb(80, 255, 0, 0));     // Красная зона (0-10 км)
        var orangeZone = CreatePollutionZone(ReferencePoint, 30, Color.FromArgb(80, 255, 165, 0)); // Оранжевая зона (10-30 км)
        var greenZone = CreatePollutionZone(ReferencePoint, 50, Color.FromArgb(80, 0, 255, 0));    // Зеленая зона (30-50 км)

        
        MapControl.Markers.Add(redZone);
        MapControl.Markers.Add(orangeZone);
        MapControl.Markers.Add(greenZone);

        _pollutionPolygons.Add(redZone);
        _pollutionPolygons.Add(orangeZone);
        _pollutionPolygons.Add(greenZone);

        MapControl.Position = ReferencePoint;
        MapControl.Zoom = 9;
    }

    private GMapPolygon CreatePollutionZone(PointLatLng centerPoint, double radiusKm, Color zoneColor)
    {
        var points = new List<PointLatLng>();
        int segments = 72; 

        for (int i = 0; i < segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            double lat = centerPoint.Lat + (radiusKm / 111.32) * Math.Cos(angle);
            double lng = centerPoint.Lng + (radiusKm / (111.32 * Math.Cos(centerPoint.Lat * Math.PI / 180))) * Math.Sin(angle);
            points.Add(new PointLatLng(lat, lng));
        }
        points.Add(points[0]); 
        var polygon = new GMapPolygon(points)
        {
            Shape = new System.Windows.Shapes.Path
            {
                Data = CreatePathGeometry(points),
                Fill = new SolidColorBrush(zoneColor),
                Stroke = new SolidColorBrush(Color.FromArgb(150, zoneColor.R, zoneColor.G, zoneColor.B)),
                StrokeThickness = 1,
                Opacity = 0.5
            }
        };
        return polygon;
    }

    private System.Windows.Media.Geometry CreatePathGeometry(List<PointLatLng> points)
    {
        var geometry = new PathGeometry();
        var figure = new PathFigure();
        if (points.Count == 0) return geometry;
        var firstPoint = MapControl.FromLatLngToLocal(points[0]);
        figure.StartPoint = new System.Windows.Point(firstPoint.X, firstPoint.Y);
        for (int i = 1; i < points.Count; i++)
        {
            var gPoint = MapControl.FromLatLngToLocal(points[i]);
            figure.Segments.Add(new LineSegment(new System.Windows.Point(gPoint.X, gPoint.Y), true));
        }
        geometry.Figures.Add(figure);
        return geometry;
    }


    private void GoToStatsButton_Click(object sender, RoutedEventArgs e)
    {
        var statsWindow = new StatisticsWindow();

        statsWindow.Show();
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
    private void ClearPollutionFields()
    {
        foreach (var polygon in _pollutionPolygons)
        {
            MapControl.Markers.Remove(polygon);
        }
        _pollutionPolygons.Clear();
    }
}

