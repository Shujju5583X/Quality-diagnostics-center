using System.Collections.Generic;
using System.Linq;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class PatientRepository : Repository<Patient>, IPatientRepository
    {
        public PatientRepository(LabDbContext context) : base(context) { }

        public IEnumerable<Patient> SearchByName(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return GetAll();
            return _dbSet.Where(p => p.FullName.Contains(query)).ToList();
        }
    }
}
