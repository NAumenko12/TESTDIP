using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TESTDIP.Model;
using TESTDIP.ViewModels;

namespace TESTDIP.ViewModel
{
    public class AddLocationViewModel : INotifyPropertyChanged
    {
        public event EventHandler<bool?> RequestClose;

        private string _name;
        private string _siteNumber;
        private string _distanceFromSource;
        private string _description;
        private string _latitude = "0";
        private string _longitude = "0";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public Location Location { get; private set; }

        public AddLocationViewModel()
        {
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string SiteNumber
        {
            get => _siteNumber;
            set { _siteNumber = value; OnPropertyChanged(nameof(SiteNumber)); }
        }

        public string DistanceFromSource
        {
            get => _distanceFromSource;
            set { _distanceFromSource = value; OnPropertyChanged(nameof(DistanceFromSource)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string Latitude
        {
            get => _latitude;
            set { _latitude = value; OnPropertyChanged(nameof(Latitude)); }
        }

        public string Longitude
        {
            get => _longitude;
            set { _longitude = value; OnPropertyChanged(nameof(Longitude)); }
        }

        private void Save()
        {
            if (!double.TryParse(Latitude, out double latitude) ||
                !double.TryParse(Longitude, out double longitude))
            {
                MessageBox.Show("Введите корректные координаты", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Location = new Location
            {
                Name = Name,
                SiteNumber = SiteNumber,
                DistanceFromSource = DistanceFromSource,
                Description = Description,
                Latitude = latitude,
                Longitude = longitude
            };

            RequestClose?.Invoke(this, true);
        }

        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

   
}

