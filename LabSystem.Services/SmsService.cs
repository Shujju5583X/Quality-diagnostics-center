using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;

namespace LabSystem.Services
{
    public class SmsService : ISmsService, IDisposable
    {
        private readonly string _apiKey;
        private readonly string _senderId;
        private readonly HttpClient _httpClient;
        private readonly IRepository<SmsLog> _logRepo;

        public SmsService(string apiKey, string senderId, IRepository<SmsLog> logRepo)
        {
            _apiKey = apiKey;
            _senderId = senderId;
            _logRepo = logRepo;
            _httpClient = new HttpClient();
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
        {
            var log = new SmsLog
            {
                PhoneNumber = phoneNumber ?? "(no phone)",
                Message = message,
                SentAt = DateTime.UtcNow
            };

            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    log.Status = "Failed";
                    log.GatewayResponse = "Empty phone number";
                    await _logRepo.AddAsync(log, cancellationToken);
                    return false;
                }

                bool sent;
                if (!string.IsNullOrWhiteSpace(_apiKey) && !string.IsNullOrWhiteSpace(_senderId))
                {
                    var url = $"https://api.msg91.com/api/sendhttp.php?authkey={_apiKey}&mobiles={phoneNumber}&message={Uri.EscapeDataString(message)}&sender={_senderId}&route=4";
                    var response = await _httpClient.GetAsync(url, cancellationToken);
                    sent = response.IsSuccessStatusCode;
                    log.GatewayResponse = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    sent = true;
                    log.GatewayResponse = "Mock (no API key configured)";
                }

                log.Status = sent ? "Sent" : "Failed";
                await _logRepo.AddAsync(log, cancellationToken);
                return sent;
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.GatewayResponse = ex.Message;
                await _logRepo.AddAsync(log, cancellationToken);
                return false;
            }
        }

        public async Task<IEnumerable<SmsLog>> GetSmsLogAsync(int? patientId = null, CancellationToken cancellationToken = default)
        {
            var all = await _logRepo.GetAllAsync(cancellationToken);
            if (patientId.HasValue)
                return all.Where(l => l.PatientId == patientId.Value);
            return all;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}