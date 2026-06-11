using System;
using System.Diagnostics;
using System.Windows;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
namespace LabSystem.UI.Views
{
    public partial class PdfPreviewWindow : Window
    {
        private readonly TestOrder _order;
        private readonly IPdfReportService _reportService;
        private string _tempPdfPath;

        public PdfPreviewWindow(TestOrder order, IPdfReportService reportService)
        {
            InitializeComponent();
            _order = order;
            _reportService = reportService;
            
            Loaded += PdfPreviewWindow_Loaded;
        }

        private async void PdfPreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Generate temporary PDF with letterhead
                _tempPdfPath = await _reportService.GenerateReportAsync(_order, includeLetterhead: true);
                
                // Display in WebBrowser
                PdfWebViewer.Navigate(new Uri(_tempPdfPath).AbsoluteUri);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // The PDF is already saved in the Reports folder by GenerateReportAsync
                MessageBox.Show($"PDF Report generated successfully!\nSaved to: {_tempPdfPath}", "Report Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PrintPdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Do you want to print WITH the letterhead background?\n\nSelect 'Yes' for blank paper, 'No' for pre-printed letterhead.", 
                "Print Options", 
                MessageBoxButton.YesNoCancel, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            string printPath = _tempPdfPath;

            try
            {
                if (result == MessageBoxResult.No)
                {
                    // Generate new PDF without letterhead for printing
                    printPath = await _reportService.GenerateReportAsync(_order, includeLetterhead: false);
                }

                // Print silently using the default system print verb
                var p = new Process();
                p.StartInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    Verb = "print",
                    FileName = printPath,
                    UseShellExecute = true
                };
                p.Start();
                
                MessageBox.Show("Print command sent successfully.", "Printing", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing PDF: {ex.Message}\nMake sure a default PDF viewer is installed and configured for printing.", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
