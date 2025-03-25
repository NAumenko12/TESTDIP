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

        public string LocationName => _location.Name;
        public string LocationSiteNumber => _location.SiteNumber;
        public ObservableCollection<Sample> Samples => _samples;

        public SamplesWindow(Location location, IEnumerable<Sample> samples)
        {
            InitializeComponent();
            _location = location;
            _samples = new ObservableCollection<Sample>(samples);
            DataContext = this;
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
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
