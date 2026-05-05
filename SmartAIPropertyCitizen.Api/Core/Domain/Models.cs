using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace SmartAIPropertyCitizen.Api.Core.Domain
{
    #region Property Models
    public class PropertySearchResult
    {
        public int OwnerId { get; set; }
        public string PropertyNo { get; set; } = string.Empty;
        public string OwnerNameMarathi { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string UpicNo { get; set; } = string.Empty;
        public string UnmaskedMobileNo { get; set; } = string.Empty;
    }

    public class PropertyFullDetails
    {
        public int OwnerId { get; set; }
        public string NewWardNo { get; set; } = string.Empty;
        public string NewPropertyNo { get; set; } = string.Empty;
        public string PropertyNo { get; set; } = string.Empty;
        public string OwnerNameMarathi { get; set; } = string.Empty;
        public string OwnerNameEnglish { get; set; } = string.Empty;
        public string UpicNo { get; set; } = string.Empty;
        public string MarathiOwnerPatta { get; set; } = string.Empty;
        public string PropertyDescription { get; set; } = string.Empty;
        public string OldPropertyNo { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string OccupierNameMarathi { get; set; } = string.Empty;
        public string MarathiOwnerDukanImarateNav { get; set; } = string.Empty;
        public string BillDistributionDate { get; set; } = string.Empty;
        public string MarathiSocietyName { get; set; } = string.Empty;
        public string NewPlotNo { get; set; } = string.Empty;
        public string FlatOrShopNo { get; set; } = string.Empty;
    }

    public class HeadwiseTaxRow
    {
        public string HeadName { get; set; } = string.Empty;
        public double PreviousBalance { get; set; }
        public double CurrentDemand { get; set; }
        public double TotalAmount { get; set; }
    }

    public class ReceiptRow
    {
        public string ReceiptID { get; set; } = string.Empty;
        public string FinanceYear { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string TransactionDate { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string PaymentResource { get; set; } = string.Empty;
        public string ChequeStatus { get; set; } = string.Empty;
    }

    public class DiscountInfo
    {
        public string LabelName { get; set; } = string.Empty;
        public string DiscountDiscription { get; set; } = string.Empty;
        public double DiscountPercentage { get; set; }
    }
    #endregion

    #region Chat Models
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string Language { get; set; } = "mr";
    }

    public class ChatResponse
    {
        public string ResponseText { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string? PaymentUrl { get; set; }
        public string? DownloadUrl { get; set; }
    }
    #endregion

    #region OTP & Search Requests
    public class SearchRequest
    {
        [Required]
        public string SearchInput { get; set; } = string.Empty;
    }

    public class OtpSendRequest
    {
        [Required]
        public int OwnerId { get; set; }
        public string PropertyNo { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public string UpicNo { get; set; } = string.Empty;
    }

    public class OtpVerifyRequest
    {
        [Required]
        public Guid SessionId { get; set; }
        [Required]
        public string Otp { get; set; } = string.Empty;
    }

    public class OtpSession
    {
        public Guid SessionId { get; set; }
        public int OwnerId { get; set; }
        public string MobileNo { get; set; } = string.Empty;
        public string UpicNo { get; set; } = string.Empty;
        public string OtpHash { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }
        public bool IsVerified { get; set; }
        public int AttemptCount { get; set; }
    }

    public class OtpResponse
    {
        public Guid SessionId { get; set; }
        public string MaskedMobile { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class OtpVerifyResponse
    {
        public bool IsSuccess { get; set; }
        public Guid SessionId { get; set; }
        public string? Message { get; set; }
    }
    #endregion
}
