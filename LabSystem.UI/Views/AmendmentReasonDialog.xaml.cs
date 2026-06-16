using System.Windows;

namespace LabSystem.UI.Views
{
    public partial class AmendmentReasonDialog : Window
    {
        public string Reason { get { return ReasonTextBox.Text; } }

        public AmendmentReasonDialog()
        {
            InitializeComponent();
            ReasonTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
            {
                MessageBox.Show("Amendment reason is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }
    }
}
