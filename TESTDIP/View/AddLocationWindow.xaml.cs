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
using TESTDIP.Model;

namespace TESTDIP
{
    public partial class AddLocationWindow : Window
    {
        public Location Location { get; private set; }

        public AddLocationWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(LatitudeTextBox.Text, out double latitude) ||
                !double.TryParse(LongitudeTextBox.Text, out double longitude))
            {
                MessageBox.Show("Введите корректные координаты", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Location = new Location
            {
                Name = NameTextBox.Text,
                SiteNumber = SiteNumberTextBox.Text,
                DistanceFromSource = DistanceTextBox.Text,
                Description = DescriptionTextBox.Text,
                Latitude = latitude,
                Longitude = longitude
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
