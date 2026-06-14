using System;

namespace LabSystem.Services
{
    public static class SmsTemplates
    {
        public static string AppointmentReminder(string patientName, DateTime appointmentDate)
            => $"Dear {patientName}, your appointment at Quality Diagnostics Center is on {appointmentDate:dd-MMM-yyyy hh:mm tt}. Please arrive 15 minutes early.";

        public static string ResultReady(string patientName)
            => $"Dear {patientName}, your test results at Quality Diagnostics Center are ready for collection.";

        public static string PaymentDue(string patientName, decimal amount)
            => $"Dear {patientName}, a payment of ₹{amount:N2} is pending at Quality Diagnostics Center. Please settle at your earliest convenience.";
    }
}