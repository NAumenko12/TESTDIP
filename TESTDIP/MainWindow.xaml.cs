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

namespace TESTDIP;


public partial class MainWindow : Window
{
    private List<MapPoint> points = new List<MapPoint>();
    private readonly PointLatLng ReferencePoint = new PointLatLng(67.911564, 32.838848);

    public MainWindow()
    {
        InitializeComponent();
        InitializeMap();
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

    private void AddPointButton_Click(object sender, RoutedEventArgs e)
    {
        AppPointWindow addPointWindow = new AppPointWindow();
        if (addPointWindow.ShowDialog() == true)
        {
            MapPoint newPoint = new MapPoint
            {
                Name = addPointWindow.PointName,
                Latitude = addPointWindow.Latitude,
                Longitude = addPointWindow.Longitude
            };
            points.Add(newPoint);
            UpdateMap();
        }
    }

    private void UpdateMap()
    {
        MapControl.Markers.Clear();
        foreach (var point in points)
        {
            double distance = CalculateDistance(ReferencePoint, new PointLatLng(point.Latitude, point.Longitude));
            Brush markerColor;
            if (distance <= 3)
            {
                markerColor = Brushes.Red; 
            }
            else if (distance <= 6)
            {
                markerColor = Brushes.Orange; 
            }
            else
            {
                markerColor = Brushes.Green; 
            }
            GMapMarker marker = new GMapMarker(new PointLatLng(point.Latitude, point.Longitude))
            {
                Shape = new System.Windows.Shapes.Ellipse
                {
                    Fill = markerColor,
                    Width = 10,
                    Height = 10,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                },
                Tag = point 
            };

            MapControl.Markers.Add(marker);
        }

        MapControl.ZoomAndCenterMarkers(null);
    }

    private void MapControl_MouseDown(object sender, MouseButtonEventArgs e)
    {
        System.Windows.Point clickPoint = e.GetPosition(MapControl);
        GPoint gClickPoint = new GPoint((int)clickPoint.X, (int)clickPoint.Y);
        PointLatLng clickPosition = MapControl.FromLocalToLatLng((int)gClickPoint.X, (int)gClickPoint.Y);
        foreach (var marker in MapControl.Markers)
        {
            GPoint gMarkerPoint = MapControl.FromLatLngToLocal(marker.Position);
            if (Math.Abs(gMarkerPoint.X - gClickPoint.X) < 10 && Math.Abs(gMarkerPoint.Y - gClickPoint.Y) < 10)
            {
                if (marker.Tag is MapPoint point)
                {
                    PointDetailsWindow detailsWindow = new PointDetailsWindow(point);
                    detailsWindow.ShowDialog();
                    break;
                }
            }
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
}
