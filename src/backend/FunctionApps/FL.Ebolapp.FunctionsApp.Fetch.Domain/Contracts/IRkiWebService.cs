using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.ResponseModels;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Contracts
{
    public interface IRkiWebService
    {
        Task<List<DistrictResponseModel>> GetDistricts(CancellationToken cancellationToken);
        Task<List<FederalStateResponseModel>> GetFederalStates(CancellationToken cancellationToken);
    }
}