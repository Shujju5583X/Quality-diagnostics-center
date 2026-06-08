using System;
using System.IO;
using LabSystem.Core.Interfaces;

namespace LabSystem.Services
{
    public class SqliteBackupService : IBackupService
    {
        public void BackupNow()
        {
            string sourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lab.db");
            if (File.Exists(sourceFile))
            {
                string backupsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                Directory.CreateDirectory(backupsDir);
                
                string filename = $"lab_backup_{DateTime.Now:yyyy-MM-dd_HHmm}.db";
                string destFile = Path.Combine(backupsDir, filename);
                
                File.Copy(sourceFile, destFile, true);
            }
        }
    }
}
