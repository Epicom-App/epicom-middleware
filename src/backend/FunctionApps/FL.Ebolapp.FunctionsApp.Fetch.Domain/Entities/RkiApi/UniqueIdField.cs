using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi
{
    public class UniqueIdField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("isSystemMaintained")]
        public bool IsSystemMaintained { get; set; }
    }
}