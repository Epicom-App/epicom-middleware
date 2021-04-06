using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi
{
    public abstract class RkiResponse
    {
        [JsonPropertyName("error")]
        public ErrorResponse Error { get; set; }
    }
}