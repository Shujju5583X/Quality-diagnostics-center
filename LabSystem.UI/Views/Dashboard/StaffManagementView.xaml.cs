using System.Windows;
using System.Windows.Controls;

namespace LabSystem.UI.Views.Dashboard
{
    public partial class StaffManagementView : UserControl
    {
        public StaffManagementView()
        {
            InitializeComponent();
        }

        private void StaffSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var viewModel = DataContext as ViewModels.StaffManagementViewModel;
            if (viewModel != null)
            {
                viewModel.SearchQuery = StaffSearchBox.Text;
            }
        }
    }
}
