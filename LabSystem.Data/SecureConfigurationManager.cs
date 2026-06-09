using System;
using System.IO;

namespace LabSystem.Data
{
    /// <summary>
    /// A secure configuration manager that dynamically constructs the connection string 
    /// at runtime to avoid exposing it in plaintext within the application's configuration file.
    /// </summary>
    public static class SecureConfigurationManager
    {
        public static string GetLabDbConnectionString()
        {
            // Retrieve the DataDirectory path (set during application startup)
            string dataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString();
            
            // Fallback to the base directory if not explicitly set
            if (string.IsNullOrEmpty(dataDirectory))
            {
                dataDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            string dbPath = Path.Combine(dataDirectory, "lab.db");
            
            return $"Data Source={dbPath};Version=3;";
        }
    }
}
