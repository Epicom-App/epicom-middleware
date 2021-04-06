using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi
{
    public class Feature<TFeature> where TFeature: IAttributes
    {
        [JsonPropertyName("attributes")]
        public TFeature Attributes { get; set; }
    }
}