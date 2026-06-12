using System;
using System.Linq;
using System.Threading.Tasks;
using LabSystem.Core.Models;
using Serilog;

namespace LabSystem.UI.ViewModels
{
    public partial class DashboardViewModel
    {
        public string AuditLogSearchQuery
        {
            get => _auditLogSearchQuery;
            set { _auditLogSearchQuery = value; OnPropertyChanged(); }
        }

        private async Task LoadAuditLogsAsync()
        {
            try
            {
                var logs = await _auditLogRepo.GetAllAsync();
                AuditLogs.Clear();
                foreach (var log in logs.OrderByDescending(l => l.LogId))
                {
                    if (log.User == null && log.UserId.HasValue)
                    {
                        log.User = await _staffRepo.GetByIdAsync(log.UserId.Value);
                    }
                    AuditLogs.Add(log);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load audit logs.");
            }
        }
    }
}
