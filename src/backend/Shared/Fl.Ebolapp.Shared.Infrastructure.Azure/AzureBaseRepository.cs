using System;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fl.Ebolapp.Shared.Infrastructure.Azure
{
    public abstract class AzureBaseRepository<TAzureOptions> : IAzureBaseRepository<TAzureOptions> where TAzureOptions : AzureRepositoryOptions, new()
    {
        public TAzureOptions Options { get; }

        protected readonly ILogger Logger;

        protected AzureBaseRepository(
            IOptions<TAzureOptions> options,
            ILogger logger
        )
        {
            Logger = logger;
            Options = options.Value;
        }

        /// <inheritdoc />
        public string CreateSharedAccessToken(
            CreateSharedAccessSignatureConfiguration configuration
            , AccountSasPermissions permissions
            , AccountSasResourceTypes resourceTypes
            , AccountSasServices services = AccountSasServices.Blobs
        )
        {
            var sas = new AccountSasBuilder
            {
                ResourceTypes = resourceTypes,
                Services = services,
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-configuration.ValidFromInMinutes),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(configuration.ValidForInMinutes + configuration.ClockScrewInMinutes)
            };

            sas.SetPermissions(permissions);

            return "?" + sas.ToSasQueryParameters(this.Options.CreateStorageSharedKeyCredential());
        }
    }
}