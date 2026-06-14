using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Services;

namespace LabSystem.UI.ViewModels
{
    public class BillingTabViewModel
    {
        public IBillingService BillingService { get; }
        public IRepository<TestPanel> TestPanelRepo { get; }

        public BillingTabViewModel(IBillingService billingService, IRepository<TestPanel> testPanelRepo)
        {
            BillingService = billingService;
            TestPanelRepo = testPanelRepo;
        }
    }
}
