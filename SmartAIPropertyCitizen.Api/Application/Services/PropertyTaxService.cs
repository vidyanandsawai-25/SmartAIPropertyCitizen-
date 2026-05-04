using SmartAIPropertyCitizen.Api.Core.Domain;
using SmartAIPropertyCitizen.Api.Core.Interfaces;

namespace SmartAIPropertyCitizen.Api.Application.Services
{
    public class PropertyTaxService : IPropertyTaxService
    {
        private readonly ISqlRepository _sqlRepository;
        private readonly ILogger<PropertyTaxService> _logger;

        public PropertyTaxService(ISqlRepository sqlRepository, ILogger<PropertyTaxService> logger)
        {
            _sqlRepository = sqlRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<HeadwiseTaxRow>> GetHeadwiseTaxDetailsAsync(int ownerId)
        {
            var dynamicRows = (await _sqlRepository.ExecuteStoredProcedureAsync<dynamic>(
                "dbo.Rpt_OwnerHeadWiseTaxDetails",
                new { OwnerID = ownerId }
            )).ToList();

            if (dynamicRows.Any())
            {
                return dynamicRows.Select(row => {
                    var d = (IDictionary<string, object>)row;
                    return new HeadwiseTaxRow
                    {
                        HeadName = d.ContainsKey("कराचे नाव") ? d["कराचे नाव"]?.ToString() ?? "Unknown" : "Unknown",
                        PreviousBalance = Convert.ToDouble(d.ContainsKey("मागील बाकी रु.") ? d["मागील बाकी रु."] : 0),
                        CurrentDemand = Convert.ToDouble(d.ContainsKey("चालू बाकी रु.") ? d["चालू बाकी रु."] : 0),
                        TotalAmount = Convert.ToDouble(d.ContainsKey("एकूण मागणी रु.") ? d["एकूण मागणी रु."] : 
                                       (Convert.ToDouble(d.ContainsKey("मागील बाकी रु.") ? d["मागील बाकी रु."] : 0) + 
                                        Convert.ToDouble(d.ContainsKey("चालू बाकी रु.") ? d["चालू बाकी रु."] : 0)))
                    };
                });
            }

            return Enumerable.Empty<HeadwiseTaxRow>();
        }

        public async Task<IEnumerable<ReceiptRow>> GetPreviousReceiptsAsync(int ownerId)
        {
            var sql = @"
                SELECT 
                    CONVERT(VARCHAR, bm.FinanceYear) 
                    + '-' + ISNULL(bm.BillBookNo,'NA') 
                    + '-' + CONVERT(VARCHAR, bm.InvoiceNo) AS ReceiptID,
                    MAX(bm.Amount) AS Amount,
                    FORMAT(MAX(bm.TransactionDate), 'dd-MM-yyyy hh:mm tt') AS TransactionDate,
                    MAX(ISNULL(bm.PaymentResource,'Offline Posting')) AS PaymentResource,
                    MAX(ISNULL(bm.PaymentMode,'')) AS PaymentMode,
                    MAX(CASE WHEN ISNULL(bm.ChequeStatus,'')='' THEN '-' ELSE bm.ChequeStatus END) AS ChequeStatus,
                    bm.FinanceYear
                FROM BillTransactionDetails bm WITH (NOLOCK)
                WHERE bm.OwnerID = @OwnerID
                  AND bm.InvoiceNo IS NOT NULL
                  AND ISNULL(bm.OtherFeeHeadName,'') = ''
                GROUP BY 
                    bm.InvoiceNo,
                    bm.OwnerID,
                    bm.FinanceYear,
                    bm.BillBookNo,
                    bm.GlobalReceiptNo,
                    bm.OtherFeeHeadName
                ORDER BY MAX(bm.TransactionDate) DESC";

            return await _sqlRepository.QueryAsync<ReceiptRow>(sql, new { OwnerID = ownerId });
        }
    }
}
