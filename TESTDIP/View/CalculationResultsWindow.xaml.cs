using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ClosedXML.Excel;
using TESTDIP.DataBase;
using TESTDIP.Model;

namespace TESTDIP.View
{
    public partial class CalculationResultsWindow : Window
    {
        private readonly DatabaseHelper _dbHelper;
        private List<CalculationResultSummary> _allResults;
        private List<CalculationResultSummary> _filteredResults;

        public CalculationResultsWindow()
        {
            InitializeComponent();
            _dbHelper = new DatabaseHelper();
            InitializeFilters();
            LoadData();
        }

        private void InitializeFilters()
        {
            try
            {
                // Загружаем металлы для фильтра
                var metals = _dbHelper.GetMetals();
                metals.Insert(0, new Metal { Id = -1, Name = "Все металлы" });
                MetalFilterComboBox.ItemsSource = metals;
                MetalFilterComboBox.SelectedIndex = 0;

                // Загружаем годы для фильтра
                var years = _dbHelper.GetCalculationYears();
                years.Insert(0, -1); // "Все годы"
                YearFilterComboBox.ItemsSource = years.Select(y => y == -1 ? "Все годы" : y.ToString()).ToList();
                YearFilterComboBox.SelectedIndex = 0;

                // Устанавливаем диапазон дат
                DateFromPicker.SelectedDate = null;
                DateToPicker.SelectedDate = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                _allResults = _dbHelper.GetCalculationResultsSummary();
                _filteredResults = new List<CalculationResultSummary>(_allResults);

                ApplyFilters();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                _filteredResults = new List<CalculationResultSummary>(_allResults);

                // Фильтр по металлу
                if (MetalFilterComboBox.SelectedValue != null && (int)MetalFilterComboBox.SelectedValue != -1)
                {
                    int selectedMetalId = (int)MetalFilterComboBox.SelectedValue;
                    _filteredResults = _filteredResults.Where(r => r.MetalId == selectedMetalId).ToList();
                }

                // Фильтр по году
                if (YearFilterComboBox.SelectedIndex > 0)
                {
                    string selectedYearStr = YearFilterComboBox.SelectedItem.ToString();
                    if (int.TryParse(selectedYearStr, out int selectedYear))
                    {
                        _filteredResults = _filteredResults.Where(r => r.Year == selectedYear).ToList();
                    }
                }

                // Фильтр по дате от
                if (DateFromPicker.SelectedDate.HasValue)
                {
                    DateTime fromDate = DateFromPicker.SelectedDate.Value.Date;
                    _filteredResults = _filteredResults.Where(r => r.CalculationDate.Date >= fromDate).ToList();
                }

                // Фильтр по дате до
                if (DateToPicker.SelectedDate.HasValue)
                {
                    DateTime toDate = DateToPicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
                    _filteredResults = _filteredResults.Where(r => r.CalculationDate <= toDate).ToList();
                }

                // Сортируем по дате (новые сверху)
                _filteredResults = _filteredResults.OrderByDescending(r => r.CalculationDate).ToList();

                ResultsDataGrid.ItemsSource = _filteredResults;
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            TotalRecordsTextBlock.Text = $"Всего записей: {_allResults?.Count ?? 0}";
            FilteredRecordsTextBlock.Text = $"Отфильтровано: {_filteredResults?.Count ?? 0}";
            SelectedRecordsTextBlock.Text = $"Выбрано: {ResultsDataGrid.SelectedItems.Count}";

            // Обновляем состояние кнопок
            bool hasSelection = ResultsDataGrid.SelectedItems.Count > 0;
            ViewDetailsButton.IsEnabled = ResultsDataGrid.SelectedItems.Count == 1;
            ExportSelectedButton.IsEnabled = hasSelection;
            DeleteSelectedButton.IsEnabled = hasSelection;
        }

        // Обработчики событий фильтров
        private void MetalFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        private void YearFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        private void DateFromPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        private void DateToPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            MetalFilterComboBox.SelectedIndex = 0;
            YearFilterComboBox.SelectedIndex = 0;
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            ApplyFilters();
        }

        // Обработчики событий DataGrid
        private void ResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStatistics();
        }

        // Просмотр деталей
        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewDetails();
        }

        private void ViewDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ViewDetails();
        }

        private void ViewDetails()
        {
            if (ResultsDataGrid.SelectedItem is CalculationResultSummary selectedResult)
            {
                try
                {
                    var detailPoints = _dbHelper.GetCalculationDetails(selectedResult.Id);
                    var metal = _dbHelper.GetMetals().FirstOrDefault(m => m.Id == selectedResult.MetalId);

                    var detailWindow = new CalculationDataWindow(detailPoints, metal, selectedResult.Year, _dbHelper);
                    detailWindow.Owner = this;
                    detailWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке деталей: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Экспорт
        private void ExportSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void ExportToExcelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void ExportToExcel()
        {
            if (ResultsDataGrid.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите записи для экспорта", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Экспорт результатов расчетов",
                FileName = $"Результаты_расчетов_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var selectedResults = ResultsDataGrid.SelectedItems.Cast<CalculationResultSummary>().ToList();

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Результаты расчетов");

                        // Заголовки
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Металл";
                        worksheet.Cell(1, 3).Value = "Год";
                        worksheet.Cell(1, 4).Value = "Дата расчета";
                        worksheet.Cell(1, 5).Value = "Количество точек";
                        worksheet.Cell(1, 6).Value = "Мин. концентрация";
                        worksheet.Cell(1, 7).Value = "Макс. концентрация";
                        worksheet.Cell(1, 8).Value = "Средняя концентрация";
                        worksheet.Cell(1, 9).Value = "Макс. расстояние (км)";

                        // Форматирование заголовков
                        var headerRange = worksheet.Range(1, 1, 1, 9);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                        // Данные
                        int row = 2;
                        foreach (var result in selectedResults)
                        {
                            worksheet.Cell(row, 1).Value = result.Id;
                            worksheet.Cell(row, 2).Value = result.MetalName;
                            worksheet.Cell(row, 3).Value = result.Year;
                            worksheet.Cell(row, 4).Value = result.CalculationDate.ToString("dd.MM.yyyy HH:mm:ss");
                            worksheet.Cell(row, 5).Value = result.PointCount;
                            worksheet.Cell(row, 6).Value = result.MinConcentration;
                            worksheet.Cell(row, 7).Value = result.MaxConcentration;
                            worksheet.Cell(row, 8).Value = result.AvgConcentration;
                            worksheet.Cell(row, 9).Value = result.MaxDistance;
                            row++;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveDialog.FileName);
                    }

                    MessageBox.Show($"Экспорт завершен успешно!\nЭкспортировано записей: {selectedResults.Count}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Удаление
        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelected();
        }

        private void DeleteSelectedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelected();
        }

        private void DeleteSelected()
        {
            if (ResultsDataGrid.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите записи для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedResults = ResultsDataGrid.SelectedItems.Cast<CalculationResultSummary>().ToList();

            var result = MessageBox.Show(
                $"Удалить {selectedResults.Count} выбранных расчетов?\n\nЭто действие нельзя отменить!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    int deletedCount = 0;
                    foreach (var calculation in selectedResults)
                    {
                        if (_dbHelper.DeleteCalculationResult(calculation.Id))
                        {
                            deletedCount++;
                        }
                    }

                    MessageBox.Show($"Удалено расчетов: {deletedCount}", "Удаление завершено",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadData(); // Перезагружаем данные
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
