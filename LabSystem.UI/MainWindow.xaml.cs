using System.Windows;
using System.Windows.Controls;
using LabSystem.UI.ViewModels;

namespace LabSystem.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PinBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && passwordBox.DataContext is LoginViewModel loginVm)
            {
                loginVm.Pin = passwordBox.Password;
            }
        }
    }
}
