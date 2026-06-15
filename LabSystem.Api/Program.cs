using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LabSystem.Data;
using LabSystem.Data.Repositories;
using LabSystem.Core.Interfaces;
using LabSystem.Services;

namespace LabSystem.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.json", optional: true);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddScoped<LabSystem.Data.LabDbContext>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IPatientRepository, PatientRepository>();
            builder.Services.AddScoped<ITestOrderRepository, TestOrderRepository>();
            builder.Services.AddScoped<IResultRepository, ResultRepository>();
            builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<IStaffRepository, StaffRepository>();
            builder.Services.AddScoped<ITestTypeRepository, TestTypeRepository>();
            builder.Services.AddScoped<IQcRepository, QcRepository>();
            builder.Services.AddScoped<IReportRepository, ReportRepository>();
            builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IResultService, ResultService>();
            builder.Services.AddScoped<IBillingService, BillingService>();
            builder.Services.AddScoped<IPdfReportService, PdfReportService>();
            builder.Services.AddScoped<IStaffService, StaffService>();
            builder.Services.AddScoped<QcService>();
            builder.Services.AddScoped<IAppointmentService, AppointmentService>();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });

            var app = builder.Build();

            app.UseCors();
            app.UseMiddleware<Middleware.ApiKeyAuthMiddleware>();
            app.MapControllers();

            app.Run();
        }
    }
}
