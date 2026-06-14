using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SQLite;
using LabSystem.Core.Models;
using LabSystem.Data;
using LabSystem.Data.Repositories;
using LabSystem.Services;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class SmsServiceTests
    {
        private SQLiteConnection _connection;
        private LabDbContext _context;
        private SmsService _service;
        private Repository<SmsLog> _logRepo;

        [SetUp]
        public void SetUp()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _connection.Open();

            _context = new LabDbContext(_connection);

            var initSqlPath = TestHelper.FindFileUpwards("LabSystem.Data", "Migrations", "V1__init.sql");
            if (initSqlPath == null || !File.Exists(initSqlPath))
            {
                throw new FileNotFoundException("Could not find V1__init.sql for SQLite setup.");
            }
            string sql = File.ReadAllText(initSqlPath);
            _context.Database.ExecuteSqlCommand(sql);

            // Create SmsLogs table
            _context.Database.ExecuteSqlCommand(@"
                CREATE TABLE IF NOT EXISTS SmsLogs (
                    SmsLogId INTEGER PRIMARY KEY AUTOINCREMENT,
                    PatientId INTEGER,
                    PhoneNumber TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    Status TEXT NOT NULL DEFAULT 'Pending',
                    GatewayResponse TEXT,
                    SentAt DATETIME NOT NULL,
                    FOREIGN KEY(PatientId) REFERENCES Patients(PatientId)
                );
            ");

            _logRepo = new Repository<SmsLog>(_context);
            _service = new SmsService("", "", _logRepo);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        [Test]
        public async Task SendSms_WithEmptyPhone_ReturnsFalseAndLogsFailure()
        {
            var result = await _service.SendSmsAsync("", "Test message");
            Assert.IsFalse(result);

            var logs = await _service.GetSmsLogAsync();
            Assert.AreEqual(1, logs.Count());
            Assert.AreEqual("Failed", logs.First().Status);
        }

        [Test]
        public async Task SendSms_WithMockConfig_ReturnsTrueAndLogsSent()
        {
            var result = await _service.SendSmsAsync("+911234567890", "Test SMS from QDC");
            Assert.IsTrue(result);

            var logs = await _service.GetSmsLogAsync();
            Assert.AreEqual(1, logs.Count());
            Assert.AreEqual("Sent", logs.First().Status);
            Assert.AreEqual("+911234567890", logs.First().PhoneNumber);
            Assert.AreEqual("Test SMS from QDC", logs.First().Message);
        }

        [Test]
        public async Task GetSmsLog_WithPatientFilter_ReturnsOnlyMatching()
        {
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var log1 = new SmsLog { PatientId = patient.PatientId, PhoneNumber = "+911", Message = "Msg 1", Status = "Sent", SentAt = DateTime.UtcNow };
            var log2 = new SmsLog { PhoneNumber = "+912", Message = "Msg 2", Status = "Sent", SentAt = DateTime.UtcNow };
            _context.SmsLogs.Add(log1);
            _context.SmsLogs.Add(log2);
            await _context.SaveChangesAsync();

            var patientLogs = await _service.GetSmsLogAsync(patient.PatientId);
            Assert.AreEqual(1, patientLogs.Count());
        }

        [Test]
        public async Task SendSms_NullPhone_ReturnsFalse()
        {
            var result = await _service.SendSmsAsync(null, "Test");
            Assert.IsFalse(result);
        }

        [Test]
        public void AppointmentReminderTemplate_FormatsCorrectly()
        {
            var date = new DateTime(2026, 7, 15, 10, 30, 0);
            var msg = SmsTemplates.AppointmentReminder("John", date);
            Assert.That(msg, Does.Contain("John"));
            Assert.That(msg, Does.Contain("15-Jul-2026"));
            Assert.That(msg, Does.Contain("Quality Diagnostics Center"));
        }

        [Test]
        public void ResultReadyTemplate_ContainsPatientName()
        {
            var msg = SmsTemplates.ResultReady("Jane");
            Assert.That(msg, Does.Contain("Jane"));
        }

        [Test]
        public void PaymentDueTemplate_ContainsAmount()
        {
            var msg = SmsTemplates.PaymentDue("John", 1250.50m);
            Assert.That(msg, Does.Contain("₹1,250.50"));
            Assert.That(msg, Does.Contain("Quality Diagnostics Center"));
        }
    }
}