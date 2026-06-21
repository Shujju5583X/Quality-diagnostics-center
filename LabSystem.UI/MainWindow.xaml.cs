using System;
using System.IO;
using System.Windows;
using Serilog;
using LabSystem.Core;

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
                var crashFile = Path.Combine(FileUtilities.GetWritableDataDirectory(), "startup_crash.log");
                try { File.AppendAllText(crashFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " MAINWINDOW XAML PARSE ERROR: " + ex + "\r\n"); } catch {}
                Log.Fatal(ex, "Failed to parse MainWindow XAML.");
                throw;
            }

            this.Loaded += (s, e) =>
            {
                Log.Information("MainWindow Loaded.");
                try
                {
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(FileUtilities.GetWritableDataDirectory(), "startup_crash.log"),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " MainWindow Loaded OK\r\n");
                }
                catch {}
            };

            this.ContentRendered += (s, e) =>
            {
                Log.Information("MainWindow ContentRendered.");
                try
                {
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(FileUtilities.GetWritableDataDirectory(), "startup_crash.log"),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " MainWindow ContentRendered OK\r\n");
                }
                catch {}
            };

        }
    }
}
