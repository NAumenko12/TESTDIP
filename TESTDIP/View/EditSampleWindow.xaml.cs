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

namespace TESTDIP.View
{
    /// <summary>
    /// Логика взаимодействия для EditSampleWindow.xaml
    /// </summary>
    public partial class EditSampleWindow : Window
    {
        public Sample EditedSample { get; private set; }

        public EditSampleWindow(Sample sampleToEdit)
        {
            InitializeComponent();
            EditedSample = new Sample
            {
                Id = sampleToEdit.Id,
                Metal = sampleToEdit.Metal,
                Value = sampleToEdit.Value,
                SamplingDate = sampleToEdit.SamplingDate,
                AnalyticsNumber = sampleToEdit.AnalyticsNumber,
                Type = sampleToEdit.Type,
                Fraction = sampleToEdit.Fraction,
                Repetition = sampleToEdit.Repetition
            };

            DataContext = this;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
