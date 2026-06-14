using LabSystem.Core.Interfaces;

namespace LabSystem.UI.ViewModels
{
    public class PatientsTabViewModel
    {
        public IPatientRepository PatientRepo { get; }

        public PatientsTabViewModel(IPatientRepository patientRepo)
        {
            PatientRepo = patientRepo;
        }
    }
}
