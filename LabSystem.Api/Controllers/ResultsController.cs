using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LabSystem.Core.Interfaces;
using LabSystem.Api.Models;

namespace LabSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultsController : ControllerBase
    {
        private readonly IResultRepository _resultRepo;

        public ResultsController(IResultRepository resultRepo)
        {
            _resultRepo = resultRepo;
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var results = await _resultRepo.GetResultsForOrderAsync(orderId);
            var dtos = results.Select(r => new ResultDto
            {
                Id = r.ResultId,
                OrderId = r.OrderId,
                TestName = r.TestType?.Name,
                Value = r.Value,
                ValueText = r.ValueText,
                Unit = r.TestType?.Unit,
                IsAbnormal = r.IsAbnormal,
                IsAmended = r.IsAmended,
                AmendmentReason = r.AmendmentReason,
                RecordedAt = r.RecordedAt
            });
            return Ok(dtos);
        }
    }
}
