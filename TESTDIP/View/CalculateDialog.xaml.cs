using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using GMap.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TESTDIP.DataBase;
using TESTDIP.Model;
using Location = TESTDIP.Model.Location;

namespace TESTDIP.View
{
    /// <summary>
    /// Логика взаимодействия для CalculateDialog.xaml
    /// </summary>
    public partial class CalculateDialog : Window
    {
        public Location SelectedLocation { get; private set; }
        public int SelectedYear { get; private set; }
        public Metal SelectedMetal { get; private set; }

        private readonly DatabaseHelper _dbHelper;
        private readonly PointLatLng _sourcePoint;

        public CalculateDialog(DatabaseHelper dbHelper, PointLatLng sourcePoint)
        {
            InitializeComponent();

            _dbHelper = dbHelper;
            _sourcePoint = sourcePoint;

            // Отложенная инициализация после загрузки окна
            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Инициализация карты
                GMaps.Instance.Mode = AccessMode.ServerAndCache;

                SelectionMap.MapProvider = GMapProviders.GoogleMap;
                SelectionMap.MinZoom = 2;
                SelectionMap.MaxZoom = 18;
                SelectionMap.Zoom = 10;
                SelectionMap.Position = _sourcePoint;
                SelectionMap.CanDragMap = true;
                SelectionMap.MouseLeftButtonDown += SelectionMap_MouseDown;

                // Загрузка данных
                InitializeData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации карты: {ex.Message}");
            }
        }

        private void InitializeData()
        {
            try
            {
                // Загрузка локаций
                var locations = _dbHelper?.GetLocations() ?? new List<Location>();
                foreach (var loc in locations)
                {
                    // 1. Проверка валидности координат перед созданием маркера
                    if (Math.Abs(loc.Latitude) > 90 || Math.Abs(loc.Longitude) > 180)
                    {
                        Console.WriteLine($"Некорректные координаты локации {loc.Id}: {loc.Latitude}, {loc.Longitude}");
                        continue;
                    }

                    var marker = new GMapMarker(new PointLatLng(loc.Latitude, loc.Longitude))
                    {
                        Shape = new System.Windows.Shapes.Ellipse
                        {
                            Width = 10,
                            Height = 10,
                            Stroke = Brushes.Black,
                            StrokeThickness = 1.5,
                            Fill = Brushes.Blue
                        },
                        Tag = loc 
                    };
                    SelectionMap.Markers.Add(marker);
                }

                YearComboBox.ItemsSource = new List<int>();
                MetalComboBox.ItemsSource = new List<Metal>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void SelectionMap_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // 3. Используем безопасное преобразование координат
                var clickPoint = e.GetPosition(SelectionMap);
                var clickPos = SelectionMap.FromLocalToLatLng((int)clickPoint.X, (int)clickPoint.Y);

                GMapMarker nearestMarker = null;
                double minDistance = double.MaxValue;

                foreach (var marker in SelectionMap.Markers.OfType<GMapMarker>())
                {
                    // 4. Двойная проверка типа
                    if (!(marker?.Tag is Location loc) || marker.Position == null)
                    {
                        Console.WriteLine("Маркер без локации или координат");
                        continue;
                    }

                    // 5. Проверка на NaN в координатах
                    if (double.IsNaN(marker.Position.Lat))
                    {
                        Console.WriteLine($"Lat is NaN in marker {loc.Id}");
                        continue;
                    }

                    try
                    {
                        double dist = CalculateDistance(clickPos, marker.Position);
                        if (dist < minDistance && dist < 0.01)
                        {
                            minDistance = dist;
                            nearestMarker = marker;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка расчета дистанции: {ex.Message}");
                    }
                }

                if (nearestMarker != null)
                {
                    // 6. Безопасное обновление SelectedLocation
                    if (nearestMarker.Tag is Location selectedLoc)
                    {
                        SelectedLocation = selectedLoc;
                        LoadLocationSpecificData();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка: маркер не содержит данные локации");
                        return;
                    }

                    // ... остальной код ...
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\n\nStackTrace:\n{ex.StackTrace}");
            }
        }

        private double CalculateDistance(PointLatLng p1, PointLatLng p2)
        {
            if (p1 == null || p2 == null)
                throw new ArgumentNullException("Координаты не могут быть null");

            const double R = 6371; // Радиус Земли в км
            var dLat = ToRadians(p2.Lat - p1.Lat);
            var dLon = ToRadians(p2.Lng - p1.Lng);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(p1.Lat)) *
                    Math.Cos(ToRadians(p2.Lat)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
        private void LoadLocationSpecificData()
        {
            if (SelectedLocation == null || SelectedLocation.Id < 0)
            {
                MessageBox.Show("Не выбрана корректная локация");
                return;
            }

            try
            {
                // 7. Проверка существования методов в DatabaseHelper
                var years = _dbHelper?.GetYearsForLocation(SelectedLocation.Id) ?? new List<int>();
                YearComboBox.ItemsSource = years;
                YearComboBox.SelectedItem = years.FirstOrDefault();

                var metals = _dbHelper?.GetMetalsForLocation(SelectedLocation.Id) ?? new List<Metal>();
                MetalComboBox.ItemsSource = metals;
                MetalComboBox.SelectedItem = metals.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }



        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedLocation == null || YearComboBox.SelectedItem == null || MetalComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите все параметры: точку на карте, год и металл!");
                    return;
                }

                SelectedYear = (int)YearComboBox.SelectedItem;
                SelectedMetal = (Metal)MetalComboBox.SelectedItem;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

