using System;
using System.Collections.Generic;
using LabSystem.Core.Models;
using LabSystem.Core.Services;
using NUnit.Framework;

namespace LabSystem.Tests
{
    [TestFixture]
    public class ReferenceRangeEvaluatorTests
    {
        private TestType _testType;
        private Patient _malePatient;
        private Patient _femalePatient;
        private Patient _patientNoDob;

        [SetUp]
        public void SetUp()
        {
            _testType = new TestType
            {
                TypeId = 1,
                Name = "Hemoglobin",
                Unit = "g/dL",
                ReferenceRangeLow = 13.0,
                ReferenceRangeHigh = 17.0,
                ReferenceRanges = new List<ReferenceRange>
                {
                    new ReferenceRange { Gender = "Male", AgeMin = 0, AgeMax = 120, RangeLow = 13.0, RangeHigh = 17.0 },
                    new ReferenceRange { Gender = "Female", AgeMin = 0, AgeMax = 120, RangeLow = 12.0, RangeHigh = 16.0 },
                    new ReferenceRange { Gender = "All", AgeMin = 0, AgeMax = 5, RangeLow = 10.0, RangeHigh = 15.0 }
                }
            };

            _malePatient = new Patient
            {
                PatientId = 1,
                FullName = "John",
                Gender = "Male",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            _femalePatient = new Patient
            {
                PatientId = 2,
                FullName = "Jane",
                Gender = "Female",
                DateOfBirth = new DateTime(1995, 6, 15)
            };

            _patientNoDob = new Patient
            {
                PatientId = 3,
                FullName = "No DOB",
                Gender = "Male",
                DateOfBirth = null
            };
        }

        [Test]
        public void CalculateAge_WithDob_ReturnsCorrectAge()
        {
            var dob = new DateTime(1990, 1, 1);
            var relativeTo = new DateTime(2026, 6, 17);
            int age = ReferenceRangeEvaluator.CalculateAge(dob, relativeTo);
            Assert.That(age, Is.EqualTo(36));
        }

        [Test]
        public void CalculateAge_BeforeBirthday_ReturnsAgeMinusOne()
        {
            var dob = new DateTime(1990, 12, 31);
            var relativeTo = new DateTime(2026, 6, 17);
            int age = ReferenceRangeEvaluator.CalculateAge(dob, relativeTo);
            Assert.That(age, Is.EqualTo(35));
        }

        [Test]
        public void CalculateAge_NullDob_ReturnsDefault30()
        {
            int age = ReferenceRangeEvaluator.CalculateAge(null, DateTime.Now);
            Assert.That(age, Is.EqualTo(30));
        }

        [Test]
        public void FindMatchingRange_ByGender_ReturnsCorrectRange()
        {
            var range = ReferenceRangeEvaluator.FindMatchingRange(_testType, _malePatient);
            Assert.That(range, Is.Not.Null);
            Assert.That(range.RangeLow, Is.EqualTo(13.0));
            Assert.That(range.RangeHigh, Is.EqualTo(17.0));
        }

        [Test]
        public void FindMatchingRange_FemalePatient_ReturnsFemaleRange()
        {
            var range = ReferenceRangeEvaluator.FindMatchingRange(_testType, _femalePatient);
            Assert.That(range, Is.Not.Null);
            Assert.That(range.RangeLow, Is.EqualTo(12.0));
            Assert.That(range.RangeHigh, Is.EqualTo(16.0));
        }

        [Test]
        public void FindMatchingRange_NullTestType_ReturnsNull()
        {
            var range = ReferenceRangeEvaluator.FindMatchingRange(null, _malePatient);
            Assert.That(range, Is.Null);
        }

        [Test]
        public void FindMatchingRange_NullPatient_ReturnsNull()
        {
            var range = ReferenceRangeEvaluator.FindMatchingRange(_testType, null);
            Assert.That(range, Is.Null);
        }

        [Test]
        public void IsAbnormal_ValueAboveRange_ReturnsTrue()
        {
            bool abnormal = ReferenceRangeEvaluator.IsAbnormal(18.0, _testType, _malePatient);
            Assert.That(abnormal, Is.True);
        }

        [Test]
        public void IsAbnormal_ValueBelowRange_ReturnsTrue()
        {
            bool abnormal = ReferenceRangeEvaluator.IsAbnormal(10.0, _testType, _malePatient);
            Assert.That(abnormal, Is.True);
        }

        [Test]
        public void IsAbnormal_ValueInRange_ReturnsFalse()
        {
            bool abnormal = ReferenceRangeEvaluator.IsAbnormal(15.0, _testType, _malePatient);
            Assert.That(abnormal, Is.False);
        }

        [Test]
        public void IsAbnormal_NullValue_ReturnsFalse()
        {
            bool abnormal = ReferenceRangeEvaluator.IsAbnormal(null, _testType, _malePatient);
            Assert.That(abnormal, Is.False);
        }

        [Test]
        public void IsAbnormal_FallsBackToStaticRange_WhenNoMatchingReferenceRange()
        {
            var testType = new TestType
            {
                TypeId = 2,
                Name = "Test",
                ReferenceRangeLow = 10.0,
                ReferenceRangeHigh = 20.0
            };
            bool abnormal = ReferenceRangeEvaluator.IsAbnormal(25.0, testType, _malePatient);
            Assert.That(abnormal, Is.True);
        }

        [Test]
        public void IsAbnormal_UsesStaticRange_WhenWithinBounds()
        {
            var testType = new TestType
            {
                TypeId = 2,
                Name = "Test",
                ReferenceRangeLow = 10.0,
                ReferenceRangeHigh = 20.0
            };
            bool abnormal = ReferenceRangeEvaluator.IsAbnormal(15.0, testType, _malePatient);
            Assert.That(abnormal, Is.False);
        }

        [Test]
        public void IsLow_ValueBelowRange_ReturnsTrue()
        {
            bool low = ReferenceRangeEvaluator.IsLow(11.0, _testType, _malePatient);
            Assert.That(low, Is.True);
        }

        [Test]
        public void IsLow_ValueInRange_ReturnsFalse()
        {
            bool low = ReferenceRangeEvaluator.IsLow(15.0, _testType, _malePatient);
            Assert.That(low, Is.False);
        }

        [Test]
        public void FormatRange_WithMatchingRange_ReturnsFormattedString()
        {
            var range = ReferenceRangeEvaluator.FormatRange(_testType, _malePatient);
            Assert.That(range, Is.EqualTo("13 - 17"));
        }

        [Test]
        public void FormatRange_NullTestType_ReturnsN_A()
        {
            var range = ReferenceRangeEvaluator.FormatRange(null, _malePatient);
            Assert.That(range, Is.EqualTo("N/A"));
        }

        [Test]
        public void FormatRange_BloodGroup_ReturnsBloodGroupString()
        {
            var testType = new TestType { Unit = "Blood Group" };
            var range = ReferenceRangeEvaluator.FormatRange(testType, _malePatient);
            Assert.That(range, Is.EqualTo("A/B/O/AB Rh +/-"));
        }

        [Test]
        public void FormatRange_Qualitative_ReturnsAbsent()
        {
            var testType = new TestType { Unit = "Qualitative" };
            var range = ReferenceRangeEvaluator.FormatRange(testType, _malePatient);
            Assert.That(range, Is.EqualTo("Absent"));
        }

        [Test]
        public void FindMatchingRange_AgeSpecificRange_ReturnsAgeAppropriateRange()
        {
            var childTestType = new TestType
            {
                TypeId = 4,
                Name = "Child Test",
                ReferenceRanges = new List<ReferenceRange>
                {
                    new ReferenceRange { Gender = "All", AgeMin = 0, AgeMax = 5, RangeLow = 10.0, RangeHigh = 15.0 },
                    new ReferenceRange { Gender = "All", AgeMin = 6, AgeMax = 120, RangeLow = 12.0, RangeHigh = 18.0 }
                }
            };

            var childPatient = new Patient
            {
                PatientId = 4,
                FullName = "Child",
                Gender = "Male",
                DateOfBirth = new DateTime(2022, 1, 1)
            };

            var range = ReferenceRangeEvaluator.FindMatchingRange(childTestType, childPatient);
            Assert.That(range, Is.Not.Null);
            Assert.That(range.AgeMin, Is.EqualTo(0));
            Assert.That(range.AgeMax, Is.EqualTo(5));
        }
    }
}
