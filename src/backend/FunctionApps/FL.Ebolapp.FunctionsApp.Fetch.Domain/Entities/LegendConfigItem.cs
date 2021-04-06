using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities
{
    public class LegendConfigItem
    {
        [JsonPropertyName("severity")]
        public int Severity { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("isRisky")]
        public bool IsRisky { get; set; }

        [JsonPropertyName("lowerBoundary")]
        public int? LowerBoundary { get; set; }

        [JsonPropertyName("upperBoundary")]
        public int? UpperBoundary { get; set; }
    }
}