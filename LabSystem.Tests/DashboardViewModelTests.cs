using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private Mock<IPaymentRepository> _mockPaymentRepo;
        private Mock<IRepository<DoctorCommission>> _mockCommissionRepo;
        private Mock<ICsvBackupService> _mockCsvBackupService;
        private Mock<IStaffService> _mockStaffService;
        private Mock<IStaffRepository> _mockStaffRepo;
        private Mock<IRepository<Invoice>> _mockInvoiceRepo;

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
            _mockPaymentRepo = new Mock<IPaymentRepository>();
            _mockCommissionRepo = new Mock<IRepository<DoctorCommission>>();
            _mockCsvBackupService = new Mock<ICsvBackupService>();
            _mockStaffService = new Mock<IStaffService>();
            _mockStaffRepo = new Mock<IStaffRepository>();
            _mockInvoiceRepo = new Mock<IRepository<Invoice>>();

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
            _mockPaymentRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(new List<Payment>());
            _mockCommissionRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(new List<DoctorCommission>());
            _mockPatientRepo.Setup(r => r.GetPatientsCountAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), ct)).ReturnsAsync(0);
        }

        private DashboardViewModel CreateViewModel()
        {
            return new DashboardViewModel(
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
                _mockPaymentRepo.Object,
                _mockCommissionRepo.Object,
                _mockCsvBackupService.Object,
                _mockStaffService.Object,
                _mockStaffRepo.Object,
                _mockInvoiceRepo.Object
            );
        }

        [Test]
        public void Constructor_WithValidDependencies_DoesNotThrow()
        {
            DashboardViewModel vm = null;
            Assert.DoesNotThrow(() => vm = CreateViewModel());
            Assert.That(vm, Is.Not.Null);
        }

        [Test]
        public void Constructor_InitializesObservableCollections()
        {
            var vm = CreateViewModel();

            Assert.That(vm.Patients, Is.Not.Null);
            Assert.That(vm.Orders, Is.Not.Null);
            Assert.That(vm.Invoices, Is.Not.Null);
            Assert.That(vm.Doctors, Is.Not.Null);
            Assert.That(vm.Departments, Is.Not.Null);
            Assert.That(vm.TestTypes, Is.Not.Null);
            Assert.That(vm.CatalogTestTypes, Is.Not.Null);
        }

        [Test]
        public void Constructor_InitializesDefaultValues()
        {
            var vm = CreateViewModel();

            Assert.That(vm.PatientCurrentPage, Is.EqualTo(1));
            Assert.That(vm.PatientTotalPages, Is.EqualTo(1));
            Assert.That(vm.PatientTotalCount, Is.EqualTo(0));
            Assert.That(vm.MainTabIndex, Is.EqualTo(0));
            Assert.That(vm.IsResultEditMode, Is.False);
            Assert.That(vm.DiscountAmount, Is.EqualTo(0));
            Assert.That(vm.TaxAmount, Is.EqualTo(0));
            Assert.That(vm.PaymentAmount, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_RegistersAllCommands()
        {
            var vm = CreateViewModel();

            Assert.That(vm.AddPatientCommand, Is.Not.Null);
            Assert.That(vm.CreateOrderCommand, Is.Not.Null);
            Assert.That(vm.BackupCommand, Is.Not.Null);
            Assert.That(vm.RefreshCommand, Is.Not.Null);
            Assert.That(vm.SaveCatalogTestCommand, Is.Not.Null);
            Assert.That(vm.AddCatalogTestCommand, Is.Not.Null);
            Assert.That(vm.AddPaymentCashCommand, Is.Not.Null);
            Assert.That(vm.AddPaymentUpiCommand, Is.Not.Null);
            Assert.That(vm.ApplyDiscountTaxCommand, Is.Not.Null);
            Assert.That(vm.GenerateRevenueReportCommand, Is.Not.Null);
            Assert.That(vm.RestoreBackupCommand, Is.Not.Null);
            Assert.That(vm.SaveResultsCommand, Is.Not.Null);
            Assert.That(vm.GenerateReportCommand, Is.Not.Null);
            Assert.That(vm.GenerateBillCommand, Is.Not.Null);
            Assert.That(vm.SaveSettingsCommand, Is.Not.Null);
            Assert.That(vm.SaveDoctorCommand, Is.Not.Null);
            Assert.That(vm.DeleteDoctorCommand, Is.Not.Null);
            Assert.That(vm.AddDepartmentCommand, Is.Not.Null);
            Assert.That(vm.DeleteDepartmentCommand, Is.Not.Null);
            Assert.That(vm.RenameDepartmentCommand, Is.Not.Null);
            Assert.That(vm.DeleteCatalogTestCommand, Is.Not.Null);
            Assert.That(vm.EditResultsCommand, Is.Not.Null);
            Assert.That(vm.SaveAmendmentCommand, Is.Not.Null);
            Assert.That(vm.CancelEditCommand, Is.Not.Null);
            Assert.That(vm.LoadPatientHistoryCommand, Is.Not.Null);
        }

        [Test]
        public void PendingOrdersFiltered_ReturnsOnlyPendingOrders()
        {
            var vm = CreateViewModel();
            var ct = default(CancellationToken);

            var orders = new List<TestOrder>
            {
                new TestOrder { OrderId = 1, Status = "Pending", StatusEnum = Core.Enums.OrderStatus.Pending },
                new TestOrder { OrderId = 2, Status = "Complete", StatusEnum = Core.Enums.OrderStatus.Complete },
                new TestOrder { OrderId = 3, Status = "Pending", StatusEnum = Core.Enums.OrderStatus.Pending }
            };
            _mockOrderRepo.Setup(r => r.GetAllAsync(ct)).ReturnsAsync(orders);
            _mockOrderRepo.Setup(r => r.GetCountAsync(ct)).ReturnsAsync(3);
            _mockOrderRepo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), ct)).ReturnsAsync(orders);

            vm.RefreshCommand.Execute(null);

            var pending = vm.PendingOrdersFiltered;
            Assert.That(pending.Count(), Is.EqualTo(2));
        }

        [Test]
        public void OperatorName_SetRaisesPropertyChanged()
        {
            var vm = CreateViewModel();
            bool propertyChanged = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "OperatorName")
                    propertyChanged = true;
            };

            vm.OperatorName = "Test Lab";
            Assert.That(propertyChanged, Is.True);
            Assert.That(vm.OperatorName, Is.EqualTo("Test Lab"));
        }

        [Test]
        public void SelectedPatient_SetRaisesPropertyChanged()
        {
            var vm = CreateViewModel();
            bool propertyChanged = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "SelectedPatient")
                    propertyChanged = true;
            };

            var patient = new Patient { PatientId = 1, FullName = "John Doe" };
            vm.SelectedPatient = patient;
            Assert.That(propertyChanged, Is.True);
            Assert.That(vm.SelectedPatient.FullName, Is.EqualTo("John Doe"));
        }

        [Test]
        public void MainTabIndex_SetRaisesPropertyChanged()
        {
            var vm = CreateViewModel();
            bool propertyChanged = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "MainTabIndex")
                    propertyChanged = true;
            };

            vm.MainTabIndex = 3;
            Assert.That(propertyChanged, Is.True);
            Assert.That(vm.MainTabIndex, Is.EqualTo(3));
        }

        [Test]
        public void IsResultEditMode_SetRaisesPropertyChanged()
        {
            var vm = CreateViewModel();
            bool propertyChanged = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsResultEditMode")
                    propertyChanged = true;
            };

            vm.IsResultEditMode = true;
            Assert.That(propertyChanged, Is.True);
            Assert.That(vm.IsResultEditMode, Is.True);
        }

        [Test]
        public void DiscountAmount_SetRaisesPropertyChanged()
        {
            var vm = CreateViewModel();
            bool propertyChanged = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "DiscountAmount")
                    propertyChanged = true;
            };

            vm.DiscountAmount = 50;
            Assert.That(propertyChanged, Is.True);
            Assert.That(vm.DiscountAmount, Is.EqualTo(50));
        }

        [Test]
        public void SelectedDoctor_SetPopulatesDoctorFields()
        {
            var vm = CreateViewModel();
            var doctor = new Doctor { DoctorId = 1, FullName = "Dr. Smith", ContactPhone = "123456", Commission = 10 };

            vm.SelectedDoctor = doctor;

            Assert.That(vm.NewDoctorName, Is.EqualTo("Dr. Smith"));
            Assert.That(vm.NewDoctorPhone, Is.EqualTo("123456"));
            Assert.That(vm.NewDoctorCommission, Is.EqualTo(10));
        }

        [Test]
        public void SelectedDoctor_SetToNullClearsDoctorFields()
        {
            var vm = CreateViewModel();
            vm.SelectedDoctor = new Doctor { FullName = "Dr. Smith" };
            vm.SelectedDoctor = null;

            Assert.That(vm.NewDoctorName, Is.EqualTo(string.Empty));
            Assert.That(vm.NewDoctorPhone, Is.EqualTo(string.Empty));
            Assert.That(vm.NewDoctorCommission, Is.EqualTo(0));
        }

        [Test]
        public void ExecuteAddPayment_WithNoSelectedInvoice_DoesNotCallService()
        {
            var vm = CreateViewModel();
            vm.SelectedInvoice = null;
            vm.PaymentAmount = 100;

            vm.AddPaymentCashCommand.Execute(null);

            _mockBillingService.Verify(s => s.AddPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExecuteAddPayment_WithPaidInvoice_DoesNotCallService()
        {
            var vm = CreateViewModel();
            vm.SelectedInvoice = new Invoice { InvoiceId = 1, IsPaid = true };
            vm.PaymentAmount = 100;

            vm.AddPaymentCashCommand.Execute(null);

            _mockBillingService.Verify(s => s.AddPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExecuteAddPayment_WithZeroAmount_DefaultsToDueAmount()
        {
            var vm = CreateViewModel();
            vm.SelectedInvoice = new Invoice { InvoiceId = 1, IsPaid = false, TotalAmount = 500, AmountPaid = 0 };
            vm.PaymentAmount = 0;

            vm.AddPaymentCashCommand.Execute(null);

            _mockBillingService.Verify(s => s.AddPaymentAsync(1, 500, "Cash"), Times.Once);
        }

        [Test]
        public void ExecuteApplyDiscountTax_WithNoSelectedInvoice_DoesNotCallService()
        {
            var vm = CreateViewModel();
            vm.SelectedInvoice = null;

            vm.ApplyDiscountTaxCommand.Execute(null);

            _mockBillingService.Verify(s => s.UpdateInvoiceFinancialsAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Never);
        }

        [Test]
        public void ExecuteApplyDiscountTax_WithPaidInvoice_DoesNotCallService()
        {
            var vm = CreateViewModel();
            vm.SelectedInvoice = new Invoice { InvoiceId = 1, IsPaid = true };

            vm.ApplyDiscountTaxCommand.Execute(null);

            _mockBillingService.Verify(s => s.UpdateInvoiceFinancialsAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Never);
        }

        [Test]
        public void ResultErrorMessage_SetRaisesPropertyChanged()
        {
            var vm = CreateViewModel();
            bool propertyChanged = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "ResultErrorMessage")
                    propertyChanged = true;
            };

            vm.ResultErrorMessage = "Error occurred";
            Assert.That(propertyChanged, Is.True);
            Assert.That(vm.ResultErrorMessage, Is.EqualTo("Error occurred"));
        }
    }
}
