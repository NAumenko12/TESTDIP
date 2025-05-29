using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using GMap.NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows;
using TESTDIP.DataBase;
using TESTDIP.Model;
using TESTDIP.ViewModels;
using Location = TESTDIP.Model.Location;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace TESTDIP.ViewModel
{
    public class CalculateDialogViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly PointLatLng _sourcePoint;
        private Location _selectedLocation;
        private int _selectedYear;
        private Metal _selectedMetal;

        public event EventHandler<bool?> RequestClose;

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand MapClickCommand { get; }

        public GMapControl MapControl { get;  set; }
        public List<int> Years { get; private set; }
        public List<Metal> Metals { get; private set; }

        private ObservableCollection<GMapMarker> _markers;
        public ObservableCollection<GMapMarker> Markers
        {
            get => _markers;
            set
            {
                _markers = value;
                OnPropertyChanged();
            }
        }

        public CalculateDialogViewModel(DatabaseHelper dbHelper, PointLatLng sourcePoint)
        {
            _dbHelper = dbHelper;
            _sourcePoint = sourcePoint;

            OkCommand = new RelayCommand(_ => Ok());
            CancelCommand = new RelayCommand(_ => Cancel());
            MapClickCommand = new RelayCommand(p =>
            {
                if (p is Point pt)
                    OnMapClick(pt);
            });
            MapControl = new GMapControl
            {
                MapProvider = GMapProviders.GoogleMap,
                MinZoom = 2,
                MaxZoom = 18,
                Zoom = 10,
                Position = sourcePoint,
                CanDragMap = true
            };

            Markers = new ObservableCollection<GMapMarker>();
            LoadLocations();
        }

        public Location SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                _selectedLocation = value;
                OnPropertyChanged();
                LoadLocationData();
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged();
            }
        }

        public Metal SelectedMetal
        {
            get => _selectedMetal;
            set
            {
                _selectedMetal = value;
                OnPropertyChanged();
            }
        }
        private void LoadLocations()
        {
            try
            {
                Markers.Clear();
                var locations = _dbHelper?.GetLocations() ?? new List<Location>();

                foreach (var loc in locations.Where(l => IsValidCoordinate(l.Latitude, l.Longitude)))
                {
                    var marker = new GMapMarker(new PointLatLng(loc.Latitude, loc.Longitude))
                    {
                        Shape = new Ellipse
                        {
                            Width = 10,
                            Height = 10,
                            Stroke = Brushes.Black,
                            StrokeThickness = 1.5,
                            Fill = Brushes.Blue
                        },
                        Tag = loc
                    };
                    Markers.Add(marker);
                }

                // Обновляем маркеры на карте
                UpdateMapMarkers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки локаций: {ex.Message}");
            }
        }
        private void UpdateMapMarkers()
        {
            MapControl.Markers.Clear();
            foreach (var marker in Markers)
            {
                MapControl.Markers.Add(marker);
            }
        }

        public void OnMapClick(System.Windows.Point clickPoint)
        {
            if (clickPoint == null) return;

            var clickPos = MapControl.FromLocalToLatLng((int)clickPoint.X, (int)clickPoint.Y);
            var nearestMarker = FindNearestMarker(clickPos);

            if (nearestMarker?.Tag is Location location)
            {
                SelectedLocation = location;
            }
        }

        private GMapMarker FindNearestMarker(PointLatLng clickPos)
        {
            GMapMarker nearestMarker = null;
            double minDistance = double.MaxValue;

            foreach (var marker in MapControl.Markers.OfType<GMapMarker>())
            {
                if (!(marker?.Tag is Location) || !IsValidCoordinate(marker.Position.Lat, marker.Position.Lng))
                    continue;

                try
                {
                    double dist = CalculateDistance(clickPos, marker.Position);
                    if (dist < minDistance && dist < 0.01) // 0.01 градуса ~ 1.1 км
                    {
                        minDistance = dist;
                        nearestMarker = marker;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return nearestMarker;
        }

        private void LoadLocationData()
        {
            if (SelectedLocation == null) return;

            try
            {
                Years = _dbHelper?.GetYearsForLocation(SelectedLocation.Id) ?? new List<int>();
                Metals = _dbHelper?.GetMetalsForLocation(SelectedLocation.Id) ?? new List<Metal>();

                OnPropertyChanged(nameof(Years));
                OnPropertyChanged(nameof(Metals));

                SelectedYear = Years.FirstOrDefault();
                SelectedMetal = Metals.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void Ok()
        {
            if (SelectedLocation == null || SelectedYear == 0 || SelectedMetal == null)
            {
                MessageBox.Show("Пожалуйста, выберите все параметры: точку на карте, год и металл!");
                return;
            }

            RequestClose?.Invoke(this, true);
        }

        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }

        private bool IsValidCoordinate(double lat, double lng)
        {
            return Math.Abs(lat) <= 90 && Math.Abs(lng) <= 180;
        }

        private double CalculateDistance(PointLatLng p1, PointLatLng p2)
        {
            const double R = 6371; // Earth radius in km
            var dLat = ToRadians(p2.Lat - p1.Lat);
            var dLon = ToRadians(p2.Lng - p1.Lng);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(p1.Lat)) *
                    Math.Cos(ToRadians(p2.Lat)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

