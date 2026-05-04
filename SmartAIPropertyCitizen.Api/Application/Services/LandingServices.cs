using SmartAIPropertyCitizen.Api.Core.Domain;
using SmartAIPropertyCitizen.Api.Core.Interfaces;

namespace SmartAIPropertyCitizen.Api.Application.Services
{
    public class LandingService : ILandingService
    {
        private readonly ISqlRepository _sqlRepository;

        public LandingService(ISqlRepository sqlRepository)
        {
            _sqlRepository = sqlRepository;
        }

        public async Task<IEnumerable<DiscountInfo>> GetDiscountsAsync()
        {
            return await _sqlRepository.ExecuteStoredProcedureAsync<DiscountInfo>("[Landing].[GetDiscount]", new { });
        }
    }
}
