using Microsoft.AspNetCore.Mvc;
using SmartAIPropertyCitizen.Api.Core.Domain;
using SmartAIPropertyCitizen.Api.Core.Interfaces;

namespace SmartAIPropertyCitizen.Api.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SmartAiController : ControllerBase
    {
        private readonly ISmartAiService _smartAiService;

        public SmartAiController(ISmartAiService smartAiService)
        {
            _smartAiService = smartAiService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            var response = await _smartAiService.ProcessChatAsync(request);
            return Ok(response);
        }
    }
}
