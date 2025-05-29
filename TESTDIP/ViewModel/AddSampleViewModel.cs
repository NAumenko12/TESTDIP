using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TESTDIP.DataBase;
using TESTDIP.Model;

namespace TESTDIP.ViewModels
{
    public class AddSampleViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
        private readonly int _locationId;
        private string _value;
        private string _type;
        private string _fraction;
        private string _repetition;
        private DateTime _samplingDate = DateTime.Today;
        private string _analyticsNumber;
        private Metal _selectedMetal;
        private IEnumerable<Metal> _metals;

        public event EventHandler<bool?> RequestClose;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddSampleViewModel(int locationId, string locationName)
        {
            _locationId = locationId;
            LocationName = locationName;

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            LoadMetals();
        }

        public string LocationName { get; }
        public Sample NewSample { get; private set; }
        public int NewSampleId { get; private set; }

        public IEnumerable<Metal> Metals
        {
            get => _metals;
            set
            {
                if (_metals != value)
                {
                    _metals = value;
                    OnPropertyChanged();
                }
            }
        }

        public Metal SelectedMetal
        {
            get => _selectedMetal;
            set
            {
                if (_selectedMetal != value)
                {
                    _selectedMetal = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Fraction
        {
            get => _fraction;
            set
            {
                if (_fraction != value)
                {
                    _fraction = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Repetition
        {
            get => _repetition;
            set
            {
                if (_repetition != value)
                {
                    _repetition = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime SamplingDate
        {
            get => _samplingDate;
            set
            {
                if (_samplingDate != value)
                {
                    _samplingDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AnalyticsNumber
        {
            get => _analyticsNumber;
            set
            {
                if (_analyticsNumber != value)
                {
                    _analyticsNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        private async void LoadMetals()
        {
            try
            {
                Metals = await Task.Run(() => _dbHelper.GetMetals());
                if (Metals.Any())
                {
                    SelectedMetal = Metals.First();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка металлов: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save()
        {
            if (SelectedMetal == null)
            {
                MessageBox.Show("Выберите металл из списка", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Value))
            {
                MessageBox.Show("Введите значение пробы", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(AnalyticsNumber))
            {
                MessageBox.Show("Введите номер аналитики", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewSample = new Sample
            {
                LocationId = _locationId,
                MetalId = SelectedMetal.Id,
                Type = Type,
                Fraction = Fraction,
                Repetition = int.TryParse(Repetition, out int rep) ? rep : (int?)null,
                Value = Value,
                SamplingDate = SamplingDate,
                AnalyticsNumber = AnalyticsNumber,
                Metal = SelectedMetal
            };

            try
            {
                NewSampleId = _dbHelper.AddSample(NewSample);
                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения пробы: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}