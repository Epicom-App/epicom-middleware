using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.ResponseModels
{
    public class DistrictResponseModel : BaseResponseModel
    {
        [JsonPropertyName("areaId")]
        public string AreaId { get; set; }
    }
}