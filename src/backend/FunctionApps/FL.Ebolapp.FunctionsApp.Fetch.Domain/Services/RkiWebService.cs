using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using FL.Ebolapp.FunctionsApp.Fetch.Domain.Contracts;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities.RkiApi;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Exceptions;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Extensions.TypeExtensions;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Mapping;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.ResponseModels;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Services.Options;
using FL.Ebolapp.Shared.Infrastructure;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Services
{
    public class RkiWebService : IRkiWebService
    {
        private readonly HttpClient _httpClient;
        private readonly RkiWebServiceOptions _options;
        private readonly ILogger<RkiWebService> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        
        public RkiWebService(HttpClient httpClient, IOptions<RkiWebServiceOptions> options, ILogger<RkiWebService> logger)
        {
            _httpClient = httpClient;
            _options = options?.Value ?? throw new ArgumentException($"[{nameof(options)}] has to have a value.");
            _logger = logger;
            _jsonSerializerOptions = Json.DefaultJsonSerializerOptions;
        }

        public async Task<List<DistrictResponseModel>> GetDistricts(CancellationToken cancellationToken)
        {
            try
            {
                // https://npgeo-corona-npgeo-de.hub.arcgis.com/datasets/917fc37a709542548cc3be077a786c17_0/geoservice?geometry=-19.511%2C46.211%2C41.529%2C55.839
                // https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/RKI_Landkreisdaten/FeatureServer/0/query?where=1%3D1&outFields=RS,cases,cases7_per_100k&outSR=4326&f=json&returnGeometry=false
                // 
                var uri = new RkiWebServiceUrlBuilder(_options.BaseUrl)
                    .AddPathPart(_options.DistrictsPath)
                    .AddParameter("where", "1%3D1")
                    .AddParameter("outFields", string.Join(",", typeof(DistrictAttributes).GetAttributeValues<JsonPropertyNameAttribute, string>(attribute => attribute.Name)))
                    .AddParameter("outSR", "4326")
                    .AddParameter("f", "json")
                    .AddParameter("returnGeometry", "false")
                    .Build()
                    ;
                _logger.LogInformation($"GET Request uri [{uri}]");
                var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new EbolappException("Unable to retrieve district data. " +
                                               $"[{response.StatusCode}]. " +
                                               $"[{response.ReasonPhrase}].");
                }

                _logger.LogInformation($"GET Request Status Code [{response.StatusCode}]");

                var content = await response.Content.ReadAsStreamAsync();
                var responseModel = await JsonSerializer.DeserializeAsync<RkiDistrictsResponse>(content, _jsonSerializerOptions, cancellationToken)
                                    ?? throw new EbolappException($"Unable to parse response model into [{nameof(RkiDistrictsResponse)}]");

                if (responseModel.Error != null)
                {
                    throw new EbolappException($"Unable to retrieve district data. " +
                                               $"[{responseModel.Error.Code}]. " +
                                               $"[{responseModel.Error.Message}]. " +
                                               $"[{string.Join("; ", responseModel.Error.Details)}]");
                }

                return responseModel
                    .Features
                    .Select(FeatureExtensions.ToDistrict)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Exception while retrieving districts.");

                return null;
            }
        }

        public async Task<List<FederalStateResponseModel>> GetFederalStates(CancellationToken cancellationToken)
        {
            try
            {
                // https://npgeo-corona-npgeo-de.hub.arcgis.com/datasets/ef4b445a53c1406892257fe63129a8ea_0/geoservice?geometry=-28.786%2C46.270%2C45.042%2C55.886
                // https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/Coronaf%C3%A4lle_in_den_Bundesl%C3%A4ndern/FeatureServer/0/query?where=1%3D1&outFields=OBJECTID_1,cases7_bl_per_100k&returnGeometry=false&outSR=4326&f=json
                // 
                var uri = new RkiWebServiceUrlBuilder(_options.BaseUrl)
                    .AddPathPart(_options.FederalStatesPath)
                    .AddParameter("where", "1%3D1")
                    .AddParameter("outFields", string.Join(",", typeof(FederalStateAttributes).GetAttributeValues<JsonPropertyNameAttribute, string>(attribute => attribute.Name)))
                    .AddParameter("outSR", "4326")
                    .AddParameter("f", "json")
                    .AddParameter("returnGeometry", "false")
                    .Build()
                    ;
                _logger.LogInformation($"GET Request uri [{uri}]");
                var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new EbolappException("Unable to retrieve federal states data. " +
                                               $"[{response.StatusCode}]. " +
                                               $"[{response.ReasonPhrase}].");
                }

                _logger.LogInformation($"GET Request Status Code [{response.StatusCode}]");

                var content = await response.Content.ReadAsStreamAsync();
                var responseModel = await JsonSerializer.DeserializeAsync<RkiFederalStatesResponse>(content, _jsonSerializerOptions, cancellationToken)
                    ?? throw new EbolappException($"Unable to parse response model into [{nameof(RkiFederalStatesResponse)}]");

                if (responseModel.Error != null)
                {
                    throw new EbolappException($"Unable to retrieve federal states data. " +
                                               $"[{responseModel.Error.Code}]. " +
                                               $"[{responseModel.Error.Message}]. " +
                                               $"[{string.Join("; ", responseModel.Error.Details)}]");
                }

                return responseModel
                    .Features
                    .Select(FeatureExtensions.ToFederalState)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Exception while retrieving districts.");

                return null;
            }
        }
    }
}
