using System;
using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.ResponseModels
{
    public abstract class BaseResponseModel
    {
        // note: <string> per requirements from apps
        [JsonPropertyName("stateId")]
        public string StateId { get; set; }

        [JsonPropertyName("numberOfCases")]
        public double Cases7Per100K { get; set; }

        [JsonPropertyName("severity")]
        public int Severity { get; set; }

        [JsonPropertyName("informationUrl")]
        public Uri InformationUrl { get; set; }
    }
}