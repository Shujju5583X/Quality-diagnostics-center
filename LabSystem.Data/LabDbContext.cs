using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Conventions;
using LabSystem.Core.Models;
using System.Linq;
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

        public LabDbContext(System.Data.Common.DbConnection connection) : base(connection, true)
        {
            Database.SetInitializer<LabDbContext>(null);
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<TestType> TestTypes { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<TestOrder> TestOrders { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Specimen> Specimens { get; set; }
        public DbSet<ReferenceRange> ReferenceRanges { get; set; }
        public DbSet<TestPanel> TestPanels { get; set; }
        public DbSet<QcRun> QcRuns { get; set; }
        public DbSet<QcLot> QcLots { get; set; }
        public DbSet<SmsLog> SmsLogs { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        public IQueryable<UnifiedQueueItem> GetUnifiedQueue()
        {
            return from o in TestOrders
                   join i in Invoices on o.OrderId equals i.OrderId into invoiceGroup
                   from invoice in invoiceGroup.DefaultIfEmpty()
                   select new UnifiedQueueItem
                   {
                       OrderId = o.OrderId,
                       PatientName = o.Patient != null ? o.Patient.FullName : null,
                       OrderedAt = o.OrderedAt,
                       OrderStatus = o.Status,
                       IsPaid = invoice != null && invoice.IsPaid,
                       InvoiceId = (int?)invoice.InvoiceId,
                       HasAllResults = o.TestTypes.Any() && Results.Count(r => r.OrderId == o.OrderId) >= o.TestTypes.Count()
                   };
        }

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
            modelBuilder.Entity<Invoice>().ToTable("Invoices");
            modelBuilder.Entity<Payment>().ToTable("Payments");
            modelBuilder.Entity<Specimen>().ToTable("Specimens");
            modelBuilder.Entity<ReferenceRange>().ToTable("ReferenceRanges");
            modelBuilder.Entity<TestPanel>().ToTable("TestPanels");
            modelBuilder.Entity<QcRun>().ToTable("QcRuns");
            modelBuilder.Entity<QcLot>().ToTable("QcLots");

            // SQLite explicit configurations
            modelBuilder.Entity<TestOrder>().HasKey(o => o.OrderId);
            modelBuilder.Entity<TestType>().HasKey(t => t.TypeId);
            modelBuilder.Entity<Invoice>().HasKey(i => i.InvoiceId);
            modelBuilder.Entity<Payment>().HasKey(p => p.PaymentId);
            modelBuilder.Entity<Specimen>().HasKey(s => s.SpecimenId);
            modelBuilder.Entity<ReferenceRange>().HasKey(r => r.ReferenceRangeId);
            modelBuilder.Entity<TestPanel>().HasKey(p => p.PanelId);
            modelBuilder.Entity<QcRun>().HasKey(q => q.QcRunId);
            modelBuilder.Entity<QcLot>().HasKey(q => q.QcLotId);

            // Configure foreign key relations
            modelBuilder.Entity<TestOrder>()
                .HasRequired(o => o.Patient)
                .WithMany()
                .HasForeignKey(o => o.PatientId);

            modelBuilder.Entity<Specimen>()
                .HasRequired(s => s.Order)
                .WithMany(o => o.Specimens)
                .HasForeignKey(s => s.OrderId);

            modelBuilder.Entity<ReferenceRange>()
                .HasRequired(r => r.TestType)
                .WithMany(t => t.ReferenceRanges)
                .HasForeignKey(r => r.TestTypeId);

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

            modelBuilder.Entity<Invoice>()
                .HasRequired(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId);

            modelBuilder.Entity<Payment>()
                .HasRequired(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId);

            modelBuilder.Entity<TestOrder>()
                .HasMany(o => o.TestTypes)
                .WithMany()
                .Map(m =>
                {
                    m.ToTable("OrderTestTypes");
                    m.MapLeftKey("OrderId");
                    m.MapRightKey("TypeId");
                });

            modelBuilder.Entity<TestPanel>()
                .HasMany(p => p.TestTypes)
                .WithMany()
                .Map(m =>
                {
                    m.ToTable("PanelTestTypes");
                    m.MapLeftKey("PanelId");
                    m.MapRightKey("TypeId");
                });

            modelBuilder.Entity<QcRun>()
                .HasRequired(q => q.TestType)
                .WithMany()
                .HasForeignKey(q => q.TestTypeId);

            modelBuilder.Entity<QcLot>()
                .HasRequired(q => q.TestType)
                .WithMany()
                .HasForeignKey(q => q.TestTypeId);

            modelBuilder.Entity<SmsLog>().ToTable("SmsLogs");
            modelBuilder.Entity<SmsLog>().HasKey(s => s.SmsLogId);
            modelBuilder.Entity<SmsLog>()
                .HasOptional(s => s.Patient)
                .WithMany()
                .HasForeignKey(s => s.PatientId);

            modelBuilder.Entity<Appointment>().ToTable("Appointments");
            modelBuilder.Entity<Appointment>().HasKey(a => a.AppointmentId);
            modelBuilder.Entity<Appointment>()
                .HasRequired(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.AppointmentDate)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Appointments_AppointmentDate")));

            // Index configurations
            modelBuilder.Entity<TestOrder>()
                .Property(o => o.PatientId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_TestOrders_PatientId_OrderedAt_Status", 1) { IsUnique = false }));

            modelBuilder.Entity<TestOrder>()
                .Property(o => o.OrderedAt)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_TestOrders_PatientId_OrderedAt_Status", 2) { IsUnique = false }));

            modelBuilder.Entity<TestOrder>()
                .Property(o => o.Status)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_TestOrders_PatientId_OrderedAt_Status", 3) { IsUnique = false }));

            modelBuilder.Entity<Invoice>()
                .Property(i => i.OrderId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Invoices_OrderId")));

            modelBuilder.Entity<Specimen>()
                .Property(s => s.OrderId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Specimens_OrderId")));

            modelBuilder.Entity<ReferenceRange>()
                .Property(r => r.TestTypeId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_ReferenceRanges_TestTypeId")));

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

            base.OnModelCreating(modelBuilder);
        }
    }
}
