using System.Windows;

namespace TESTDIP
{
    public partial class PointDetailsWindow : Window
    {
        public PointDetailsWindow(MapPoint point)
        {
            InitializeComponent();

    
            NameTextBox.Text = point.Name;
            LatitudeTextBox.Text = point.Latitude.ToString();
            LongitudeTextBox.Text = point.Longitude.ToString();
        }
    }
}
