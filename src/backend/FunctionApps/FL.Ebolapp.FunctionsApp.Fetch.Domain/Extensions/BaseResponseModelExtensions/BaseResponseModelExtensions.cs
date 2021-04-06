using System.Collections.Generic;
using System.Linq;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.ResponseModels;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Extensions.BaseResponseModelExtensions
{
    public static class BaseResponseModelExtensions
    {
        public static int CalculateSeverity(this BaseResponseModel entity, IReadOnlyCollection<Severity> severities) 
            => severities.FirstOrDefault(x => x.Matches(entity.Cases7Per100K))?.Value ?? -1;
    }
}