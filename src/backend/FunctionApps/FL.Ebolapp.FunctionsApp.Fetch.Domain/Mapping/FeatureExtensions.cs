using FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.ResponseModels;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Mapping
{
    public static class FeatureExtensions
    {
        public static DistrictResponseModel ToDistrict(this Feature<DistrictAttributes> entity)
            => new DistrictResponseModel
            {
                AreaId = entity.Attributes.Rs,
                Cases7Per100K = entity.Attributes.Cases7Per100K,
                StateId = entity.Attributes.FederalStateId.ToString(),
            };

        public static FederalStateResponseModel ToFederalState(this Feature<FederalStateAttributes> entity)
            => new FederalStateResponseModel
            {
                StateId = entity.Attributes.FederalStateId.ToString(),
                Cases7Per100K = entity.Attributes.Cases7Per100K,
            };
    }
}