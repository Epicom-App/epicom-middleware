using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi
{
    public class RkiFederalStatesResponse : RkiResponse
    {
        [JsonPropertyName("features")]
        public Feature<FederalStateAttributes>[] Features { get; set; }
    }
}