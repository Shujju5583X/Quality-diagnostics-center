using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Conventions;
using LabSystem.Core.Models;
using System.Data.SQLite;

namespace LabSystem.Data
{
    public class LabDbContext : DbContext
    {
        public LabDbContext() : base(new SQLiteConnection(SecureConfigurationManager.GetLabDbConnectionString()), true)
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

            // Configure foreign key relations explicitly for SQLite compatibility
            modelBuilder.Entity<AuditLog>()
                .HasOptional(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId);

            modelBuilder.Entity<TestOrder>()
                .HasRequired(o => o.Patient)
                .WithMany()
                .HasForeignKey(o => o.PatientId);

            modelBuilder.Entity<Result>()
                .HasRequired(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId);

            modelBuilder.Entity<Result>()
                .HasRequired(r => r.TestType)
                .WithMany()
                .HasForeignKey(r => r.TypeId);

            modelBuilder.Entity<Result>()
                .HasRequired(r => r.Technician)
                .WithMany()
                .HasForeignKey(r => r.TechnicianId);

            modelBuilder.Entity<Report>()
                .HasRequired(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId);

            // Add index configurations for foreign keys
            modelBuilder.Entity<TestOrder>()
                .Property(o => o.PatientId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_TestOrders_PatientId")));

            modelBuilder.Entity<Result>()
                .Property(r => r.OrderId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Results_OrderId")));

            modelBuilder.Entity<Result>()
                .Property(r => r.TypeId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Results_TypeId")));

            modelBuilder.Entity<Result>()
                .Property(r => r.TechnicianId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Results_TechnicianId")));

            modelBuilder.Entity<Report>()
                .Property(r => r.OrderId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Reports_OrderId")));

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.UserId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_AuditLogs_UserId")));

            base.OnModelCreating(modelBuilder);
        }
    }
}
