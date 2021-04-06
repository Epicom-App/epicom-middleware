using System;
using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities
{
    public class StateConfig
    {
        [JsonPropertyName("stateId")]
        public int StateId { get; set; }

        [JsonPropertyName("stateName")]
        public string Name { get; set; }
        
        [JsonPropertyName("informationUrl")]
        public Uri InformationUrl { get; set; }
    }
}