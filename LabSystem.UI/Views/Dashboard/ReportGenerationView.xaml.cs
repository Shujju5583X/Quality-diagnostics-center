using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Serilog;

namespace LabSystem.UI.Views.Dashboard
{
    public partial class ReportGenerationView : UserControl
    {
        private string _tempPdfPath;

        public ReportGenerationView()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize ReportGenerationView");
                throw;
            }
        }

        private async void CompletedOrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await UpdateReportPreviewAsync();
        }

        private async void ReportLetterheadComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await UpdateReportPreviewAsync();
        }

        private async Task UpdateReportPreviewAsync()
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null && vm.SelectedReportOrder != null)
            {
                try
                {
                    bool includeLetterhead = (ReportLetterheadComboBox.SelectedIndex == 0);
                    string oldPath = _tempPdfPath;

                    _tempPdfPath = await vm.ReportService.GenerateReportAsync(vm.SelectedReportOrder, includeLetterhead);

                    if (oldPath != null && oldPath != _tempPdfPath && File.Exists(oldPath))
                    {
                        try { File.Delete(oldPath); } catch { }
                    }

                    try
                    {
                        ReportWebViewer.Navigate(new Uri(_tempPdfPath).AbsoluteUri);
                    }
                    catch
                    {
                        Process.Start(_tempPdfPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating Report preview.");
                }
            }
        }

        private void SaveReportPdf_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.DashboardViewModel;
            if (vm != null && vm.SelectedReportOrder != null)
            {
                try
                {
                    var sfd = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "PDF Documents (*.pdf)|*.pdf",
                        FileName = "Report_" + vm.SelectedReportOrder.OrderId + ".pdf"
                    };
                    if (sfd.ShowDialog() == true)
                    {
                        File.Copy(_tempPdfPath, sfd.FileName, true);
                        MessageBox.Show("Report PDF saved successfully.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrintReportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tempPdfPath) || !File.Exists(_tempPdfPath)) return;

            try
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    Verb = "print",
                    FileName = _tempPdfPath,
                    UseShellExecute = true
                };
                p.Start();
                MessageBox.Show("Print command sent successfully.", "Printing", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error printing report PDF");
                MessageBox.Show("Error printing report: " + ex.Message, "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
