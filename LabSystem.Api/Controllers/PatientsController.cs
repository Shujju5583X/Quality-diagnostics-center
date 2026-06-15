using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LabSystem.Core.Interfaces;
using LabSystem.Core.Models;
using LabSystem.Api.Models;

namespace LabSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientRepository _patientRepo;

        public PatientsController(IPatientRepository patientRepo)
        {
            _patientRepo = patientRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var patients = await _patientRepo.SearchPatientsAsync(search, null, null, page, pageSize);
            var dtos = patients.Select(p => new PatientDto
            {
                Id = p.PatientId,
                Uhid = p.Uhid,
                FullName = p.FullName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender,
                Phone = p.ContactPhone,
                Email = p.ContactEmail,
                CreatedAt = p.CreatedAt
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var patient = await _patientRepo.GetByIdAsync(id);
            if (patient == null) return NotFound();

            return Ok(new PatientDto
            {
                Id = patient.PatientId,
                Uhid = patient.Uhid,
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                Phone = patient.ContactPhone,
                Email = patient.ContactEmail,
                CreatedAt = patient.CreatedAt
            });
        }
    }
}
