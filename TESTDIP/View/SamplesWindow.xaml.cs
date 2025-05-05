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
using TESTDIP.View;
using TESTDIP.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TESTDIP
{
    /// <summary>
    /// Логика взаимодействия для SamplesWindow.xaml
    /// </summary>
    public partial class SamplesWindow : Window
    {
        private readonly Location _location;
        private readonly ObservableCollection<Sample> _samples;
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
        private ICollectionView _filteredSamples;

        public string LocationName => _location.Name;
        public string LocationSiteNumber => _location.SiteNumber;
        public ObservableCollection<Sample> Samples => _samples;
        public ICollectionView FilteredSamples => _filteredSamples;

        public SamplesWindow(Location location, IEnumerable<Sample> samples)
        {
            InitializeComponent();
            _location = location;
            _samples = new ObservableCollection<Sample>(samples);
            _filteredSamples = CollectionViewSource.GetDefaultView(_samples);

            DataContext = this;
            LoadFilters();
        }

        private void LoadFilters()
        {
            // Загрузка металлов для фильтра (с правильным Distinct)
            var metals = _samples
                .GroupBy(s => s.Metal.Id)  // Группируем по Id металла
                .Select(g => g.First().Metal) // Берем первый металл из каждой группы
                .OrderBy(m => m.Name)
                .ToList();

            // Создаем новую коллекцию с "Все металлы" в начале
            var allMetals = new List<Metal> { new Metal { Id = -1, Name = "Все металлы" } };
            allMetals.AddRange(metals);

            MetalFilterComboBox.ItemsSource = allMetals;
            MetalFilterComboBox.SelectedIndex = 0;

            // Загрузка годов для фильтра (оставляем как было)
            var years = _samples
                .Select(s => s.SamplingDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            var allYears = new List<object> { "Все годы" };
            allYears.AddRange(years.Cast<object>());

            YearFilterComboBox.ItemsSource = allYears;
            YearFilterComboBox.SelectedIndex = 0;
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_filteredSamples == null) return;

            _filteredSamples.Filter = item =>
            {
                var sample = item as Sample;
                if (sample == null) return false;

                // Фильтр по металлу
                bool metalFilter = true;
                if (MetalFilterComboBox.SelectedValue != null &&
                    MetalFilterComboBox.SelectedValue is int metalId &&
                    metalId != -1)
                {
                    metalFilter = sample.Metal.Id == metalId;
                }

                // Фильтр по году
                bool yearFilter = true;
                if (YearFilterComboBox.SelectedItem != null &&
                    YearFilterComboBox.SelectedItem is int year)
                {
                    yearFilter = sample.SamplingDate.Year == year;
                }

                return metalFilter && yearFilter;
            };
        }
        private void EditSample_Click(object sender, RoutedEventArgs e)
        {
            if (SamplesDataGrid.SelectedItem is Sample selectedSample)
            {
                var editWindow = new EditSampleWindow(selectedSample);
                if (editWindow.ShowDialog() == true)
                {
                    // Обновляем пробу в базе данных
                    bool success = _dbHelper.UpdateSample(editWindow.EditedSample);

                    if (success)
                    {
                        // Обновляем пробу в коллекции
                        int index = _samples.IndexOf(selectedSample);
                        _samples[index] = editWindow.EditedSample;

                        // Обновляем фильтры (если изменился металл или год)
                        LoadFilters();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось обновить пробу в базе данных", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите пробу для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteSample_Click(object sender, RoutedEventArgs e)
        {
            if (SamplesDataGrid.SelectedItem is Sample selectedSample)
            {
                var result = MessageBox.Show(
                    $"Удалить пробу {selectedSample.Metal.Name} от {selectedSample.SamplingDate:dd.MM.yyyy}?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    bool success = _dbHelper.DeleteSample(selectedSample.Id);

                    if (success)
                    {
                        _samples.Remove(selectedSample);
                        LoadFilters(); // Обновляем фильтры после удаления
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить пробу", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите пробу для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void AddSample_Click(object sender, RoutedEventArgs e)
        {
            var addSampleWindow = new AddSampleWindow(_location.Id, $"{_location.Name} (пл. {_location.SiteNumber})");
            if (addSampleWindow.ShowDialog() == true && addSampleWindow.NewSampleId > 0)
            {
                var newSample = _dbHelper.GetSampleById(addSampleWindow.NewSampleId);
                if (newSample != null)
                {
                    _samples.Add(newSample);
                    LoadFilters(); // Обновляем фильтры после добавления новой пробы
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
