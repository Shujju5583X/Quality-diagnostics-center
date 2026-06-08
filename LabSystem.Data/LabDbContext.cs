using System.Data.Entity;
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

            // Explicit Table mappings
            modelBuilder.Entity<Patient>().ToTable("Patients");
            modelBuilder.Entity<TestType>().ToTable("TestTypes");
            modelBuilder.Entity<Staff>().ToTable("Staff");
            modelBuilder.Entity<TestOrder>().ToTable("TestOrders");
            modelBuilder.Entity<Result>().ToTable("Results");
            modelBuilder.Entity<Report>().ToTable("Reports");
            modelBuilder.Entity<AuditLog>().ToTable("AuditLogs");

            // SQLite explicit configurations
            modelBuilder.Entity<AuditLog>().HasKey(a => a.LogId);
            modelBuilder.Entity<TestOrder>().HasKey(o => o.OrderId);
            modelBuilder.Entity<TestType>().HasKey(t => t.TypeId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
