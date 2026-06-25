using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.Services
{
    public class CsvBackupService : ICsvBackupService
    {
        private readonly IRepository<Setting> _settingRepo;
        private readonly IPatientRepository _patientRepo;
        private readonly IRepository<Doctor> _doctorRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<TestType> _testTypeRepo;
        private readonly ITestOrderRepository _orderRepo;
        private readonly IResultRepository _resultRepo;
        private readonly IBillingService _billingService;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IRepository<DoctorCommission> _commissionRepo;

        public CsvBackupService(
            IRepository<Setting> settingRepo,
            IPatientRepository patientRepo,
            IRepository<Doctor> doctorRepo,
            IRepository<Department> departmentRepo,
            IRepository<TestType> testTypeRepo,
            ITestOrderRepository orderRepo,
            IResultRepository resultRepo,
            IBillingService billingService,
            IPaymentRepository paymentRepo,
            IRepository<DoctorCommission> commissionRepo)
        {
            _settingRepo = settingRepo;
            _patientRepo = patientRepo;
            _doctorRepo = doctorRepo;
            _departmentRepo = departmentRepo;
            _testTypeRepo = testTypeRepo;
            _orderRepo = orderRepo;
            _resultRepo = resultRepo;
            _billingService = billingService;
            _paymentRepo = paymentRepo;
            _commissionRepo = commissionRepo;
        }

        public async Task ExportToCsvAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== SETTINGS ===");
            sb.AppendLine("Key,Value");
            var settings = await _settingRepo.GetAllAsync(cancellationToken);
            foreach (var s in settings)
            {
                sb.AppendLine(EscapeCsv(s.Key) + "," + EscapeCsv(s.Value));
            }
            sb.AppendLine();

            sb.AppendLine("=== PATIENTS ===");
            sb.AppendLine("PatientId,Uhid,FullName,Gender,Age,ContactPhone,ContactEmail,CreatedAt,Title,AgeYears,AgeMonths,AgeDays");
            var patients = await _patientRepo.GetAllAsync(cancellationToken);
            foreach (var p in patients)
            {
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", 
                    p.PatientId, 
                    EscapeCsv(p.Uhid), 
                    EscapeCsv(p.FullName), 
                    EscapeCsv(p.Gender), 
                    p.Age, 
                    EscapeCsv(p.ContactPhone), 
                    EscapeCsv(p.ContactEmail), 
                    p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    EscapeCsv(p.Title),
                    p.AgeYears,
                    p.AgeMonths,
                    p.AgeDays));
            }
            sb.AppendLine();

            sb.AppendLine("=== DOCTORS ===");
            sb.AppendLine("DoctorId,FullName,ContactPhone,Commission,CreatedAt");
            var doctors = await _doctorRepo.GetAllAsync(cancellationToken);
            foreach (var d in doctors)
            {
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4}", d.DoctorId, EscapeCsv(d.FullName), EscapeCsv(d.ContactPhone), d.Commission, d.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            sb.AppendLine();

            sb.AppendLine("=== DEPARTMENTS ===");
            sb.AppendLine("DepartmentId,Name");
            var depts = await _departmentRepo.GetAllAsync(cancellationToken);
            foreach (var dept in depts)
            {
                sb.AppendLine(dept.DepartmentId + "," + EscapeCsv(dept.Name));
            }
            sb.AppendLine();

            sb.AppendLine("=== TEST CATALOG ===");
            sb.AppendLine("TypeId,Name,Unit,Price,SampleType,Category,IsActive");
            var testTypes = await _testTypeRepo.GetAllAsync(cancellationToken);
            foreach (var t in testTypes)
            {
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6}", t.TypeId, EscapeCsv(t.Name), EscapeCsv(t.Unit), t.Price, EscapeCsv(t.SampleType), EscapeCsv(t.Category), t.IsActive ? 1 : 0));
            }
            sb.AppendLine();

            sb.AppendLine("=== ORDERS ===");
            sb.AppendLine("OrderId,PatientId,DoctorId,ReferredBy,Status,OrderedAt");
            var orders = await _orderRepo.GetAllAsync(cancellationToken);
            foreach (var o in orders)
            {
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5}", o.OrderId, o.PatientId, o.DoctorId, EscapeCsv(o.ReferredBy), EscapeCsv(o.Status), o.OrderedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            sb.AppendLine();

            sb.AppendLine("=== RESULTS ===");
            sb.AppendLine("ResultId,OrderId,TypeId,Value,ValueText,IsAbnormal,IsAmended,RecordedAt");
            var results = await _resultRepo.GetAllAsync(cancellationToken);
            foreach (var r in results)
            {
                string valStr = r.Value.HasValue ? r.Value.Value.ToString() : "NULL";
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", r.ResultId, r.OrderId, r.TypeId, valStr, EscapeCsv(r.ValueText), r.IsAbnormal ? 1 : 0, r.IsAmended ? 1 : 0, r.RecordedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            sb.AppendLine();

            sb.AppendLine("=== INVOICES ===");
            sb.AppendLine("InvoiceId,OrderId,TotalAmount,DiscountAmount,TaxAmount,AmountPaid,PaymentMethod,IsPaid,CreatedAt");
            var invoices = await _billingService.GetAllInvoicesAsync();
            foreach (var i in invoices)
            {
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", i.InvoiceId, i.OrderId, i.TotalAmount, i.DiscountAmount, i.TaxAmount, i.AmountPaid, EscapeCsv(i.PaymentMethod), i.IsPaid ? 1 : 0, i.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            sb.AppendLine();

            sb.AppendLine("=== PAYMENTS ===");
            sb.AppendLine("PaymentId,InvoiceId,Amount,PaymentMethod,PaymentDate");
            var payments = await _paymentRepo.GetAllAsync(cancellationToken);
            foreach (var p in payments)
            {
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4}", p.PaymentId, p.InvoiceId, p.Amount, EscapeCsv(p.PaymentMethod), p.PaymentDate.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            sb.AppendLine();

            sb.AppendLine("=== DOCTOR COMMISSIONS ===");
            sb.AppendLine("CommissionId,DoctorId,InvoiceId,CommissionAmount,Status,CreatedAt");
            var commissions = await _commissionRepo.GetAllAsync(cancellationToken);
            foreach (var c in commissions)
            {
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4},{5}", c.CommissionId, c.DoctorId, c.InvoiceId, c.CommissionAmount, EscapeCsv(c.Status), c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            string nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var settingsList = (await _settingRepo.GetAllAsync(cancellationToken)).ToList();
            var lastBackupSetting = settingsList.FirstOrDefault(s => s.Key == "last_backup");
            if (lastBackupSetting == null)
            {
                lastBackupSetting = new Setting { Key = "last_backup", Value = nowStr };
                await _settingRepo.AddAsync(lastBackupSetting, cancellationToken);
            }
            else
            {
                lastBackupSetting.Value = nowStr;
                await _settingRepo.UpdateAsync(lastBackupSetting, cancellationToken);
            }

            Log.Information("CSV Backup exported to {Path}", filePath);
        }

        public async Task ImportFromCsvAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            string content = File.ReadAllText(filePath);
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            using (var connection = new System.Data.SQLite.SQLiteConnection(LabSystem.Data.SecureConfigurationManager.GetLabDbConnectionString()))
            {
                await connection.OpenAsync(cancellationToken);

                using (var pragmaCmd = new System.Data.SQLite.SQLiteCommand("PRAGMA foreign_keys = OFF", connection))
                {
                    await pragmaCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string[] tables = new[] { "Results", "Payments", "DoctorCommissions", "Reports", "Invoices", "TestOrders", "TestTypes", "Departments", "Doctors", "Patients", "Settings" };
                        foreach (var table in tables)
                        {
                            using (var cmd = new System.Data.SQLite.SQLiteCommand("DELETE FROM " + table, connection, transaction))
                            {
                                await cmd.ExecuteNonQueryAsync(cancellationToken);
                            }
                        }

                        string currentSection = null;
                        bool skipNext = false;

                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            if (line.StartsWith("==="))
                            {
                                currentSection = line.Trim();
                                skipNext = true;
                                continue;
                            }
                            if (skipNext)
                            {
                                skipNext = false;
                                continue;
                            }

                            var cols = ParseCsvLine(line);
                            if (currentSection == "=== SETTINGS ===")
                            {
                                using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Settings ([Key], [Value]) VALUES (@p0, @p1)", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@p0", cols[0]);
                                    cmd.Parameters.AddWithValue("@p1", cols[1]);
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                            else if (currentSection == "=== PATIENTS ===")
                            {
                                int age = 0;
                                DateTime? dob = null;
                                string title = null;
                                int? ageYears = null;
                                int? ageMonths = null;
                                int? ageDays = null;

                                if (cols.Count > 8)
                                {
                                    title = string.IsNullOrWhiteSpace(cols[8]) ? null : cols[8];
                                    int y;
                                    if (int.TryParse(cols[9], out y)) ageYears = y;
                                    int m;
                                    if (int.TryParse(cols[10], out m)) ageMonths = m;
                                    int d;
                                    if (int.TryParse(cols[11], out d)) ageDays = d;

                                    dob = DateTime.Today.AddYears(-(ageYears ?? 0)).AddMonths(-(ageMonths ?? 0)).AddDays(-(ageDays ?? 0));
                                }
                                else if (cols.Count > 4 && int.TryParse(cols[4], out age))
                                {
                                    dob = DateTime.Today.AddYears(-age);
                                    ageYears = age;
                                }
                                using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Patients (PatientId, Uhid, FullName, Gender, DateOfBirth, ContactPhone, ContactEmail, CreatedAt, Title, AgeYears, AgeMonths, AgeDays) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11)", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@p0", cols[0]);
                                    cmd.Parameters.AddWithValue("@p1", cols[1]);
                                    cmd.Parameters.AddWithValue("@p2", cols[2]);
                                    cmd.Parameters.AddWithValue("@p3", cols[3]);
                                    cmd.Parameters.AddWithValue("@p4", dob.HasValue ? (object)dob.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@p5", cols[5]);
                                    cmd.Parameters.AddWithValue("@p6", cols[6]);
                                    cmd.Parameters.AddWithValue("@p7", cols[7]);
                                    cmd.Parameters.AddWithValue("@p8", (object)title ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@p9", (object)ageYears ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@p10", (object)ageMonths ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@p11", (object)ageDays ?? DBNull.Value);
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                            else if (currentSection == "=== DOCTORS ===")
                            {
                                using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Doctors (DoctorId, FullName, ContactPhone, Commission, CreatedAt) VALUES (@p0, @p1, @p2, @p3, @p4)", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@p0", cols[0]);
                                    cmd.Parameters.AddWithValue("@p1", cols[1]);
                                    cmd.Parameters.AddWithValue("@p2", cols[2]);
                                    cmd.Parameters.AddWithValue("@p3", string.IsNullOrEmpty(cols[3]) ? DBNull.Value : (object)cols[3]);
                                    cmd.Parameters.AddWithValue("@p4", cols[4]);
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                            else if (currentSection == "=== DEPARTMENTS ===")
                            {
                                using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Departments (DepartmentId, Name) VALUES (@p0, @p1)", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@p0", cols[0]);
                                    cmd.Parameters.AddWithValue("@p1", cols[1]);
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                            else if (currentSection == "=== TEST CATALOG ===")
                            {
                                using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO TestTypes (TypeId, Name, Unit, Price, SampleType, Category, IsActive) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@p0", cols[0]);
                                    cmd.Parameters.AddWithValue("@p1", cols[1]);
                                    cmd.Parameters.AddWithValue("@p2", cols[2]);
                                    cmd.Parameters.AddWithValue("@p3", string.IsNullOrEmpty(cols[3]) ? DBNull.Value : (object)cols[3]);
                                    cmd.Parameters.AddWithValue("@p4", cols[4]);
                                    cmd.Parameters.AddWithValue("@p5", cols[5]);
                                    cmd.Parameters.AddWithValue("@p6", cols[6]);
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                            else if (currentSection == "=== ORDERS ===")
                            {
                                using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO TestOrders (OrderId, PatientId, DoctorId, ReferredBy, Status, OrderedAt, CreatedAt, UpdatedAt) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p5, @p5)", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@p0", cols[0]);
                                    cmd.Parameters.AddWithValue("@p1", cols[1]);
                                    cmd.Parameters.AddWithValue("@p2", string.IsNullOrEmpty(cols[2]) ? DBNull.Value : (object)cols[2]);
                                    cmd.Parameters.AddWithValue("@p3", cols[3]);
                                    cmd.Parameters.AddWithValue("@p4", cols[4]);
                                    cmd.Parameters.AddWithValue("@p5", cols[5]);
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                            else if (currentSection == "=== RESULTS ===")
                            {
                                using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Results (ResultId, OrderId, TypeId, Value, ValueText, IsAbnormal, IsAmended, RecordedAt, TechnicianId) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8)", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@p0", cols[0]);
                                    cmd.Parameters.AddWithValue("@p1", cols[1]);
                                    cmd.Parameters.AddWithValue("@p2", cols[2]);
                                    cmd.Parameters.AddWithValue("@p3", cols[3] == "NULL" ? DBNull.Value : (object)cols[3]);
                                    cmd.Parameters.AddWithValue("@p4", cols[4]);
                                    cmd.Parameters.AddWithValue("@p5", cols[5]);
                                    cmd.Parameters.AddWithValue("@p6", cols[6]);
                                    cmd.Parameters.AddWithValue("@p7", cols[7]);
                                    cmd.Parameters.AddWithValue("@p8", 1);
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                            else if (currentSection == "=== INVOICES ===")
                            {
                                bool isPaid = cols[7] == "1";
                                decimal total = 0; decimal.TryParse(cols[2], out total);
                                decimal paid = 0; decimal.TryParse(cols[5], out paid);
                                string status = "Pending";
                                if (isPaid || paid >= total) status = "Paid";
                                else if (paid > 0) status = "Partial";

                                DateTime? paidAt = null;
                                DateTime parsedPaidAt;
                                if (isPaid && cols.Count > 8 && !string.IsNullOrEmpty(cols[8]))
                                {
                                    DateTime.TryParse(cols[8], out parsedPaidAt);
                                    paidAt = parsedPaidAt;
                                }

                                using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Invoices (InvoiceId, OrderId, TotalAmount, DiscountAmount, TaxAmount, AmountPaid, PaymentMethod, IsPaid, CreatedAt, Status, PaidAt, UpdatedAt) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p8)", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@p0", cols[0]);
                                    cmd.Parameters.AddWithValue("@p1", cols[1]);
                                    cmd.Parameters.AddWithValue("@p2", string.IsNullOrEmpty(cols[2]) ? DBNull.Value : (object)cols[2]);
                                    cmd.Parameters.AddWithValue("@p3", string.IsNullOrEmpty(cols[3]) ? DBNull.Value : (object)cols[3]);
                                    cmd.Parameters.AddWithValue("@p4", string.IsNullOrEmpty(cols[4]) ? DBNull.Value : (object)cols[4]);
                                    cmd.Parameters.AddWithValue("@p5", string.IsNullOrEmpty(cols[5]) ? DBNull.Value : (object)cols[5]);
                                    cmd.Parameters.AddWithValue("@p6", cols[6]);
                                    cmd.Parameters.AddWithValue("@p7", cols[7]);
                                    cmd.Parameters.AddWithValue("@p8", cols[8]);
                                    cmd.Parameters.AddWithValue("@p9", status);
                                    cmd.Parameters.AddWithValue("@p10", paidAt.HasValue ? (object)paidAt.Value : DBNull.Value);
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                            else if (currentSection == "=== PAYMENTS ===")
                            {
                                using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO Payments (PaymentId, InvoiceId, Amount, PaymentMethod, PaymentDate) VALUES (@p0, @p1, @p2, @p3, @p4)", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@p0", cols[0]);
                                    cmd.Parameters.AddWithValue("@p1", cols[1]);
                                    cmd.Parameters.AddWithValue("@p2", string.IsNullOrEmpty(cols[2]) ? DBNull.Value : (object)cols[2]);
                                    cmd.Parameters.AddWithValue("@p3", cols[3]);
                                    cmd.Parameters.AddWithValue("@p4", cols[4]);
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                            else if (currentSection == "=== DOCTOR COMMISSIONS ===")
                            {
                                using (var cmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO DoctorCommissions (CommissionId, DoctorId, InvoiceId, CommissionAmount, Status, CreatedAt) VALUES (@p0, @p1, @p2, @p3, @p4, @p5)", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@p0", cols[0]);
                                    cmd.Parameters.AddWithValue("@p1", cols[1]);
                                    cmd.Parameters.AddWithValue("@p2", cols[2]);
                                    cmd.Parameters.AddWithValue("@p3", string.IsNullOrEmpty(cols[3]) ? DBNull.Value : (object)cols[3]);
                                    cmd.Parameters.AddWithValue("@p4", cols[4]);
                                    cmd.Parameters.AddWithValue("@p5", cols[5]);
                                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                                }
                            }
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex, "CSV restore failed during import.");
                        throw;
                    }
                    finally
                    {
                        using (var pragmaCmd = new System.Data.SQLite.SQLiteCommand("PRAGMA foreign_keys = ON", connection))
                        {
                            pragmaCmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            Log.Information("CSV Backup restored from {Path}", filePath);
        }

        private static string EscapeCsv(string value)
        {
            if (value == null) return string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        current.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result;
        }
    }
}
