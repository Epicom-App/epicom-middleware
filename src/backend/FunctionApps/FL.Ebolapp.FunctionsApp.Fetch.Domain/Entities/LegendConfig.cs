using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities
{
    public class LegendConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("items")]
        public List<LegendConfigItem> Items { get; set; }
    }
}