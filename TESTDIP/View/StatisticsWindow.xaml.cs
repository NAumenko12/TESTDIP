using ClosedXML.Excel;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Office.Interop.Excel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TESTDIP.DataBase;
using TESTDIP.Model;
using TESTDIP.ViewModel;

namespace TESTDIP.View
{
    public partial class StatisticsWindow : System.Windows.Window
    {
        public Func<double, string> YAxisFormatter { get; set; }
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
        private List<Sample> _allSamples = new List<Sample>();
        private readonly StringToDoubleConverter _valueConverter = new StringToDoubleConverter();
        public LiveCharts.SeriesCollection SeriesCollection { get; set; }
        public List<string> Labels { get; set; }
        public StatisticsWindow()
        {
            InitializeComponent();
            DataContext = this;
            YAxisFormatter = value => value.ToString("F3");
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
            SeriesCollection = new LiveCharts.SeriesCollection();
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
            SeriesCollection = new LiveCharts.SeriesCollection();
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
                bool hasSignificantValues = false;
                foreach (var distance in distances)
                {
                    var sample = _allSamples
                        .FirstOrDefault(s => s.SamplingDate.Year == selectedYear &&
                                           s.MetalId == metal.Id &&
                                           Math.Abs(ParseDistance(s.Location.DistanceFromSource) - distance) < 0.1);

                    double value = sample != null ?
                        (double)_valueConverter.ConvertBack(sample.Value, typeof(double), null, CultureInfo.InvariantCulture) :
                        0;
                    values.Add(value);
                    if (value >= 0.001)
                    {
                        hasSignificantValues = true;
                    }
                }
                if (hasSignificantValues)
                {
                    SeriesCollection.Add(new LineSeries
                    {
                        Title = $"{metal.Name}",
                        Values = values,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 10
                    });
                }
            }
            if (SeriesCollection.Count == 0)
            {
                MessageBox.Show("Нет данных со значимыми значениями (>= 0.001) для выбранного года");
                return;
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
            SeriesCollection = new LiveCharts.SeriesCollection();
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
                bool hasSignificantValues = false;
                foreach (var distance in distances)
                {
                    var sample = _allSamples
                        .FirstOrDefault(s => s.SamplingDate.Year == year &&
                                           s.MetalId == selectedMetalId &&
                                           Math.Abs(ParseDistance(s.Location.DistanceFromSource) - distance) < 0.1);
                    double value = sample != null ?
                        (double)_valueConverter.ConvertBack(sample.Value, typeof(double), null, CultureInfo.InvariantCulture) :
                        0;
                    values.Add(value);
                    if (value >= 0.001)
                    {
                        hasSignificantValues = true;
                    }
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
            if (SeriesCollection.Count == 0)
            {
                MessageBox.Show("Нет данных со значимыми значениями (>= 0.001) для выбранного металла");
                return;
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
        private void ExportChartImage_Click(object sender, RoutedEventArgs e)
        {
            var formatDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                Title = "Сохранить график как изображение",
                FileName = GenerateImageFileName()
            };

            if (formatDialog.ShowDialog() == true)
            {
                try
                {
                    // Сохраняем текущие цвета
                    var originalAxisXColor = Chart.AxisX[0].Foreground;
                    var originalAxisYColor = Chart.AxisY[0].Foreground;
                    var originalBackground = Chart.Background;

                    // Временно меняем цвета для экспорта
                    Chart.AxisX[0].Foreground = Brushes.Black;
                    Chart.AxisY[0].Foreground = Brushes.Black;
                    Chart.Background = Brushes.White;

                    // Даем время на применение стилей
                    Chart.UpdateLayout();
                    Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

                    // Рендерим
                    var renderBitmap = new RenderTargetBitmap(
                        (int)Chart.ActualWidth,
                        (int)Chart.ActualHeight,
                        96d, 96d, PixelFormats.Pbgra32);

                    renderBitmap.Render(Chart);

                    // Восстанавливаем оригинальные цвета
                    Chart.AxisX[0].Foreground = originalAxisXColor;
                    Chart.AxisY[0].Foreground = originalAxisYColor;
                    Chart.Background = originalBackground;

                    // Сохраняем файл
                    string extension = System.IO.Path.GetExtension(formatDialog.FileName).ToLower();
                    BitmapEncoder encoder = extension == ".png"
                        ? new PngBitmapEncoder()
                        : new JpegBitmapEncoder { QualityLevel = 90 };

                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (var stream = File.Create(formatDialog.FileName))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show("График сохранен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private string GenerateImageFileName()
        {
            string metalName = (MetalComboBox.SelectedItem as Metal)?.Name ?? "Все металлы";
            string year = YearComboBox.SelectedItem?.ToString() ?? "Все годы";
            return $"График_{metalName}_{year}.png";
        }
        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = "График_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    string tempImagePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chart_export.png");
                    SaveChartAsImage(tempImagePath);
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Данные");
                        worksheet.Cell(1, 1).Value = "Экспорт данных графика";
                        worksheet.Range(1, 1, 1, 2).Merge().Style.Font.Bold = true;
                        int dataRow = 3;
                        worksheet.Cell(dataRow, 1).Value = "Расстояние (км)";
                        for (int i = 0; i < SeriesCollection.Count; i++)
                        {
                            worksheet.Cell(dataRow, i + 2).Value = SeriesCollection[i].Title;
                        }
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
                        // Добавляем изображение графика
                        if (File.Exists(tempImagePath))
                        {
                            var imageRow = dataRow + Labels.Count + 2;

                            var picture = worksheet.AddPicture(tempImagePath)
                                .MoveTo(worksheet.Cell(imageRow, 1))
                                .Scale(0.8);

                            // Настройки изображения
                            picture.Width = (int)(Chart.ActualWidth * 0.7);
                            picture.Height = (int)(Chart.ActualHeight * 0.7);
                        }
                        // Форматирование таблицы
                        var dataRange = worksheet.Range(dataRow, 1, dataRow + Labels.Count, SeriesCollection.Count + 1);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveDialog.FileName);
                    }
                    // Удаляем временный файл
                    if (File.Exists(tempImagePath))
                        File.Delete(tempImagePath);

                    MessageBox.Show("Экспорт завершен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void SaveChartAsImage(string filePath)
        {
            try
            {
                if (Chart == null || Chart.ActualWidth <= 0 || Chart.ActualHeight <= 0)
                {
                    MessageBox.Show("Ошибка: график не инициализирован или имеет нулевой размер");
                    return;
                }
                double scale = 1.5;
                int dpi = 144;
                var renderTarget = new RenderTargetBitmap(
                    (int)(Chart.ActualWidth * scale),
                    (int)(Chart.ActualHeight * scale),
                    dpi, dpi, PixelFormats.Pbgra32);
                var border = new System.Windows.Controls.Border
                {
                    Width = Chart.ActualWidth * scale,
                    Height = Chart.ActualHeight * scale,
                    Child = new CartesianChart
                    {
                        Series = Chart.Series,
                        AxisX = { Chart.AxisX.FirstOrDefault() },
                        AxisY = { Chart.AxisY.FirstOrDefault() },
                        LegendLocation = Chart.LegendLocation,
                        Width = Chart.ActualWidth * scale,
                        Height = Chart.ActualHeight * scale,
                    }
                };
                border.Measure(new Size(border.Width, border.Height));
                border.Arrange(new Rect(0, 0, border.Width, border.Height));
                Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                Thread.Sleep(300);
                renderTarget.Render(border);
                using (var stream = File.Create(filePath))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderTarget));
                    encoder.Save(stream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении графика: {ex.Message}");
            }
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