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

namespace TESTDIP.View
{
    /// <summary>
    /// Логика взаимодействия для EditSampleWindow.xaml
    /// </summary>
    public partial class EditSampleWindow : Window
    {
        public EditSampleWindow(Sample sampleToEdit)
        {
            InitializeComponent();
            var viewModel = new EditSampleViewModel(sampleToEdit);
            viewModel.RequestClose += (sender, result) =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = viewModel;
        }

        public Sample EditedSample => (DataContext as EditSampleViewModel)?.EditedSample;
    }
}