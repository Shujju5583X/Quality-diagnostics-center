using System;

namespace LabSystem.Api.Models
{
    public class PatientDto
    {
        public int Id { get; set; }
        public string Uhid { get; set; }
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public DateTime OrderedAt { get; set; }
        public string Status { get; set; }
        public string ReferredBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ResultDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string TestName { get; set; }
        public double? Value { get; set; }
        public string ValueText { get; set; }
        public string Unit { get; set; }
        public bool IsAbnormal { get; set; }
        public bool IsAmended { get; set; }
        public string AmendmentReason { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    public class TestTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double? ReferenceRangeLow { get; set; }
        public double? ReferenceRangeHigh { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string SampleType { get; set; }
    }

    public class InvoiceDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal GrandTotal { get; set; }
        public bool IsPaid { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BranchDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; }
    }
}
