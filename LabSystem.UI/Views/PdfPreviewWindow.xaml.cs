using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using Serilog;
namespace LabSystem.UI.Views
{
    public partial class PdfPreviewWindow : Window
    {
        private readonly TestOrder _order;
        private readonly IPdfReportService _reportService;
        private string _tempPdfPath;
        private bool _isLoaded = false;

        public PdfPreviewWindow(TestOrder order, IPdfReportService reportService)
        {
            InitializeComponent();
            _order = order;
            _reportService = reportService;
            
            Loaded += PdfPreviewWindow_Loaded;
        }

        private async void PdfPreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            await UpdatePreviewAsync();
        }

        private async void LetterheadComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            await UpdatePreviewAsync();
        }

        private async Task UpdatePreviewAsync()
        {
            try
            {
                bool includeLetterhead = (LetterheadComboBox.SelectedIndex == 0); // 0 = With Letterhead, 1 = Without Letterhead

                string oldPath = _tempPdfPath;

                // Generate temporary PDF
                _tempPdfPath = await _reportService.GenerateReportAsync(_order, includeLetterhead: includeLetterhead);
                
                if (oldPath != null && oldPath != _tempPdfPath && File.Exists(oldPath))
                {
                    try { File.Delete(oldPath); } catch { }
                }

                try
                {
                    // Display in WebBrowser (works on modern IE versions)
                    PdfWebViewer.Navigate(new Uri(_tempPdfPath).AbsoluteUri);
                }
                catch
                {
                    // Fallback for Windows 7: embedded IE often can't render PDFs
                    // Open in system default PDF viewer (Adobe Reader, Foxit, etc.)
                    Process.Start(_tempPdfPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating PDF preview.");
                MessageBox.Show("Error loading preview: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // The PDF is already saved in the Reports folder by GenerateReportAsync
                MessageBox.Show("PDF Report generated successfully!\nSaved to: " + _tempPdfPath, "Report Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintPdf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tempPdfPath) || !File.Exists(_tempPdfPath)) return;

            try
            {
                // Print silently using the default system print verb
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
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error printing PDF");
                MessageBox.Show("Error printing PDF: " + ex.Message + "\nMake sure a default PDF viewer is installed and configured for printing.", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
