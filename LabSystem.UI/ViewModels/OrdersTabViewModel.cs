using LabSystem.Core.Interfaces;

namespace LabSystem.UI.ViewModels
{
    public class OrdersTabViewModel
    {
        public ITestOrderRepository OrderRepo { get; }
        public IOrderService OrderService { get; }
        public IPdfReportService ReportService { get; }

        public OrdersTabViewModel(ITestOrderRepository orderRepo, IOrderService orderService, IPdfReportService reportService)
        {
            OrderRepo = orderRepo;
            OrderService = orderService;
            ReportService = reportService;
        }
    }
}
