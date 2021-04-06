using Azure.Storage.Sas;

namespace Fl.Ebolapp.Shared.Infrastructure.Azure
{
    public interface IAzureBaseRepository<out TAzureOptions> where TAzureOptions : AzureRepositoryOptions, new()
    {
        TAzureOptions Options { get; }

        /// <summary>
        /// crates a SAS Token with the provided configurations
        /// </summary>
        /// <param name="configuration">configuration</param>
        /// <param name="permissions">permissions of the sas token. <see cref="T:Azure.Storage.Sas.AccountSasPermissions"/></param>
        /// <param name="resourceTypes">resource types the sas token has access to. <see cref="T:Azure.Storage.Sas.AccountSasResourceTypes"/></param>
        /// <param name="services">services the sas token has access to. default = AccountSasServices.Blobs <see cref="T:Azure.Storage.Sas.AccountSasServices"/></param>
        /// <returns></returns>
        string CreateSharedAccessToken(
            CreateSharedAccessSignatureConfiguration configuration
            , AccountSasPermissions permissions
            , AccountSasResourceTypes resourceTypes
            , AccountSasServices services = AccountSasServices.Blobs
        );
    }
}