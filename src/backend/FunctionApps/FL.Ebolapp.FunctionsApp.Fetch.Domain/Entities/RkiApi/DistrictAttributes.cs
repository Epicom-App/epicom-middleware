using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi
{
    public class DistrictAttributes : IAttributes
    {
        [JsonPropertyName("BL_ID")]
        public int FederalStateId { get; set; }

        [JsonPropertyName("RS")]
        public string Rs { get; set; }

        [JsonPropertyName("cases7_per_100k")]
        public double Cases7Per100K { get; set; }
    }
}