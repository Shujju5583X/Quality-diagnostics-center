using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.UI.ViewModels
{
    public class LabTabViewModel
    {
        public IResultRepository ResultRepo { get; }
        public IRepository<TestType> TestTypeRepo { get; }
        public IResultService ResultService { get; }

        public LabTabViewModel(IResultRepository resultRepo, IRepository<TestType> testTypeRepo, IResultService resultService)
        {
            ResultRepo = resultRepo;
            TestTypeRepo = testTypeRepo;
            ResultService = resultService;
        }
    }
}
