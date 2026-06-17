using System;
using System.Collections.Generic;
using System.Threading;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.UI.ViewModels;
using Moq;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class DashboardViewModelTests
    {
        private Mock<IPatientRepository> _mockPatientRepo;
        private Mock<ITestOrderRepository> _mockOrderRepo;
        private Mock<IOrderService> _mockOrderService;
        private Mock<IPdfReportService> _mockReportService;
        private Mock<IResultRepository> _mockResultRepo;
        private Mock<IRepository<TestType>> _mockTestTypeRepo;
        private Mock<IResultService> _mockResultService;
        private Mock<IBillingService> _mockBillingService;
        private Mock<IRepository<TestPanel>> _mockTestPanelRepo;
        private Mock<IBackupService> _mockBackupService;
        private Mock<IRepository<Doctor>> _mockDoctorRepo;
        private Mock<IRepository<Department>> _mockDepartmentRepo;
        private Mock<IRepository<Setting>> _mockSettingRepo;
        private Mock<IUnitOfWork> _mockUnitOfWork;

        [SetUp]
        public void SetUp()
        {
            _mockPatientRepo = new Mock<IPatientRepository>();
            _mockOrderRepo = new Mock<ITestOrderRepository>();
            _mockOrderService = new Mock<IOrderService>();
            _mockReportService = new Mock<IPdfReportService>();
            _mockResultRepo = new Mock<IResultRepository>();
            _mockTestTypeRepo = new Mock<IRepository<TestType>>();
            _mockResultService = new Mock<IResultService>();
            _mockBillingService = new Mock<IBillingService>();
            _mockTestPanelRepo = new Mock<IRepository<TestPanel>>();
            _mockBackupService = new Mock<IBackupService>();
            _mockDoctorRepo = new Mock<IRepository<Doctor>>();
            _mockDepartmentRepo = new Mock<IRepository<Department>>();
            _mockSettingRepo = new Mock<IRepository<Setting>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            var ct = default(CancellationToken);
            _mockSettingRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(new List<Setting>());
            _mockPatientRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(new List<Patient>());
            _mockOrderRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(new List<TestOrder>());
            _mockTestTypeRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(new List<TestType>());
            _mockBillingService.Setup(s => s.GetAllInvoicesAsync()).ReturnsAsync(new List<Invoice>());
            _mockDoctorRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(new List<Doctor>());
            _mockDepartmentRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(new List<Department>());
            _mockTestPanelRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(new List<TestPanel>());
            _mockResultRepo.Setup(r => r.CountAbnormalAsync(ct)).ReturnsAsync(0);
            _mockResultRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(new List<Result>());
        }

        [Test]
        public void Constructor_WithValidDependencies_DoesNotThrow()
        {
            DashboardViewModel vm = null;
            Assert.DoesNotThrow(() =>
            {
                vm = new DashboardViewModel(
                    _mockPatientRepo.Object,
                    _mockOrderRepo.Object,
                    _mockOrderService.Object,
                    _mockReportService.Object,
                    _mockResultRepo.Object,
                    _mockTestTypeRepo.Object,
                    _mockResultService.Object,
                    _mockBillingService.Object,
                    _mockTestPanelRepo.Object,
                    _mockBackupService.Object,
                    _mockDoctorRepo.Object,
                    _mockDepartmentRepo.Object,
                    _mockSettingRepo.Object,
                    _mockUnitOfWork.Object
                );
            });
            Assert.That(vm, Is.Not.Null);
        }

        [Test]
        public void Constructor_InitializesObservableCollections()
        {
            var vm = new DashboardViewModel(
                _mockPatientRepo.Object,
                _mockOrderRepo.Object,
                _mockOrderService.Object,
                _mockReportService.Object,
                _mockResultRepo.Object,
                _mockTestTypeRepo.Object,
                _mockResultService.Object,
                _mockBillingService.Object,
                _mockTestPanelRepo.Object,
                _mockBackupService.Object,
                _mockDoctorRepo.Object,
                _mockDepartmentRepo.Object,
                _mockSettingRepo.Object,
                _mockUnitOfWork.Object
            );

            Assert.That(vm.Patients, Is.Not.Null);
            Assert.That(vm.Orders, Is.Not.Null);
            Assert.That(vm.Invoices, Is.Not.Null);
            Assert.That(vm.Doctors, Is.Not.Null);
            Assert.That(vm.Departments, Is.Not.Null);
            Assert.That(vm.TestTypes, Is.Not.Null);
            Assert.That(vm.CatalogTestTypes, Is.Not.Null);
        }
    }
}
