using SmartAIPropertyCitizen.Api.Core.Domain;

namespace SmartAIPropertyCitizen.Api.Core.Interfaces
{
    public interface ICitizenSearchService
    {
        Task<IEnumerable<PropertySearchResult>> SearchAsync(string input);
    }

    public interface IOtpService
    {
        Task<OtpResponse> GenerateOtpAsync(int ownerId, string mobileNumber, string upicNo);
        Task<OtpVerifyResponse> VerifyOtpAsync(Guid sessionId, string otp);
        Task<OtpSession?> GetSessionAsync(Guid sessionId);
    }

    public interface IPropertyTaxService
    {
        Task<IEnumerable<HeadwiseTaxRow>> GetHeadwiseTaxDetailsAsync(int ownerId);
        Task<IEnumerable<ReceiptRow>> GetPreviousReceiptsAsync(int ownerId);
    }

    public interface ISmartAiService
    {
        Task<ChatResponse> ProcessChatAsync(ChatRequest request);
    }

    public interface ILandingService
    {
        Task<IEnumerable<DiscountInfo>> GetDiscountsAsync();
    }

    public interface ISqlRepository
    {
        Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(string spName, object parameters);
        Task<int> ExecuteAsync(string sql, object parameters);
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters);
    }
}
