using LiveCharts.Wpf;
using LiveCharts;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Data;
using ClosedXML.Excel;
using Separator = LiveCharts.Wpf.Separator;
using Path = System.IO.Path; 

namespace TESTDIP.View
{
    public partial class CalculationDataWindow : Window
    {
        private readonly List<GridPoint> _gridPoints;
        private readonly Metal _metal;
        private readonly int _year;
        private readonly DatabaseHelper _dbHelper;
        private readonly DateTime _calculationDate;

        public SeriesCollection ChartSeries { get; set; }
        public ChartValues<double> ChartValues { get; set; }
        public List<string> DistanceLabels { get; set; }

        public CalculationDataWindow(List<GridPoint> gridPoints, Metal metal, int year, DatabaseHelper dbHelper)
        {
            if (gridPoints == null || gridPoints.Count == 0)
                throw new ArgumentException("Нет данных для отображения");

            InitializeComponent();

            _gridPoints = gridPoints;
            _metal = metal;
            _year = year;
            _dbHelper = dbHelper;
            _calculationDate = DateTime.Now; 

            ShowLoadingIndicator();
            LoadDataAsync();

            DataContext = this;
        }

        private void ShowLoadingIndicator()
        {
            var loadingData = new List<object>
            {
                new { Lat = "Загрузка...", Lon = "", Concentration = "", DistanceFromSource = "" }
            };
            DataGridResults.ItemsSource = loadingData;
        }

        private async void LoadDataAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var optimizedData = OptimizeDataForDisplay(_gridPoints);
                    var chartData = PrepareChartDataWithFixedAxis(_gridPoints);

