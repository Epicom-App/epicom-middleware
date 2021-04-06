using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Fl.Ebolapp.Shared.Infrastructure.Azure.Blob.RequestModels;

namespace Fl.Ebolapp.Shared.Infrastructure.Azure.Blob
{
    public interface IAzureBlobRepository<out TAzureBlobOptions> where TAzureBlobOptions : AzureBlobRepositoryOptions, new()
    {
        TAzureBlobOptions Options { get; }

        string VirtualPath { get; set; }

        Task SetContainer(string containerName);

        Task<Uri> UploadAsync(string name
            , Stream data
            , bool useVirtualDirectory
            , bool disposeSource
            , CancellationToken cancellationToken = default
            , Dictionary<string, string> metaData = null
            , BlobHttpHeaders httpHeaders = null);

        Task<bool> DeleteBlobAsync(string name, bool useVirtualDirectory = true, CancellationToken cancellationToken = default, DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None);

        Task<bool> DeleteBlobAsync(Uri uri, CancellationToken cancellationToken = default, DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None);

        Task<int> DeleteDirectoryAsync(string virtualDirectoryPath, bool useStoredVirtualDirectory = false, CancellationToken cancellationToken = default);

        Task<Uri[]> ListContent(string virtualPath = "", Func<BlobItemProperties, bool> predicate = null, CancellationToken cancellationToken = default);
        Task<Uri[]> ListContent(string virtualPath = "", CancellationToken cancellationToken = default);

        Task<(Stream Data, Dictionary<string, string> Properties)> GetAsync(string name, bool useVirtualDirectory = true, CancellationToken cancellationToken = default, params string[] properties);

        Task<(BlobClient blob, Dictionary<string, string> Properties)> GetBlobAsync(string name, bool useVirtualDirectory = true, CancellationToken cancellationToken = default, params string[] properties);

        Task<(Stream Data, Dictionary<string, string> Properties)> GetAsync(Uri uri, CancellationToken cancellationToken = default, params string[] properties);

        Task<(BlobClient Data, Dictionary<string, string> Properties)> GetBlobAsync(Uri uri, CancellationToken cancellationToken = default, params string[] properties);

        Task MoveAsync(string name, bool toVirtualDirectory = false, CancellationToken cancellationToken = default, string[] properties = null);

        Task MoveAsync(string name, string toVirtualDirectoryName, bool toVirtualDirectory = false, CancellationToken cancellationToken = default, string[] properties = null);

        Task<int> UploadZipSerial(string rootFolderName, Stream stream, CancellationToken cancellationToken = default);

        Task<bool> BlobExists(string fullName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a SAS token for a specific container.
        /// </summary>
        /// <param name="configuration">configuration</param>
        /// <param name="permissions">permission</param>
        /// <param name="resourceTypes">resource types, default = AccountSasResourceTypes.Container | AccountSasResourceTypes.Object</param>
        /// <param name="services">services, default = AccountSasServices.Blobs</param>
        /// <returns></returns>
        Task<(string Uri, string SasToken)> CreateContainerSharedAccessToken(
            CreateSharedAccessSignatureConfiguration configuration
            , AccountSasPermissions permissions
            , AccountSasResourceTypes resourceTypes = AccountSasResourceTypes.Container | AccountSasResourceTypes.Object
            , AccountSasServices services = AccountSasServices.Blobs
        );

        /// <summary>
        /// Create a SAS token for a specific blob.
        /// </summary>
        /// <param name="blobName">blob name</param>
        /// <param name="configuration">configuration</param>
        /// <param name="permissions">permission</param>
        /// <param name="resourceTypes">resource types, default = AccountSasResourceTypes.Object</param>
        /// <param name="services">services, default = AccountSasServices.Blobs</param>
        /// <param name="virtualPath">virtual path</param>
        /// <returns></returns>
        Task<(string Uri, string SasToken)> CreateBlobSharedAccessToken(
            string blobName,
            CreateSharedAccessSignatureConfiguration configuration
            , AccountSasPermissions permissions
            , AccountSasResourceTypes resourceTypes = AccountSasResourceTypes.Object
            , AccountSasServices services = AccountSasServices.Blobs
            , string virtualPath = null
        );

        Task<Uri> CopyAsync(
            Uri sourceUriWithSasToken
            , string targetName
            , string targetVirtualPath = null
            , Dictionary<string, object> httpHeaders = null
            , Dictionary<string, string> metadata = null
            , CancellationToken cancellationToken = default
        );

        Task<Uri> CopyAsync(
            Uri sourceUriWithSasToken
            , string targetBlobName
            , string targetVirtualPath = null
            , PropertiesRequest propertiesRequest = null
            , HttpHeadersRequest httpHeadersRequest = null
            , CancellationToken cancellationToken = default
        );
    }
}