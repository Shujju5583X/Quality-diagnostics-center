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
    public class OrdersController : ControllerBase
    {
        private readonly ITestOrderRepository _orderRepo;

        public OrdersController(ITestOrderRepository orderRepo)
        {
            _orderRepo = orderRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string status)
        {
            var orders = await _orderRepo.GetByStatusAsync(status);
            var dtos = orders.Select(o => new OrderDto
            {
                Id = o.OrderId,
                PatientId = o.PatientId,
                PatientName = o.Patient?.FullName,
                OrderedAt = o.OrderedAt,
                Status = o.Status,
                ReferredBy = o.ReferredBy,
                CreatedAt = o.CreatedAt
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();

            return Ok(new OrderDto
            {
                Id = order.OrderId,
                PatientId = order.PatientId,
                PatientName = order.Patient?.FullName,
                OrderedAt = order.OrderedAt,
                Status = order.Status,
                ReferredBy = order.ReferredBy,
                CreatedAt = order.CreatedAt
            });
        }
    }
}
