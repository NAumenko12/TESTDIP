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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления графика: {ex.Message}");
            }
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

