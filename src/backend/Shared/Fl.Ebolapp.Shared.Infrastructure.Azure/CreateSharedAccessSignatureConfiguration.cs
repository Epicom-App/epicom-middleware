using System.Text.Json.Serialization;

namespace Fl.Ebolapp.Shared.Infrastructure.Azure
{
    public class CreateSharedAccessSignatureConfiguration
    {
        /// <summary>
        /// Please note that this value should be positive.
        /// </summary>
        [JsonPropertyName("validUntilInMinutes")]
        public int ValidFromInMinutes { get; set; }

        [JsonPropertyName("validForInMinutes")]
        public int ValidForInMinutes { get; set; }

        [JsonPropertyName("clockScrewInMinutes")]
        public int ClockScrewInMinutes { get; set; }

        [JsonPropertyName("containerName")]
        public string ContainerName { get; set; }
    }
}