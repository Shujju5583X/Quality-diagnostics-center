using System.Windows;

namespace LabSystem.UI.Views
{
    public partial class PatientHistoryWindow : Window
    {
        public PatientHistoryWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
