using System.Windows;
using System.Windows.Controls;
using LabSystem.UI.ViewModels;

namespace LabSystem.UI.Views
{
    public partial class PinSetupView : Window
    {
        public PinSetupView()
        {
            InitializeComponent();
        }

        private void NewPinBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PinSetupViewModel;
            if (vm != null)
            {
                vm.NewPin = ((PasswordBox)sender).Password;
            }
        }

        private void ConfirmPinBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PinSetupViewModel;
            if (vm != null)
            {
                vm.ConfirmPin = ((PasswordBox)sender).Password;
            }
        }
    }
}
