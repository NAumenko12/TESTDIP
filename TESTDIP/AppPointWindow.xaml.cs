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

namespace TESTDIP
{
    public partial class AppPointWindow : Window
    {
        public string PointName { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public AppPointWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(LatitudeTextBox.Text, out double lat) && double.TryParse(LongitudeTextBox.Text, out double lon))
            {
                PointName = NameTextBox.Text;
                Latitude = lat;
                Longitude = lon;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректные значения широты и долготы.");
            }
        }
    }
}
