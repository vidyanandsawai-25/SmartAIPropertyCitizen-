using Microsoft.AspNetCore.Mvc;
using SmartAIPropertyCitizen.Api.Core.Interfaces;

namespace SmartAIPropertyCitizen.Api.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ISqlRepository _sqlRepository;

        public HealthController(ISqlRepository sqlRepository)
        {
            _sqlRepository = sqlRepository;
        }

        [HttpGet("db")]
        public async Task<IActionResult> CheckDb()
        {
            try
            {
                var result = await _sqlRepository.QueryAsync<int>("SELECT 1", new { });
                return Ok(new { Status = "Connected", Version = result.FirstOrDefault() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "Error", Message = ex.Message, Stack = ex.StackTrace });
            }
        }
    }
}
