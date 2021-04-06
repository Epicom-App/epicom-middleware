using System.Collections.Generic;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.ResponseModels
{
    public class FetchResponseModel
    {
        public List<FederalStateResponseModel> FederalStates { get; set; }
        public List<DistrictResponseModel> Districts { get; set; }
    }
}