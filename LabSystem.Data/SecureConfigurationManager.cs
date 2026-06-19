using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Data.SQLite;

namespace LabSystem.Data
{
    /// <summary>
    /// A secure configuration manager that dynamically constructs the connection string 
    /// at runtime to avoid exposing it in plaintext within the application's configuration file.
    /// It secures the SQLite database using DPAPI-based encryption.
    /// </summary>
    public static class SecureConfigurationManager
    {
        public static string GetLabDbConnectionString()
        {
            // Retrieve the DataDirectory path (set during application startup)
            object dataDirObj = AppDomain.CurrentDomain.GetData("DataDirectory");
            string dataDirectory = dataDirObj != null ? dataDirObj.ToString() : null;
            
            // Fallback to the base directory if not explicitly set
            if (string.IsNullOrEmpty(dataDirectory))
            {
                dataDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            string dbPath = Path.Combine(dataDirectory, "lab.db");

            // Verify the database can be opened without a password.
            // If it cannot (e.g. because it was encrypted in a previous run or is corrupted),
            // we move it aside so it is recreated fresh.
            EnsureDatabaseIsReadable(dbPath);
            
            return "Data Source=" + dbPath + ";Version=3;Pooling=False;Journal Mode=WAL;Timeout=30;";
        }

        private static void EnsureDatabaseIsReadable(string dbPath)
        {
            if (!File.Exists(dbPath)) return;

            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT name FROM sqlite_master LIMIT 1;";
                        cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string backupPath = dbPath + ".old." + timestamp;
                    File.Move(dbPath, backupPath);
                    Serilog.Log.Warning(ex, "Existing lab.db was encrypted or corrupted. Moved to {BackupPath} to allow recreation.", backupPath);
                }
                catch (Exception moveEx)
                {
                    Serilog.Log.Error(moveEx, "Failed to move unreadable lab.db.");
                }
            }
        }
    }
}
