using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;

using FL.Ebolapp.FunctionsApp.Fetch.Domain.Contracts;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Services.Options;
using FL.Ebolapp.Shared.Infrastructure;
using Fl.Ebolapp.Shared.Infrastructure.Azure.Blob;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Services
{
    public class StorageService<TBlobType, TOptions> : IStorageService<TBlobType, TOptions> where TOptions : StorageServiceOptions, new()
    {
        private readonly IAzureBlobRepository<TOptions> _azureBlobRepository;
        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public StorageService(IAzureBlobRepository<TOptions> azureBlobRepository, ILoggerFactory loggerFactory)
        {
            _azureBlobRepository = azureBlobRepository;
            _logger = loggerFactory?.CreateLogger(LogCategories.CreateFunctionCategory(nameof(StorageService<TBlobType, TOptions>))) ?? throw new Exception($"Failed to inject [{nameof(ILoggerFactory)}]"); ;
            _jsonSerializerOptions = Json.DefaultJsonSerializerOptions;
        }

        public async Task AddEntry(string countryCode, TBlobType entry, DateTime? dateTime = null, CancellationToken cancellationToken = default)
        {
            await using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, entry, _jsonSerializerOptions, cancellationToken);

            dateTime ??= DateTime.UtcNow;
            var blobName = $"{countryCode}/{dateTime.Value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)}/{_azureBlobRepository.Options.TargetFileName}.json";
            
            _logger.LogInformation($"Attempt to upload json to [{blobName}].");

            try
            {
                await _azureBlobRepository.SetContainer(_azureBlobRepository.Options.InitialContainerName);
                var uri = await _azureBlobRepository.UploadAsync(
                    blobName,
                    stream, 
                    false, 
                    false, 
                    cancellationToken,
                    httpHeaders: new BlobHttpHeaders
                    {
                        ContentType = MimeType.Json,
                        CacheControl = $"max-age={_azureBlobRepository.Options.CacheControlInSeconds}" // 1h
                    });

                _logger.LogInformation($"Upload json to [{uri}] successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error uploading json file [{blobName}] to [{_azureBlobRepository.Options.GetBlobServiceClient().Uri}]");
                throw;
            }
        }

        public async Task<TBlobType> GetEntry(CancellationToken cancellationToken = default)
        {
            var blobName = $"{_azureBlobRepository.Options.TargetFileName}.json";

            _logger.LogInformation($"Attempt to get json file [{blobName}].");

            try
            {
                await _azureBlobRepository.SetContainer(_azureBlobRepository.Options.InitialContainerName);
                var (blobData, _) = await _azureBlobRepository.GetAsync(blobName, false, cancellationToken);

                if (blobData.CanSeek)
                {
                    blobData.Seek(0L, SeekOrigin.Begin);
                }
                
                var model = await JsonSerializer.DeserializeAsync<TBlobType>(blobData, _jsonSerializerOptions, cancellationToken);

                _logger.LogInformation($"Downloaded json file successfully.");

                return model;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error downloading json file [{blobName}] from [{_azureBlobRepository.Options.GetBlobServiceClient().Uri}]");
                throw;
            }
        }
    }
}