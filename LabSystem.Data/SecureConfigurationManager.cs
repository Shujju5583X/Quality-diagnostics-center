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
        private static readonly byte[] Entropy = { 11, 23, 58, 13, 21, 34, 55, 89 };

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
            string password = GetOrCreateEncryptionPassword();

            // Perform in-place database encryption if it is currently unencrypted
            EnsureDatabaseIsEncrypted(dbPath, password);
            
            return $"Data Source={dbPath};Version=3;Password={password};";
        }

        private static string GetOrCreateEncryptionPassword()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string keyPath = Path.Combine(baseDir, "key.dat");

            if (File.Exists(keyPath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(keyPath);
                    byte[] decryptedData = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.LocalMachine);
                    return Encoding.UTF8.GetString(decryptedData);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Failed to decrypt local key.dat file.");
                }
            }

            // Generate a new secure random password
            string newPassword;
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[32];
                rng.GetBytes(randomBytes);
                newPassword = Convert.ToBase64String(randomBytes);
            }

            try
            {
                byte[] plaintextBytes = Encoding.UTF8.GetBytes(newPassword);
                byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, Entropy, DataProtectionScope.LocalMachine);
                File.WriteAllBytes(keyPath, encryptedBytes);
                Serilog.Log.Information("Generated and saved new DPAPI-encrypted database key.");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to write DPAPI-encrypted key.dat.");
            }

            return newPassword;
        }

        private static void EnsureDatabaseIsEncrypted(string dbPath, string password)
        {
            if (!File.Exists(dbPath)) return;

            // Step 1: Try opening WITH password — already encrypted, nothing to do
            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;Password={password};"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT 1;";
                        cmd.ExecuteScalar();
                    }
                }
                return; // Already encrypted, all good
            }
            catch
            {
                // Not encrypted with our password (or corrupted) — continue
            }

            // Step 2: Try opening WITHOUT password — unencrypted, needs encryption
            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT 1;";
                        cmd.ExecuteScalar();
                    }
                }

                // It's a valid unencrypted database — now encrypt it
                try
                {
                    using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                    {
                        conn.Open();
                        conn.ChangePassword(password);
                    }
                    Serilog.Log.Information("Successfully encrypted existing unencrypted SQLite database.");
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "Could not encrypt database this time; will retry next launch.");
                }
            }
            catch
            {
                // File exists but can't be opened at all — it's corrupted.
                // Delete it so DatabaseInitializer will recreate it fresh.
                try
                {
                    File.Delete(dbPath);
                    Serilog.Log.Warning("Deleted corrupted lab.db — will be recreated on next launch.");
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Failed to delete corrupted lab.db.");
                }
            }
        }
    }
}
