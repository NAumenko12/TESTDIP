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
using System.ComponentModel;
using TESTDIP.ViewModel;

namespace TESTDIP
{
    /// <summary>
    /// Логика взаимодействия для SamplesWindow.xaml
    /// </summary>
    public partial class SamplesWindow : Window
    {
        public SamplesWindow(Location location, IEnumerable<Sample> samples)
        {
            InitializeComponent();
            DataContext = new SamplesViewModel(location, samples);
        }
    }
}