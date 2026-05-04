using SmartAIPropertyCitizen.Api.Core.Domain;
using SmartAIPropertyCitizen.Api.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartAIPropertyCitizen.Api.Application.Services
{
    public class CitizenSearchService : ICitizenSearchService
    {
        private readonly ISqlRepository _sqlRepository;

        public CitizenSearchService(ISqlRepository sqlRepository)
        {
            _sqlRepository = sqlRepository;
        }

        public async Task<IEnumerable<PropertySearchResult>> SearchAsync(string input)
        {
            string searchType = "ByOwnerName";
            object parameters;

            if (Regex.IsMatch(input, @"^\d{10}$"))
            {
                searchType = "MobileNo";
                parameters = new { SearchType = searchType, MobileNo = input };
            }
            else if (int.TryParse(input, out int ownerId))
            {
                searchType = "OwnerID";
                parameters = new { SearchType = searchType, OwnerID = ownerId };
            }
            else if (input.Contains("-"))
            {
                searchType = "PropertyNo";
                var parts = input.Split('-');
                parameters = new 
                { 
                    SearchType = searchType, 
                    NewWardNo = parts.Length > 0 ? parts[0] : "", 
                    NewPropertyNo = parts.Length > 1 ? parts[1] : "", 
                    PartitionNo = parts.Length > 2 ? parts[2] : "" 
                };
            }
            else if (Regex.IsMatch(input, @"^[A-Z0-9]{10,}$"))
            {
                searchType = "UpicId";
                parameters = new { SearchType = searchType, UpicNo = input };
            }
            else
            {
                searchType = "ByOwnerName";
                parameters = new { SearchType = searchType, OwnerNameMarathi = input };
            }

            var results = (await _sqlRepository.ExecuteStoredProcedureAsync<PropertySearchResult>(
                "[Citizen].[Prc_CitizenSearch]",
                parameters
            )).ToList();

            foreach (var res in results)
            {
                res.UnmaskedMobileNo = res.MobileNo ?? string.Empty;
                if (res.MobileNo?.Length >= 10)
                    res.MobileNo = res.MobileNo.Substring(0, 2) + "******" + res.MobileNo.Substring(8);
            }
            return results;
        }
    }

    public class OtpService : IOtpService
    {
        private readonly ISqlRepository _sqlRepository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OtpService> _logger;

        public OtpService(ISqlRepository sqlRepository, HttpClient httpClient, IConfiguration configuration, ILogger<OtpService> logger)
        {
            _sqlRepository = sqlRepository;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<OtpSession?> GetSessionAsync(Guid sessionId)
        {
            var sql = "SELECT * FROM CitizenOtpSession WHERE SessionId = @SessionId";
            return (await _sqlRepository.QueryAsync<OtpSession>(sql, new { SessionId = sessionId })).FirstOrDefault();
        }

        public async Task<OtpResponse> GenerateOtpAsync(int ownerId, string mobileNumber, string upicNo)
        {
            var isTestMode = _configuration.GetValue<bool>("SmsGateway:OtpTestMode");
            var otp = isTestMode ? "123456" : new Random().Next(100000, 999999).ToString();
            var otpHash = HashOtp(otp);
            var sessionId = Guid.NewGuid();
            var expiry = DateTime.Now.AddMinutes(5);

            var sql = @"INSERT INTO CitizenOtpSession (SessionId, OwnerID, MobileNo, UpicNo, OtpHash, Expiry, IsVerified, AttemptCount, CreatedDate)
                        VALUES (@SessionId, @OwnerId, @MobileNo, @UpicNo, @OtpHash, @Expiry, 0, 0, GETDATE())";

            await _sqlRepository.ExecuteAsync(sql, new { SessionId = sessionId, OwnerId = ownerId, MobileNo = mobileNumber, UpicNo = upicNo, OtpHash = otpHash, Expiry = expiry });

            if (!isTestMode)
            {
                await SendSmsAsync(mobileNumber, otp);
            }

            return new OtpResponse
            {
                SessionId = sessionId,
                MaskedMobile = mobileNumber.Length >= 10 ? mobileNumber.Substring(0, 2) + "******" + mobileNumber.Substring(8) : mobileNumber,
                Message = "OTP पाठवण्यात आला आहे."
            };
        }

        public async Task<OtpVerifyResponse> VerifyOtpAsync(Guid sessionId, string otp)
        {
            var session = await GetSessionAsync(sessionId);
            if (session == null) return new OtpVerifyResponse { IsSuccess = false, Message = "सेशन आढळले नाही." };
            if (session.IsVerified) return new OtpVerifyResponse { IsSuccess = true, SessionId = sessionId };
            if (session.Expiry < DateTime.Now) return new OtpVerifyResponse { IsSuccess = false, Message = "OTP कालबाह्य झाला आहे." };
            
            if (session.AttemptCount >= 5) return new OtpVerifyResponse { IsSuccess = false, Message = "जास्तीत जास्त प्रयत्न झाले आहेत. कृपया पुन्हा प्रयत्न करा." };

            if (session.OtpHash == HashOtp(otp))
            {
                var sqlVerify = "UPDATE CitizenOtpSession SET IsVerified = 1 WHERE SessionId = @SessionId";
                await _sqlRepository.ExecuteAsync(sqlVerify, new { SessionId = sessionId });
                return new OtpVerifyResponse { IsSuccess = true, SessionId = sessionId };
            }
            else
            {
                var sqlInc = "UPDATE CitizenOtpSession SET AttemptCount = AttemptCount + 1 WHERE SessionId = @SessionId";
                await _sqlRepository.ExecuteAsync(sqlInc, new { SessionId = sessionId });
                return new OtpVerifyResponse { IsSuccess = false, Message = "चुकीचा OTP." };
            }
        }

        private async Task SendSmsAsync(string mobile, string otp)
        {
            var smsConfig = _configuration.GetSection("SmsGateway");
            var message = $"Your PTAX Login OTP is {otp} Akola Municipal Corporation";
            var url = $"{smsConfig["Url"]}?user={smsConfig["User"]}&password={smsConfig["Password"]}&senderid={smsConfig["SenderId"]}&mobiles={mobile}&sms={Uri.EscapeDataString(message)}&tempid={smsConfig["TempId"]}";
            try { await _httpClient.GetAsync(url); } catch (Exception ex) { _logger.LogError(ex, "SMS Send Failed"); }
        }

        private string HashOtp(string otp)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(otp)));
        }
    }
}
