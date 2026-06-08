using System;
using System.IO;
using System.Data.SQLite;

class Program
{
    static void Main()
    {
        string dbPath = @"LabSystem.UI\bin\Debug\net462\lab.db";
        string connStr = $"Data Source={dbPath};Version=3;";
        using (var conn = new SQLiteConnection(connStr))
        {
            conn.Open();
            var cmd = new SQLiteCommand("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='Staff';", conn);
            var result = cmd.ExecuteScalar();
            Console.WriteLine("Staff table count: " + result);

            if (Convert.ToInt32(result) == 0)
            {
                string initSql = File.ReadAllText(@"LabSystem.Data\Migrations\V1__init.sql");
                new SQLiteCommand(initSql, conn).ExecuteNonQuery();
                Console.WriteLine("Init script executed.");
                
                string seedSql = File.ReadAllText(@"seed.sql");
                new SQLiteCommand(seedSql, conn).ExecuteNonQuery();
                Console.WriteLine("Seed script executed.");
            }
        }
    }
}
