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
    public class AppointmentServiceTests
    {
        private SQLiteConnection _connection;
        private LabDbContext _context;
        private AppointmentService _service;
        private AppointmentRepository _repo;

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

            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Patients ADD COLUMN BranchId INTEGER DEFAULT 1;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE Staff ADD COLUMN BranchId INTEGER DEFAULT 1;"); } catch { }
            try { _context.Database.ExecuteSqlCommand("ALTER TABLE TestOrders ADD COLUMN BranchId INTEGER DEFAULT 1;"); } catch { }

            // Create Appointments table
            _context.Database.ExecuteSqlCommand(@"
                CREATE TABLE IF NOT EXISTS Appointments (
                    AppointmentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    PatientId INTEGER NOT NULL,
                    AppointmentDate DATETIME NOT NULL,
                    DurationMinutes INTEGER NOT NULL DEFAULT 15,
                    Purpose TEXT,
                    Status TEXT NOT NULL DEFAULT 'Scheduled',
                    Notes TEXT,
                    CreatedAt DATETIME,
                    UpdatedAt DATETIME,
                    FOREIGN KEY(PatientId) REFERENCES Patients(PatientId)
                );
            ");

            _repo = new AppointmentRepository(_context);
            _service = new AppointmentService(_repo);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        [Test]
        public async Task BookAppointment_WithOverlappingSlot_ThrowsInvalidOperation()
        {
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var baseDate = DateTime.UtcNow.AddDays(30).Date;
            var appointment1 = new Appointment
            {
                PatientId = patient.PatientId,
                AppointmentDate = baseDate.AddHours(10),
                DurationMinutes = 30,
                Purpose = "Checkup",
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Appointments.Add(appointment1);
            await _context.SaveChangesAsync();

            // Verify repo can find it
            var found = await _repo.GetByDateAsync(baseDate);
            Assert.AreEqual(1, found.Count(), "Repo should find the saved appointment");

            var appointment2 = new Appointment
            {
                PatientId = patient.PatientId,
                AppointmentDate = baseDate.AddHours(10).AddMinutes(15),
                DurationMinutes = 30,
                Purpose = "Follow-up"
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _service.BookAppointmentAsync(appointment2));
            Assert.That(ex.Message, Does.Contain("already booked"));
        }

        [Test]
        public async Task BookAppointment_WithNonOverlappingSlot_Works()
        {
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                AppointmentDate = DateTime.UtcNow.AddDays(1).Date.AddHours(14),
                DurationMinutes = 30,
                Purpose = "Routine Checkup"
            };

            var result = await _service.BookAppointmentAsync(appointment);

            Assert.IsNotNull(result);
            Assert.AreEqual("Scheduled", result.Status);
            Assert.Greater(result.AppointmentId, 0);
        }

        [Test]
        public async Task CancelAppointment_SetsStatusCancelled()
        {
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                AppointmentDate = DateTime.UtcNow.AddDays(2),
                DurationMinutes = 15,
                Purpose = "Consultation",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            await _service.CancelAppointmentAsync(appointment.AppointmentId);

            var cancelled = await _repo.GetByIdAsync(appointment.AppointmentId);
            Assert.AreEqual("Cancelled", cancelled.Status);
        }

        [Test]
        public async Task MarkNoShow_SetsStatusNoShow()
        {
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                DurationMinutes = 15,
                Purpose = "Checkup",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            await _service.MarkNoShowAsync(appointment.AppointmentId);

            var noshow = await _repo.GetByIdAsync(appointment.AppointmentId);
            Assert.AreEqual("NoShow", noshow.Status);
        }

        [Test]
        public async Task GetAppointmentsByDateRange_ReturnsCorrectCount()
        {
            var patient = new Patient { FullName = "Test Patient", CreatedAt = DateTime.UtcNow };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var app1 = new Appointment { PatientId = patient.PatientId, AppointmentDate = DateTime.UtcNow.AddDays(1), DurationMinutes = 15, Purpose = "A", Status = "Scheduled", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var app2 = new Appointment { PatientId = patient.PatientId, AppointmentDate = DateTime.UtcNow.AddDays(2), DurationMinutes = 15, Purpose = "B", Status = "Scheduled", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var app3 = new Appointment { PatientId = patient.PatientId, AppointmentDate = DateTime.UtcNow.AddDays(5), DurationMinutes = 15, Purpose = "C", Status = "Scheduled", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.Appointments.Add(app1);
            _context.Appointments.Add(app2);
            _context.Appointments.Add(app3);
            await _context.SaveChangesAsync();

            var results = await _service.GetAppointmentsByDateAsync(DateTime.UtcNow.AddDays(1));
            Assert.AreEqual(1, results.Count());
        }
    }
}