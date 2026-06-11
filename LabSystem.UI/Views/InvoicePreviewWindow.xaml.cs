using System;
using System.IO;
using System.Windows;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using Microsoft.Win32;
using Serilog;

namespace LabSystem.UI.Views
{
    public partial class InvoicePreviewWindow : Window
    {
        private readonly Invoice _invoice;
        private readonly IPdfReportService _reportService;
        private string _currentPdfPath;

        public InvoicePreviewWindow(Invoice invoice, IPdfReportService reportService)
        {
            InitializeComponent();
            _invoice = invoice;
            _reportService = reportService;
            
            this.Loaded += InvoicePreviewWindow_Loaded;
            this.Closed += InvoicePreviewWindow_Closed;
        }

        private async void InvoicePreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentPdfPath = await _reportService.GenerateInvoicePdfAsync(_invoice);
                if (!string.IsNullOrEmpty(_currentPdfPath) && File.Exists(_currentPdfPath))
                {
                    PdfWebViewer.Navigate(new Uri(_currentPdfPath));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load invoice PDF preview.");
                MessageBox.Show("Error generating invoice PDF.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void InvoicePreviewWindow_Closed(object sender, EventArgs e)
        {
            // Optional: Clean up temporary PDF
            // if (!string.IsNullOrEmpty(_currentPdfPath) && File.Exists(_currentPdfPath))
            // {
            //     try { File.Delete(_currentPdfPath); } catch { }
            // }
        }

        private void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPdfPath) || !File.Exists(_currentPdfPath)) return;

            var dlg = new SaveFileDialog
            {
                FileName = Path.GetFileName(_currentPdfPath),
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.Copy(_currentPdfPath, dlg.FileName, true);
                    MessageBox.Show("Invoice PDF saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to save invoice PDF.");
                    MessageBox.Show("Error saving PDF.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrintPdf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPdfPath) || !File.Exists(_currentPdfPath)) return;

            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _currentPdfPath,
                        Verb = "print",
                        CreateNoWindow = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to print invoice PDF.");
                MessageBox.Show("Error printing PDF. Make sure you have a default PDF viewer installed that supports printing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
