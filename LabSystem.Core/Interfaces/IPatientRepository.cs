using System.Collections.Generic;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface IPatientRepository : IRepository<Patient>
    {
        IEnumerable<Patient> SearchByName(string query);
    }
}
