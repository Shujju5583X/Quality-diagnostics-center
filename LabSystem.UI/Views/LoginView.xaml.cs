using System.Windows;
using System.Windows.Controls;
using LabSystem.UI.ViewModels;

namespace LabSystem.UI.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void PinBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as LoginViewModel;
            if (vm != null)
            {
                vm.Pin = ((PasswordBox)sender).Password;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
