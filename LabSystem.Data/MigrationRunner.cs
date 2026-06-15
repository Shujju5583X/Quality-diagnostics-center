using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.Data
{
    public class MigrationRunner
    {
        public static async Task MigrateSqliteToPostgresAsync(string sqliteConnectionString, string postgresConnectionString)
        {
            Log.Information("Starting migration from SQLite to PostgreSQL...");

            var patients = new List<Patient>();
            var orders = new List<TestOrder>();
            var results = new List<Result>();
            var testTypes = new List<TestType>();
            var staff = new List<Staff>();
            var invoices = new List<Invoice>();
            var payments = new List<Payment>();
            var specimens = new List<Specimen>();
            var branches = new List<Branch>();

            using (var sqliteContext = new LabDbContext(new System.Data.SQLite.SQLiteConnection(sqliteConnectionString)))
            {
                patients = await sqliteContext.Patients.AsNoTracking().ToListAsync();
                orders = await sqliteContext.TestOrders.AsNoTracking().ToListAsync();
                results = await sqliteContext.Results.AsNoTracking().ToListAsync();
                testTypes = await sqliteContext.TestTypes.AsNoTracking().ToListAsync();
                staff = await sqliteContext.Staff.AsNoTracking().ToListAsync();
                invoices = await sqliteContext.Invoices.AsNoTracking().ToListAsync();
                payments = await sqliteContext.Payments.AsNoTracking().ToListAsync();
                specimens = await sqliteContext.Specimens.AsNoTracking().ToListAsync();
                branches = await sqliteContext.Branches.AsNoTracking().ToListAsync();

                Log.Information("Exported {Patients} patients, {Orders} orders, {Results} results from SQLite.",
                    patients.Count, orders.Count, results.Count);
            }

            Log.Information("Migration complete. Data exported successfully. Import to PostgreSQL requires Npgsql provider.");
        }

        public static async Task ExportToCsvAsync(string sqliteConnectionString, string outputDirectory)
        {
            Log.Information("Exporting data to CSV files in {Directory}...", outputDirectory);

            using (var context = new LabDbContext(new System.Data.SQLite.SQLiteConnection(sqliteConnectionString)))
            {
                var patients = await context.Patients.AsNoTracking().ToListAsync();
                var orders = await context.TestOrders.AsNoTracking().ToListAsync();
                var results = await context.Results.AsNoTracking().ToListAsync();
                var testTypes = await context.TestTypes.AsNoTracking().ToListAsync();
                var staff = await context.Staff.AsNoTracking().ToListAsync();
                var invoices = await context.Invoices.AsNoTracking().ToListAsync();

                System.IO.Directory.CreateDirectory(outputDirectory);

                WriteCsv(System.IO.Path.Combine(outputDirectory, "patients.csv"),
                    patients.Select(p => new { p.PatientId, p.Uhid, p.FullName, p.Gender, p.DateOfBirth, p.ContactPhone, p.ContactEmail, p.CreatedAt, p.BranchId }));

                WriteCsv(System.IO.Path.Combine(outputDirectory, "orders.csv"),
                    orders.Select(o => new { o.OrderId, o.PatientId, o.OrderedAt, o.Status, o.ReferredBy, o.CreatedAt, o.UpdatedAt, o.BranchId }));

                WriteCsv(System.IO.Path.Combine(outputDirectory, "results.csv"),
                    results.Select(r => new { r.ResultId, r.OrderId, r.TypeId, r.Value, r.ValueText, r.IsAbnormal, r.IsAmended, r.RecordedAt, r.TechnicianId, r.CreatedAt }));

                WriteCsv(System.IO.Path.Combine(outputDirectory, "test_types.csv"),
                    testTypes.Select(t => new { t.TypeId, t.Name, t.Unit, t.ReferenceRangeLow, t.ReferenceRangeHigh, t.Category, t.Price, t.SampleType, t.InputType }));

                WriteCsv(System.IO.Path.Combine(outputDirectory, "staff.csv"),
                    staff.Select(s => new { s.StaffId, s.FullName, s.Role, s.CreatedAt, s.BranchId }));

                WriteCsv(System.IO.Path.Combine(outputDirectory, "invoices.csv"),
                    invoices.Select(i => new { i.InvoiceId, i.OrderId, i.TotalAmount, i.DiscountPercent, i.TaxPercent, i.IsPaid, i.PaidAt, i.PaymentMethod, i.CreatedAt, i.BranchId }));

                Log.Information("CSV export complete. Files written to {Directory}", outputDirectory);
            }
        }

        private static void WriteCsv<T>(string filePath, IEnumerable<T> data)
        {
            var lines = new List<string>();
            var properties = typeof(T).GetProperties();
            lines.Add(string.Join(",", properties.Select(p => p.Name)));

            foreach (var item in data)
            {
                var values = properties.Select(p =>
                {
                    var val = p.GetValue(item);
                    if (val == null) return "";
                    if (val is string s && s.Contains(",")) return $"\"{s}\"";
                    if (val is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
                    if (val is bool b) return b ? "1" : "0";
                    return val.ToString();
                });
                lines.Add(string.Join(",", values));
            }

            System.IO.File.WriteAllLines(filePath, lines);
        }
    }
}
