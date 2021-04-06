using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi
{
    public class SpatialReference
    {
        [JsonPropertyName("wkid")]
        public long WkId { get; set; }

        [JsonPropertyName("latestWkid")]
        public long LatestWkId { get; set; }
    }
}