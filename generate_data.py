import os

data_dir = r"E:\Quality diagnostics center\LabSystem.Data"
repositories_dir = os.path.join(data_dir, "Repositories")

os.makedirs(repositories_dir, exist_ok=True)

data_files = {
    "LabDbContext.cs": """using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using LabSystem.Core.Models;

namespace LabSystem.Data
{
    public class LabDbContext : DbContext
    {
        public LabDbContext() : base("name=LabDbContext")
        {
            // For SQLite
            Database.SetInitializer<LabDbContext>(null);
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<TestType> TestTypes { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<TestOrder> TestOrders { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // SQLite explicit configurations (e.g., auto-increment needs special handling in some cases, 
            // but EF6 SQLite provider handles primary keys conventionally).

            base.OnModelCreating(modelBuilder);
        }
    }
}
""",
    "Repositories/Repository.cs": """using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using LabSystem.Core.Interfaces;

namespace LabSystem.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly LabDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(LabDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public IEnumerable<T> GetAll()
        {
            return _dbSet.ToList();
        }

        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
                _dbSet.Remove(entity);
        }
    }
}
""",
    "Repositories/PatientRepository.cs": """using System.Collections.Generic;
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
""",
    "Repositories/TestOrderRepository.cs": """using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class TestOrderRepository : Repository<TestOrder>, ITestOrderRepository
    {
        public TestOrderRepository(LabDbContext context) : base(context) { }

        public IEnumerable<TestOrder> GetOrdersForPatient(int patientId)
        {
            return _dbSet.Include(o => o.Patient)
                         .Where(o => o.PatientId == patientId)
                         .ToList();
        }

        public IEnumerable<TestOrder> GetByStatus(string status)
        {
            return _dbSet.Include(o => o.Patient)
                         .Where(o => o.Status == status)
                         .ToList();
        }
    }
}
""",
    "Repositories/ResultRepository.cs": """using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Data.Repositories
{
    public class ResultRepository : Repository<Result>, IResultRepository
    {
        public ResultRepository(LabDbContext context) : base(context) { }

        public IEnumerable<Result> GetResultsForOrder(int orderId)
        {
            return _dbSet.Include(r => r.TestType)
                         .Include(r => r.Technician)
                         .Where(r => r.OrderId == orderId)
                         .ToList();
        }
    }
}
"""
}

for name, content in data_files.items():
    with open(os.path.join(data_dir, name), "w", encoding="utf-8") as f:
        f.write(content)

print("Data files generated.")
