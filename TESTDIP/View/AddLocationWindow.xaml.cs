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
using TESTDIP.ViewModel;

namespace TESTDIP
{
    public partial class AddLocationWindow : Window
    {
        public AddLocationWindow()
        {
            InitializeComponent();
            var viewModel = new AddLocationViewModel();
            viewModel.RequestClose += (sender, result) =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = viewModel;
        }

        public Location Location => (DataContext as AddLocationViewModel)?.Location;
    }
}