using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities
{
    public class AreaConfig : StateConfig
    {
        [JsonPropertyName("areaId")]
        public int AreaId { get; set; }

        [JsonPropertyName("areaCode")]
        public string AreaCode { get; set; }
    }
}