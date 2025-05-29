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
using TESTDIP.ViewModel;
using TESTDIP.ViewModels;

namespace TESTDIP.View
{
    /// <summary>
    /// Логика взаимодействия для AddSampleWindow.xaml
    /// </summary>
    public partial class AddSampleWindow : Window
    {
        public AddSampleWindow(int locationId, string locationName)
        {
            InitializeComponent();
            var viewModel = new AddSampleViewModel(locationId, locationName);
            viewModel.RequestClose += (sender, result) =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = viewModel;
        }

        public Sample NewSample => (DataContext as AddSampleViewModel)?.NewSample;
        public int NewSampleId => (DataContext as AddSampleViewModel)?.NewSampleId ?? 0;
    }
}