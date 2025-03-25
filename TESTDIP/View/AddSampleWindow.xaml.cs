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

namespace TESTDIP.View
{
    /// <summary>
    /// Логика взаимодействия для AddSampleWindow.xaml
    /// </summary>
    public partial class AddSampleWindow : Window
    {
        private readonly int _locationId;
        private readonly string _locationName;
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
        public int NewSampleId { get; private set; }

        public Sample NewSample { get; private set; }
        public string LocationName { get; }

        public AddSampleWindow(int locationId, string locationName)
        {
            InitializeComponent();
            _locationId = locationId;
            LocationName = locationName;
            DataContext = this;

            LoadMetals();
            SamplingDatePicker.SelectedDate = DateTime.Today;
        }

        private void LoadMetals()
        {
            try
            {
                MetalComboBox.ItemsSource = _dbHelper.GetMetals();
                MetalComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка металлов: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (MetalComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите металл из списка", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ValueTextBox.Text))
            {
                MessageBox.Show("Введите значение пробы", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(AnalyticsNumberTextBox.Text))
            {
                MessageBox.Show("Введите номер аналитики", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NewSample = new Sample
            {
                LocationId = _locationId,
                MetalId = (int)MetalComboBox.SelectedValue,
                Type = TypeTextBox.Text,
                Fraction = FractionTextBox.Text, 
                Repetition = int.TryParse(RepetitionTextBox.Text, out int rep) ? rep : (int?)null,
                Value = ValueTextBox.Text,
                SamplingDate = SamplingDatePicker.SelectedDate ?? DateTime.Today,
                AnalyticsNumber = AnalyticsNumberTextBox.Text,
                Metal = (Metal)MetalComboBox.SelectedItem
            };

            try
            {
                NewSampleId = _dbHelper.AddSample(NewSample);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения пробы: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
