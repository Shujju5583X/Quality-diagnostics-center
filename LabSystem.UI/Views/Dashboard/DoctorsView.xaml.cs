using System.Windows.Controls;

namespace LabSystem.UI.Views.Dashboard
{
    public partial class DoctorsView : UserControl
    {
        public DoctorsView()
        {
            InitializeComponent();
        }

        private void DoctorsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DoctorsTabControl != null)
            {
                DoctorsTabControl.SelectedIndex = 0;
            }
        }
    }
}
