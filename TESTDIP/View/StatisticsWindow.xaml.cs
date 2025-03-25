using LiveCharts.Wpf;
using LiveCharts;
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
using System.Globalization;
using System.Data.SQLite;

namespace TESTDIP.View
{
    /// <summary>
    /// Логика взаимодействия для StatisticsWindow.xaml
    /// </summary>
    public partial class StatisticsWindow : Window
    {
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
        private List<Sample> _allSamples = new List<Sample>();

        public SeriesCollection SeriesCollection { get; set; }
        public List<string> Labels { get; set; }

        public StatisticsWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += StatisticsWindow_Loaded;
        }

        private void StatisticsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _allSamples = _dbHelper.GetAllSamplesWithLocations();
                LoadYears();
                LoadMetals();
                ChartTypeComboBox.SelectedIndex = 0; // Выбираем первый тип графика по умолчанию
                UpdateChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadYears()
        {
            var years = _allSamples
                .Select(s => s.SamplingDate.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            YearComboBox.ItemsSource = years;
            if (years.Count > 0) YearComboBox.SelectedItem = years.First();
        }

        private void LoadMetals()
        {
            var metals = _dbHelper.GetMetals();
            MetalComboBox.ItemsSource = metals;
            MetalComboBox.DisplayMemberPath = "Name";
            MetalComboBox.SelectedValuePath = "Id";
            if (metals.Count > 0) MetalComboBox.SelectedIndex = 0;
        }

        private void UpdateChart()
        {
            try
            {
                if (ChartTypeComboBox.SelectedIndex == -1) return;

                switch (ChartTypeComboBox.SelectedIndex)
                {
                    case 0:
                        UpdateSingleMetalChart();
                        break;
                    case 1:
                        UpdateAllMetalsChart();
                        break;
                    case 2:
                        UpdateMetalTrendChart();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления графика: {ex.Message}");
            }
        }

        private void UpdateSingleMetalChart()
        {
            if (YearComboBox.SelectedItem == null || MetalComboBox.SelectedValue == null)
                return;

            int selectedYear = (int)YearComboBox.SelectedItem;
            int selectedMetalId = (int)MetalComboBox.SelectedValue;

            var filteredSamples = _allSamples
                .Where(s => s.SamplingDate.Year == selectedYear && s.MetalId == selectedMetalId)
                .OrderBy(s => ParseDistance(s.Location.DistanceFromSource))
                .ToList();

            SeriesCollection = new SeriesCollection();
            Labels = new List<string>();

            if (!filteredSamples.Any())
            {
                MessageBox.Show("Нет данных для выбранных фильтров");
                return;
            }

            var values = new ChartValues<double>();
            foreach (var sample in filteredSamples)
            {
                if (double.TryParse(sample.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double concentration))
                {
                    values.Add(concentration);
                    Labels.Add($"{ParseDistance(sample.Location.DistanceFromSource):F1} км");
                }
            }

            var metal = (MetalComboBox.SelectedItem as Metal)?.Name ?? "Металл";

            SeriesCollection.Add(new LineSeries
            {
                Title = $"{metal} ({selectedYear} г.)",
                Values = values,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 10,
                Stroke = Brushes.Blue,
                Fill = Brushes.Transparent
            });

            Chart.Series = SeriesCollection;
            XAxis.Labels = Labels;
            XAxis.Title = "Расстояние от источника (км)";
        }

        private void UpdateAllMetalsChart()
        {
            if (YearComboBox.SelectedItem == null)
                return;

            int selectedYear = (int)YearComboBox.SelectedItem;
            var metals = MetalComboBox.ItemsSource as List<Metal>;

            SeriesCollection = new SeriesCollection();
            Labels = new List<string>();

            
            var distances = _allSamples
                .Where(s => s.SamplingDate.Year == selectedYear)
                .Select(s => ParseDistance(s.Location.DistanceFromSource))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!distances.Any())
            {
                MessageBox.Show("Нет данных для выбранного года");
                return;
            }

            Labels = distances.Select(d => $"{d:F1} км").ToList();

            foreach (var metal in metals)
            {
                var values = new ChartValues<double>();

                foreach (var distance in distances)
                {
                    var sample = _allSamples
                        .FirstOrDefault(s => s.SamplingDate.Year == selectedYear &&
                                           s.MetalId == metal.Id &&
                                           Math.Abs(ParseDistance(s.Location.DistanceFromSource) - distance) < 0.1);

                    if (sample != null && double.TryParse(sample.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double concentration))
                    {
                        values.Add(concentration);
                    }
                    else
                    {
                        values.Add(0);
                    }
                }

                SeriesCollection.Add(new LineSeries
                {
                    Title = $"{metal.Name}",
                    Values = values,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10
                });
            }

            Chart.Series = SeriesCollection;
            XAxis.Labels = Labels;
            XAxis.Title = "Расстояние от источника (км)";
        }

        private void UpdateMetalTrendChart()
        {
            if (MetalComboBox.SelectedValue == null)
                return;

            int selectedMetalId = (int)MetalComboBox.SelectedValue;
            var years = YearComboBox.ItemsSource as List<int>;

            SeriesCollection = new SeriesCollection();
            Labels = new List<string>();

            
            var distances = _allSamples
                .Where(s => s.MetalId == selectedMetalId)
                .Select(s => ParseDistance(s.Location.DistanceFromSource))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!distances.Any())
            {
                MessageBox.Show("Нет данных для выбранного металла");
                return;
            }

            
            Labels = distances.Select(d => $"{d:F1} км").ToList();

            
            foreach (var year in years)
            {
                var values = new ChartValues<double>();

                foreach (var distance in distances)
                {
                    var sample = _allSamples
                        .FirstOrDefault(s => s.SamplingDate.Year == year &&
                                           s.MetalId == selectedMetalId &&
                                           Math.Abs(ParseDistance(s.Location.DistanceFromSource) - distance) < 0.1);

                    if (sample != null && double.TryParse(sample.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double concentration))
                    {
                        values.Add(concentration);
                    }
                    else
                    {
                        values.Add(0);
                    }
                }

                SeriesCollection.Add(new LineSeries
                {
                    Title = $"{year} г.",
                    Values = values,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10
                });
            }

            var metal = (MetalComboBox.SelectedItem as Metal)?.Name ?? "Металл";
            Chart.Series = SeriesCollection;
            XAxis.Labels = Labels;
            XAxis.Title = $"Динамика {metal} по годам (расстояние от источника, км)";
        }

        private void ChartTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FiltersPanel == null) return;

            
            switch (ChartTypeComboBox.SelectedIndex)
            {
                case 0: 
                    YearComboBox.Visibility = Visibility.Visible;
                    MetalComboBox.Visibility = Visibility.Visible;
                    break;
                case 1: 
                    YearComboBox.Visibility = Visibility.Visible;
                    MetalComboBox.Visibility = Visibility.Collapsed;
                    break;
                case 2: 
                    YearComboBox.Visibility = Visibility.Collapsed;
                    MetalComboBox.Visibility = Visibility.Visible;
                    break;
            }

            UpdateChart();
        }

        private double ParseDistance(string distanceStr)
        {
            if (string.IsNullOrWhiteSpace(distanceStr))
                return 0;

            distanceStr = distanceStr
                .Replace("km", "", StringComparison.OrdinalIgnoreCase)
                .Replace("км", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (double.TryParse(distanceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double distance))
            {
                return distance;
            }
            return 0;
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateChart();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _allSamples = _dbHelper.GetAllSamplesWithLocations();
                LoadYears();
                LoadMetals();
                UpdateChart();
                MessageBox.Show("Данные успешно обновлены");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления данных: {ex.Message}");
            }
        }
    }
}

