using System;
using System.IO;
using System.Windows;
using Serilog;

namespace LabSystem.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                var crashFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log");
                File.AppendAllText(crashFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " MAINWINDOW XAML PARSE ERROR: " + ex + "\r\n");
                Log.Fatal(ex, "Failed to parse MainWindow XAML.");
                throw;
            }

            this.Loaded += (s, e) =>
            {
                Log.Information("MainWindow Loaded.");
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " MainWindow Loaded OK\r\n");
            };

            this.ContentRendered += (s, e) =>
            {
                Log.Information("MainWindow ContentRendered.");
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " MainWindow ContentRendered OK\r\n");
            };

        }
    }
}
