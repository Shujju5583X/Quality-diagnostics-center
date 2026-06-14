using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Models;

namespace LabSystem.Core.Interfaces
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
        Task<IEnumerable<SmsLog>> GetSmsLogAsync(int? patientId = null, CancellationToken cancellationToken = default);
    }
}