using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi
{
    public class FederalStateAttributes : IAttributes
    {
        [JsonPropertyName("OBJECTID_1")]
        public int FederalStateId { get; set; }

        [JsonPropertyName("cases7_bl_per_100k")]
        public double Cases7Per100K { get; set; }
    }
}