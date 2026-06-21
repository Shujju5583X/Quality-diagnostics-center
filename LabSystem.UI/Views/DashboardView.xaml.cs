using System;
using System.IO;
using System.Windows.Controls;
using Serilog;
using LabSystem.Core;

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
                var crashFile = Path.Combine(FileUtilities.GetWritableDataDirectory(), "startup_crash.log");
                try { File.AppendAllText(crashFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " DASHBOARDVIEW XAML PARSE ERROR: " + ex + "\r\n"); } catch {}
                Log.Fatal(ex, "Failed to parse DashboardView XAML.");
                throw;
            }
        }
    }
}
