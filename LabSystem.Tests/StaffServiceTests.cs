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
    public class StaffServiceTests
    {
        private SQLiteConnection _connection;
        private LabDbContext _context;
        private StaffService _service;
        private StaffRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _connection.Open();

            _context = new LabDbContext(_connection);

            TestHelper.InitializeTestDatabase(_context);

            _repo = new StaffRepository(_context);
            _service = new StaffService(_repo);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        [Test]
        public async Task CreateStaff_WithValidData_ReturnsStaff()
        {
            var staff = await _service.CreateStaffAsync("Dr. Smith", "Admin", "1234");
            Assert.IsNotNull(staff);
            Assert.AreEqual("Dr. Smith", staff.FullName);
            Assert.AreEqual("Admin", staff.Role);
            Assert.IsNotNull(staff.PinHash);
            Assert.Greater(staff.StaffId, 0);
        }

        [Test]
        public void CreateStaff_WithEmptyName_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.CreateStaffAsync("", "Technician", "1234"));
            Assert.That(ex.Message, Does.Contain("name"));
        }

        [Test]
        public void CreateStaff_WithShortPin_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.CreateStaffAsync("Test", "Technician", "12"));
            Assert.That(ex.Message, Does.Contain("PIN"));
        }

        [Test]
        public void CreateStaff_WithInvalidRole_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.CreateStaffAsync("Test", "Doctor", "1234"));
            Assert.That(ex.Message, Does.Contain("Role"));
        }

        [Test]
        public async Task ResetPin_UpdatesPinHash()
        {
            var staff = await _service.CreateStaffAsync("Jane", "Technician", "1234");
            var oldHash = staff.PinHash;

            await _service.ResetPinAsync(staff.StaffId, "5678");

            var updated = await _repo.GetByIdAsync(staff.StaffId);
            Assert.AreNotEqual(oldHash, updated.PinHash);
        }

        [Test]
        public async Task ResetPin_CanBeVerifiedWithBCrypt()
        {
            var staff = await _service.CreateStaffAsync("Bob", "Admin", "0000");
            await _service.ResetPinAsync(staff.StaffId, "9999");
            
            var updated = await _repo.GetByIdAsync(staff.StaffId);
            Assert.IsTrue(BCrypt.Net.BCrypt.Verify("9999", updated.PinHash));
        }

        [Test]
        public async Task ToggleLockout_LocksAndUnlocks()
        {
            var staff = await _service.CreateStaffAsync("John", "Technician", "1234");

            await _service.ToggleLockoutAsync(staff.StaffId, true);
            var locked = await _repo.GetByIdAsync(staff.StaffId);
            Assert.IsNotNull(locked.LockoutEnd);
            Assert.AreEqual(5, locked.FailedLoginAttempts);

            await _service.ToggleLockoutAsync(staff.StaffId, false);
            var unlocked = await _repo.GetByIdAsync(staff.StaffId);
            Assert.IsNull(unlocked.LockoutEnd);
            Assert.AreEqual(0, unlocked.FailedLoginAttempts);
        }

        [Test]
        public async Task GetAllStaff_ReturnsAllCreated()
        {
            await _service.CreateStaffAsync("A", "Admin", "1111");
            await _service.CreateStaffAsync("B", "Technician", "2222");
            await _service.CreateStaffAsync("C", "Receptionist", "3333");

            var all = await _service.GetAllStaffAsync();
            Assert.AreEqual(3, all.Count());
        }
    }
}