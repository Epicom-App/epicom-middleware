using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi
{
    public class Field
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("sqlType")]
        public string SqlType { get; set; }

        [JsonPropertyName("length")]
        public long? Length { get; set; }

        [JsonPropertyName("domain")]
        public object Domain { get; set; }

        [JsonPropertyName("defaultValue")]
        public object DefaultValue { get; set; }
    }
}