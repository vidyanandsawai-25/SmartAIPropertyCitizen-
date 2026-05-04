using Microsoft.AspNetCore.Mvc;
using SmartAIPropertyCitizen.Api.Core.Domain;
using SmartAIPropertyCitizen.Api.Core.Interfaces;

namespace SmartAIPropertyCitizen.Api.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CitizenController : ControllerBase
    {
        private readonly ICitizenSearchService _searchService;
        private readonly IOtpService _otpService;
        private readonly ILandingService _landingService;

        public CitizenController(ICitizenSearchService searchService, IOtpService otpService, ILandingService landingService)
        {
            _searchService = searchService;
            _otpService = otpService;
            _landingService = landingService;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            var results = await _searchService.SearchAsync(request.SearchInput);
            return Ok(results);
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] OtpSendRequest request)
        {
            // Reliable lookup using OwnerId if available
            var results = await _searchService.SearchAsync(request.OwnerId > 0 ? request.OwnerId.ToString() : request.PropertyNo);
            var property = results.FirstOrDefault(p => p.OwnerId == request.OwnerId || p.PropertyNo == request.PropertyNo);
            
            if (property == null)
                return BadRequest("मालमत्ता आढळली नाही.");

            var response = await _otpService.GenerateOtpAsync(property.OwnerId, property.UnmaskedMobileNo, property.UpicNo);
            return Ok(response);
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyRequest request)
        {
            var response = await _otpService.VerifyOtpAsync(request.SessionId, request.Otp);
            if (response.IsSuccess) return Ok(response);
            return BadRequest(response);
        }

        [HttpGet("discounts")]
        public async Task<IActionResult> GetDiscounts()
        {
            var discounts = await _landingService.GetDiscountsAsync();
            return Ok(discounts);
        }
    }
}
