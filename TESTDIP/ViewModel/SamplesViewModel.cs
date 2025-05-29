using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;
using TESTDIP.DataBase;
using TESTDIP.Model;
using TESTDIP.View;
using TESTDIP.ViewModels;

namespace TESTDIP.ViewModel
{
    public class SamplesViewModel : INotifyPropertyChanged
    {
        private readonly Location _location;
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
        private ICollectionView _filteredSamples;
        private Sample _selectedSample;
        private Metal _selectedMetalFilter;
        private object _selectedYearFilter;

        public ObservableCollection<Sample> Samples { get; }
        public List<Metal> MetalsFilter { get; private set; }
        public List<object> YearsFilter { get; private set; }

        public ICommand AddSampleCommand { get; set; }
        public ICommand EditSampleCommand { get; set; }
        public ICommand DeleteSampleCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand FilterChangedCommand { get; set; }

        public SamplesViewModel(Location location, IEnumerable<Sample> samples)
        {
            _location = location;
            Samples = new ObservableCollection<Sample>(samples);
            _filteredSamples = CollectionViewSource.GetDefaultView(Samples);

            InitializeCommands();
            LoadFilters();
        }

        public string LocationName => _location.Name;
        public string LocationSiteNumber => _location.SiteNumber;
        public ICollectionView FilteredSamples => _filteredSamples;

        public Sample SelectedSample
        {
            get => _selectedSample;
            set
            {
                _selectedSample = value;
                OnPropertyChanged(nameof(SelectedSample));
            }
        }

        public Metal SelectedMetalFilter
        {
            get => _selectedMetalFilter;
            set
            {
                _selectedMetalFilter = value;
                OnPropertyChanged(nameof(SelectedMetalFilter));
                ApplyFilters();
            }
        }

        public object SelectedYearFilter
        {
            get => _selectedYearFilter;
            set
            {
                _selectedYearFilter = value;
                OnPropertyChanged(nameof(SelectedYearFilter));
                ApplyFilters();
            }
        }

        private void InitializeCommands()
        {
            AddSampleCommand = new RelayCommand(_ => AddSample());
            EditSampleCommand = new RelayCommand(_ => EditSample(), _ => SelectedSample != null);
            DeleteSampleCommand = new RelayCommand(_ => DeleteSample(), _ => SelectedSample != null);
            CloseCommand = new RelayCommand(_ => Close());
            FilterChangedCommand = new RelayCommand(_ => ApplyFilters());
        }

        private void LoadFilters()
        {
            // Загрузка металлов для фильтра
            var metals = Samples
                .GroupBy(s => s.Metal.Id)
                .Select(g => g.First().Metal)
                .OrderBy(m => m.Name)
                .ToList();

            MetalsFilter = new List<Metal> { new Metal { Id = -1, Name = "Все металлы" } };
            MetalsFilter.AddRange(metals);
            OnPropertyChanged(nameof(MetalsFilter));

            // Загрузка годов для фильтра
            var years = Samples
                .Select(s => s.SamplingDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            YearsFilter = new List<object> { "Все годы" };
            YearsFilter.AddRange(years.Cast<object>());
            OnPropertyChanged(nameof(YearsFilter));

            // Установка начальных значений фильтров
            SelectedMetalFilter = MetalsFilter.First();
            SelectedYearFilter = YearsFilter.First();
        }

        private void ApplyFilters()
        {
            if (_filteredSamples == null) return;

            _filteredSamples.Filter = item =>
            {
                if (!(item is Sample sample)) return false;

                // Фильтр по металлу
                bool metalFilter = SelectedMetalFilter?.Id == -1 ||
                                 sample.Metal.Id == SelectedMetalFilter?.Id;

                // Фильтр по году
                bool yearFilter = SelectedYearFilter?.ToString() == "Все годы" ||
                                (SelectedYearFilter is int year && sample.SamplingDate.Year == year);

                return metalFilter && yearFilter;
            };
        }

        private void AddSample()
        {
            var addSampleWindow = new AddSampleWindow(_location.Id, $"{_location.Name} (пл. {_location.SiteNumber})");
            if (addSampleWindow.ShowDialog() == true && addSampleWindow.NewSampleId > 0)
            {
                var newSample = _dbHelper.GetSampleById(addSampleWindow.NewSampleId);
                if (newSample != null)
                {
                    Samples.Add(newSample);
                    LoadFilters();
                }
            }
        }

        private void EditSample()
        {
            if (SelectedSample == null) return;

            var editWindow = new EditSampleWindow(SelectedSample);
            if (editWindow.ShowDialog() == true)
            {
                bool success = _dbHelper.UpdateSample(editWindow.EditedSample);
                if (success)
                {
                    int index = Samples.IndexOf(SelectedSample);
                    Samples[index] = editWindow.EditedSample;
                    LoadFilters();
                }
                else
                {
                    ShowError("Не удалось обновить пробу в базе данных");
                }
            }
        }

        private void DeleteSample()
        {
            if (SelectedSample == null) return;

            var result = MessageBox.Show(
                $"Удалить пробу {SelectedSample.Metal.Name} от {SelectedSample.SamplingDate:dd.MM.yyyy}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                bool success = _dbHelper.DeleteSample(SelectedSample.Id);
                if (success)
                {
                    Samples.Remove(SelectedSample);
                    LoadFilters();
                }
                else
                {
                    ShowError("Не удалось удалить пробу");
                }
            }
        }

        private void Close()
        {
            Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this)?.Close();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