                    Dispatcher.Invoke(() =>
                    {
                        DataGridResults.ItemsSource = optimizedData;
                        SetupChartWithFixedAxis(chartData);

                        var totalPoints = _gridPoints.Count;
                        var pointsIn100km = _gridPoints.Count(p => p.DistanceFromSource <= 100.0);
                        var maxDistance = _gridPoints.Any() ? _gridPoints.Max(p => p.DistanceFromSource) : 0;

                        DataInfoTextBlock.Text = $"Металл: {_metal?.Name}, Год: {_year}, " +
                            $"Всего точек: {totalPoints}, в пределах 100 км: {pointsIn100km}, " +
                            $"макс. расстояние: {maxDistance:F1} км, дата расчета: {_calculationDate:dd.MM.yyyy HH:mm}";
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<GridPoint> OptimizeDataForDisplay(List<GridPoint> originalData)
        {
            const int maxDisplayPoints = 1000;

            if (originalData.Count <= maxDisplayPoints)
                return originalData;

            int step = originalData.Count / maxDisplayPoints;
            return originalData.Where((point, index) => index % step == 0).ToList();
        }

        private List<ChartDataPoint> PrepareChartDataWithFixedAxis(List<GridPoint> originalData)
        {
            var chartData = new List<ChartDataPoint>();

            for (double distance = 0; distance <= 100; distance += 7.5)
            {
                var pointsInRange = originalData
                    .Where(p => Math.Abs(p.DistanceFromSource - distance) <= 3.75)
                    .ToList();

                double concentration = 0;
                if (pointsInRange.Any())
                {
                    concentration = pointsInRange.Average(p => p.Concentration);
                }
                else if (distance > 0)
                {
                    var closestBefore = originalData
                        .Where(p => p.DistanceFromSource < distance)
                        .OrderByDescending(p => p.DistanceFromSource)
                        .FirstOrDefault();

                    var closestAfter = originalData
                        .Where(p => p.DistanceFromSource > distance)
                        .OrderBy(p => p.DistanceFromSource)
                        .FirstOrDefault();

                    if (closestBefore != null && closestAfter != null)
                    {
                        double t = (distance - closestBefore.DistanceFromSource) /
                                  (closestAfter.DistanceFromSource - closestBefore.DistanceFromSource);
                        concentration = closestBefore.Concentration +
                                      (closestAfter.Concentration - closestBefore.Concentration) * t;
                    }
                    else if (closestBefore != null)
                    {
                        concentration = closestBefore.Concentration;
                    }
                    else if (closestAfter != null)
                    {
                        concentration = closestAfter.Concentration;
                    }
                }

                chartData.Add(new ChartDataPoint
                {
                    Distance = distance,
                    AverageConcentration = Math.Max(0, concentration),
                    PointCount = pointsInRange.Count
                });
            }

            return chartData;
        }

        private void SetupChartWithFixedAxis(List<ChartDataPoint> chartData)
        {
            try
            {
                if (chartData == null || !chartData.Any())
                {
                    Console.WriteLine("Нет данных для построения диаграммы");
                    return;
                }

                ChartValues = new ChartValues<double>(chartData.Select(p => p.AverageConcentration));
                DistanceLabels = chartData.Select(p => p.Distance.ToString("F1")).ToList();

                ChartSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = $"Концентрация {_metal?.Name ?? "металла"} (0-100 км)",
                        Values = ChartValues,
                        PointGeometrySize = 4,
                        LineSmoothness = 0.2,
                        StrokeThickness = 2,
                        Fill = System.Windows.Media.Brushes.Transparent
                    }
                };

                var chart = this.FindName("CartesianChart") as CartesianChart;
                if (chart != null)
                {
                    chart.Series = ChartSeries;

                    chart.AxisX.Clear();
                    chart.AxisX.Add(new Axis
                    {
                        Title = "Расстояние от источника (км)",
                        Labels = DistanceLabels,
                        Separator = new Separator { Step = 1, IsEnabled = true },
                        LabelsRotation = 45
                    });

                    chart.AxisY.Clear();
                    chart.AxisY.Add(new Axis
                    {
                        Title = "Концентрация (мг/м³)",
                        LabelFormatter = value => value.ToString("F3"),
                        Separator = new Separator { IsEnabled = true }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании диаграммы: {ex.Message}");
                MessageBox.Show($"Ошибка при создании диаграммы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExportChartImage_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                Title = "Экспорт диаграммы",
                FileName = GenerateChartFileName()
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    ExportChartToImage(saveDialog.FileName);
                    MessageBox.Show($"Диаграмма успешно экспортирована:\n{saveDialog.FileName}",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте диаграммы: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportChartToImage(string filePath)
        {
            var chart = this.FindName("CartesianChart") as CartesianChart;
            if (chart == null)
            {
                throw new InvalidOperationException("График не найден");
            }

            
            if (chart.ActualWidth == 0 || chart.ActualHeight == 0)
            {
                throw new InvalidOperationException("График не инициализирован");
            }

            try
            {
                
                var originalBackground = chart.Background;
                var originalForeground = chart.Foreground;

                
                chart.Background = Brushes.White;
                chart.Foreground = Brushes.Black;

                
                chart.UpdateLayout();
                chart.InvalidateVisual();

                
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                Thread.Sleep(100);

                
                double exportWidth = Math.Max(800, chart.ActualWidth);
                double exportHeight = Math.Max(600, chart.ActualHeight);
                double dpi = 96;

                
                var renderBitmap = new RenderTargetBitmap(
                    (int)exportWidth,
                    (int)exportHeight,
                    dpi, dpi, PixelFormats.Pbgra32);

                
                renderBitmap.Render(chart);

                
                chart.Background = originalBackground;
                chart.Foreground = originalForeground;

                
                BitmapEncoder encoder;
                string extension = Path.GetExtension(filePath).ToLower();

                switch (extension)
                {
                    case ".png":
                        encoder = new PngBitmapEncoder();
                        break;
                    case ".jpg":
                    case ".jpeg":
                        encoder = new JpegBitmapEncoder { QualityLevel = 95 };
                        break;
                    default:
                        encoder = new PngBitmapEncoder();
                        break;
                }

                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using (var stream = File.Create(filePath))
                {
                    encoder.Save(stream);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при создании изображения: {ex.Message}", ex);
            }
        }

        private string GenerateChartFileName()
        {
            string metalName = _metal?.Name ?? "Неизвестный_металл";
            string safeMetalName = string.Join("_", metalName.Split(Path.GetInvalidFileNameChars()));
            return $"Диаграмма_{safeMetalName}_{_year}_{_calculationDate:yyyyMMdd_HHmm}.png";
        }
        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Экспорт в Excel",
                FileName = $"Расчет_{_metal?.Name}_{_year}_{_calculationDate:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Данные расчета");

                        
                        worksheet.Cell(1, 1).Value = "Широта";
                        worksheet.Cell(1, 2).Value = "Долгота";
                        worksheet.Cell(1, 3).Value = "Концентрация (мг/м³)";
                        worksheet.Cell(1, 4).Value = "Металл";
                        worksheet.Cell(1, 5).Value = "Расстояние (км)";
                        worksheet.Cell(1, 6).Value = "Год";
                        worksheet.Cell(1, 7).Value = "Дата расчета";

                        
                        var headerRange = worksheet.Range(1, 1, 1, 7);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                        
                        int row = 2;
                        foreach (var point in _gridPoints)
                        {
                            worksheet.Cell(row, 1).Value = point.Lat;
                            worksheet.Cell(row, 2).Value = point.Lon;
                            worksheet.Cell(row, 3).Value = point.Concentration;
                            worksheet.Cell(row, 4).Value = _metal?.Name ?? "Неизвестно";
                            worksheet.Cell(row, 5).Value = point.DistanceFromSource;
                            worksheet.Cell(row, 6).Value = _year;
                            worksheet.Cell(row, 7).Value = _calculationDate.ToString("dd.MM.yyyy HH:mm:ss");
                            row++;
                        }

                        
                        worksheet.Columns().AdjustToContents();

                        
                        var summarySheet = workbook.Worksheets.Add("Сводка");
                        summarySheet.Cell(1, 1).Value = "Параметр";
                        summarySheet.Cell(1, 2).Value = "Значение";

                        summarySheet.Cell(2, 1).Value = "Металл";
                        summarySheet.Cell(2, 2).Value = _metal?.Name ?? "Неизвестно";

                        summarySheet.Cell(3, 1).Value = "Год расчета";
                        summarySheet.Cell(3, 2).Value = _year;

                        summarySheet.Cell(4, 1).Value = "Дата расчета";
                        summarySheet.Cell(4, 2).Value = _calculationDate.ToString("dd.MM.yyyy HH:mm:ss");

                        summarySheet.Cell(5, 1).Value = "Общее количество точек";
                        summarySheet.Cell(5, 2).Value = _gridPoints.Count;

                        summarySheet.Cell(6, 1).Value = "Максимальная концентрация";
                        summarySheet.Cell(6, 2).Value = _gridPoints.Max(p => p.Concentration);

                        summarySheet.Cell(7, 1).Value = "Минимальная концентрация";
                        summarySheet.Cell(7, 2).Value = _gridPoints.Min(p => p.Concentration);

                        summarySheet.Cell(8, 1).Value = "Средняя концентрация";
                        summarySheet.Cell(8, 2).Value = _gridPoints.Average(p => p.Concentration);

                        summarySheet.Columns().AdjustToContents();

                        workbook.SaveAs(saveDialog.FileName);
                    }

                    MessageBox.Show($"Данные успешно экспортированы в Excel:\n{saveDialog.FileName}\n" +
                                  $"Записей: {_gridPoints.Count}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        
        private void SaveToDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    $"Сохранить {_gridPoints.Count} записей в базу данных?\n\n" +
                    $"Металл: {_metal?.Name}\n" +
                    $"Год: {_year}\n" +
                    $"Дата расчета: {_calculationDate:dd.MM.yyyy HH:mm:ss}",
                    "Подтверждение сохранения",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    int savedCount = _dbHelper.SaveCalculationResults(_gridPoints, _metal.Id, _year);

                    MessageBox.Show($"Успешно сохранено {savedCount} записей в базу данных",
                        "Сохранение завершено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении в БД: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    
}