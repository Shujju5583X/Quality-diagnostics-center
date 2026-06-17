using System;
using System.IO;
using System.Windows.Controls;
using Serilog;

namespace LabSystem.UI.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            try
            {
                InitializeComponent();
                Log.Information("DashboardView XAML loaded successfully.");
            }
            catch (Exception ex)
            {
                var crashFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log");
                File.AppendAllText(crashFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " DASHBOARDVIEW XAML PARSE ERROR: " + ex + "\r\n");
                Log.Fatal(ex, "Failed to parse DashboardView XAML.");
                throw;
            }
        }
    }
}
