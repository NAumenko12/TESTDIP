using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TESTDIP.DataBase;
using TESTDIP.Model;
using ClosedXML.Excel;
using TESTDIP.ViewModels;
using System.Windows.Input;
using System.Windows.Controls;

namespace TESTDIP.ViewModel
{
    public class StatisticsViewModel : BaseViewModel
    {
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
        private List<Sample> _allSamples = new List<Sample>();
        private readonly StringToDoubleConverter _valueConverter = new StringToDoubleConverter();
        public ICommand RefreshCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        

        private SeriesCollection _seriesCollection;
        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection;
            set { _seriesCollection = value; OnPropertyChanged(); }
        }

        private List<string> _labels;
        public List<string> Labels
        {
            get => _labels;
            set { _labels = value; OnPropertyChanged(); }
        }

        public Func<double, string> YAxisFormatter { get; set; }

        private List<int> _availableYears;
        public List<int> AvailableYears
        {
            get => _availableYears;
            set { _availableYears = value; OnPropertyChanged(); }
        }

        private List<Metal> _availableMetals;
        public List<Metal> AvailableMetals
        {
            get => _availableMetals;
            set { _availableMetals = value; OnPropertyChanged(); }
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); UpdateChart(); }
        }

        private Metal _selectedMetal;
        public Metal SelectedMetal
        {
            get => _selectedMetal;
            set { _selectedMetal = value; OnPropertyChanged(); UpdateChart(); }
        }

        private int _selectedChartType;
        public int SelectedChartType
        {
            get => _selectedChartType;
            set { _selectedChartType = value; OnPropertyChanged(); UpdateChartVisibility(); UpdateChart(); }
        }

        public Visibility YearVisibility { get; set; } = Visibility.Visible;
        public Visibility MetalVisibility { get; set; } = Visibility.Visible;

        public StatisticsViewModel()
        {
            YAxisFormatter = value => value.ToString("F3");

            // Инициализация команд
            RefreshCommand = new RelayCommand(_ => RefreshData());
            ExportToExcelCommand = new RelayCommand(_ => ExportToExcel());
           

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _allSamples = _dbHelper.GetAllSamplesWithLocations();
                AvailableYears = _allSamples
                    .Select(s => s.SamplingDate.Year)
                    .Distinct()
                    .OrderBy(y => y)
                    .ToList();

                AvailableMetals = _dbHelper.GetMetals();

                if (AvailableYears.Count > 0) SelectedYear = AvailableYears.First();
                if (AvailableMetals.Count > 0) SelectedMetal = AvailableMetals.First();
                SelectedChartType = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void UpdateChartVisibility()
        {
            switch (SelectedChartType)
            {
                case 0:
                    YearVisibility = Visibility.Visible;
                    MetalVisibility = Visibility.Visible;
                    break;
                case 1:
                    YearVisibility = Visibility.Visible;
                    MetalVisibility = Visibility.Collapsed;
                    break;
                case 2:
                    YearVisibility = Visibility.Collapsed;
                    MetalVisibility = Visibility.Visible;
                    break;
            }
            OnPropertyChanged(nameof(YearVisibility));
            OnPropertyChanged(nameof(MetalVisibility));
        }

        public void UpdateChart()
        {
            try
            {
                switch (SelectedChartType)
                {
                    case 0: UpdateSingleMetalChart(); break;
                    case 1: UpdateAllMetalsChart(); break;
                    case 2: UpdateMetalTrendChart(); break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления графика: {ex.Message}");
            }
        }

        private void UpdateSingleMetalChart()
        {
            if (SelectedMetal == null) return;

            var filteredSamples = _allSamples
                .Where(s => s.SamplingDate.Year == SelectedYear && s.MetalId == SelectedMetal.Id)
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
                double concentration = (double)_valueConverter.ConvertBack(sample.Value, typeof(double), null, CultureInfo.InvariantCulture);
                values.Add(concentration);
                Labels.Add($"{ParseDistance(sample.Location.DistanceFromSource):F1} км");
            }

            SeriesCollection.Add(new LineSeries
            {
                Title = $"{SelectedMetal.Name} ({SelectedYear} г.)",
                Values = values,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 10,
                Stroke = Brushes.Blue,
                Fill = Brushes.Transparent
            });
        }

        private void UpdateAllMetalsChart()
        {
            SeriesCollection = new SeriesCollection();
            Labels = new List<string>();

            var distances = _allSamples
                .Where(s => s.SamplingDate.Year == SelectedYear)
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

            foreach (var metal in AvailableMetals)
            {
                var values = new ChartValues<double>();
                bool hasSignificantValues = false;

                foreach (var distance in distances)
                {
                    var sample = _allSamples
                        .FirstOrDefault(s => s.SamplingDate.Year == SelectedYear &&
                                           s.MetalId == metal.Id &&
                                           Math.Abs(ParseDistance(s.Location.DistanceFromSource) - distance) < 0.1);

                    double value = sample != null ?
                        (double)_valueConverter.ConvertBack(sample.Value, typeof(double), null, CultureInfo.InvariantCulture) :
                        0;
                    values.Add(value);
                    if (value >= 0.001) hasSignificantValues = true;
                }

                if (hasSignificantValues)
                {
                    SeriesCollection.Add(new LineSeries
                    {
                        Title = metal.Name,
                        Values = values,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 10
                    });
                }
            }
        }

        private void UpdateMetalTrendChart()
        {
            if (SelectedMetal == null) return;

            SeriesCollection = new SeriesCollection();
            Labels = new List<string>();

            var distances = _allSamples
                .Where(s => s.MetalId == SelectedMetal.Id)
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

            foreach (var year in AvailableYears)
            {
                var values = new ChartValues<double>();
                bool hasSignificantValues = false;

                foreach (var distance in distances)
                {
                    var sample = _allSamples
                        .FirstOrDefault(s => s.SamplingDate.Year == year &&
                                           s.MetalId == SelectedMetal.Id &&
                                           Math.Abs(ParseDistance(s.Location.DistanceFromSource) - distance) < 0.1);

                    double value = sample != null ?
                        (double)_valueConverter.ConvertBack(sample.Value, typeof(double), null, CultureInfo.InvariantCulture) :
                        0;
                    values.Add(value);
                    if (value >= 0.001) hasSignificantValues = true;
                }

                if (hasSignificantValues)
                {
                    SeriesCollection.Add(new LineSeries
                    {
                        Title = $"{year} г.",
                        Values = values,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 10
                    });
                }
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

        public void RefreshData()
        {
            try
            {
                _allSamples = _dbHelper.GetAllSamplesWithLocations();
                AvailableYears = _allSamples
                    .Select(s => s.SamplingDate.Year)
                    .Distinct()
                    .OrderBy(y => y)
                    .ToList();
                UpdateChart();
                MessageBox.Show("Данные успешно обновлены");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления данных: {ex.Message}");
            }
        }

        public void ExportToExcel()
        {
            try
            {
                if (SeriesCollection == null || Labels == null)
                {
                    MessageBox.Show("Нет данных для экспорта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    FileName = $"График_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Данные");

                        // Заголовок
                        worksheet.Cell(1, 1).Value = "Экспорт данных графика";
                        worksheet.Range(1, 1, 1, 2).Merge().Style.Font.Bold = true;

                        // Заголовки столбцов
                        int dataRow = 3;
                        worksheet.Cell(dataRow, 1).Value = "Расстояние (км)";

                        for (int i = 0; i < SeriesCollection.Count; i++)
                        {
                            worksheet.Cell(dataRow, i + 2).Value = SeriesCollection[i].Title;
                        }

                        // Данные
                        for (int i = 0; i < Labels.Count; i++)
                        {
                            worksheet.Cell(dataRow + 1 + i, 1).Value = Labels[i];

                            for (int j = 0; j < SeriesCollection.Count; j++)
                            {
                                if (i < SeriesCollection[j].Values.Count)
                                {
                                    worksheet.Cell(dataRow + 1 + i, j + 2).Value =
                                        ((ChartValues<double>)SeriesCollection[j].Values)[i];
                                }
                            }
                        }

                        // Форматирование
                        var dataRange = worksheet.Range(dataRow, 1, dataRow + Labels.Count, SeriesCollection.Count + 1);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(saveDialog.FileName);
                    }

                    MessageBox.Show("Экспорт завершен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}