using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi
{
    public class RkiDistrictsResponse : RkiResponse
    {
        [JsonPropertyName("features")]
        public Feature<DistrictAttributes>[] Features { get; set; }

        // further field unused:
        //[JsonPropertyName("objectIdFieldName")]
        //public string ObjectIdFieldName { get; set; }

        //[JsonPropertyName("uniqueIdField")]
        //public UniqueIdField UniqueIdField { get; set; }

        //[JsonPropertyName("globalIdFieldName")]
        //public string GlobalIdFieldName { get; set; }

        //[JsonPropertyName("geometryType")]
        //public string GeometryType { get; set; }

        //[JsonPropertyName("spatialReference")]
        //public SpatialReference SpatialReference { get; set; }

        //[JsonPropertyName("fields")]
        //public Field[] Fields { get; set; }
    }
}