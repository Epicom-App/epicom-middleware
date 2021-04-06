using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using FL.Ebolapp.Shared.Infrastructure;
using Fl.Ebolapp.Shared.Infrastructure.Azure.Blob.RequestModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fl.Ebolapp.Shared.Infrastructure.Azure.Blob
{
    public class AzureBlobRepository<TAzureBlobOptions> : AzureBaseRepository<TAzureBlobOptions>, IAzureBlobRepository<TAzureBlobOptions> where TAzureBlobOptions : AzureBlobRepositoryOptions, new()
    {
        public string VirtualPath
        {
            get => this.Options.VirtualPath;
            set => this.Options.VirtualPath = value;
        }

        private string _currentContainerName;
        private readonly IDictionary<string, BlobContainerClient> _cloudBlobContainers;
        protected readonly BlobServiceClient BlobServiceClient;

        public AzureBlobRepository(
            IOptions<TAzureBlobOptions> options,
            ILogger<AzureBlobRepository<TAzureBlobOptions>> logger
        ) : base(options, logger)
        {
            this._cloudBlobContainers = new Dictionary<string, BlobContainerClient>();
            this._currentContainerName = this.Options.InitialContainerName;

            this.BlobServiceClient = this.Options.GetBlobServiceClient();
        }

        #region private methods

        private async Task<BlobContainerClient> GetContainerAsync(string containerName = null)
        {
            containerName ??= this._currentContainerName ?? throw new ArgumentNullException(nameof(containerName));

            if (this._cloudBlobContainers.ContainsKey(containerName))
            {
                return this._cloudBlobContainers[containerName];
            }

            // container
            var container = this.BlobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            lock (this._cloudBlobContainers)
            {
                if (this._cloudBlobContainers.ContainsKey(containerName))
                {
                    return this._cloudBlobContainers[containerName];
                }

                this._cloudBlobContainers.Add(containerName, container);

                return container;
            }
        }

        #endregion

        /// <summary>
        /// Sets the current active container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <returns></returns>
        public async Task SetContainer(string containerName)
        {
            this._currentContainerName = containerName;

            if (this._cloudBlobContainers.ContainsKey(this._currentContainerName))
            {
                return;
            }

            await this.GetContainerAsync(this._currentContainerName).ConfigureAwait(false);
        }

        public async Task<Uri> UploadAsync(
            string name,
            Stream data,
            bool useVirtualDirectory,
            bool disposeSource,
            CancellationToken cancellationToken = default,
            Dictionary<string, string> metaData = null,
            BlobHttpHeaders httpHeaders = null
        )
        {
            var container = await this.GetContainerAsync().ConfigureAwait(false);
            var blob = container.GetBlobClient(this.GetBlobName(name, useVirtualDirectory));

            return await AzureBlobRepository<TAzureBlobOptions>
                .InternalUpload(data, blob, disposeSource, cancellationToken, metaData, httpHeaders)
                .ConfigureAwait(false);
        }

        private static async Task<Uri> InternalUpload(
            Stream data,
            BlobClient blob,
            bool disposeSource = false,
            CancellationToken cancellationToken = default,
            IDictionary<string, string> metaData = null,
            BlobHttpHeaders httpHeaders = null
        )
        {
            if (data.CanSeek)
            {
                data.Seek(0L, SeekOrigin.Begin);
            }

            await blob.UploadAsync(data, httpHeaders, metaData, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (disposeSource)
            {
                data.Dispose();
            }

            return blob.Uri;
        }

        public async Task<bool> DeleteBlobAsync(Uri uri, CancellationToken cancellationToken = default, DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None)
        {
            var blob = this.Options.GetBlobClient(uri);

            return await blob.DeleteIfExistsAsync(deleteSnapshotsOption, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> DeleteBlobAsync(string name, bool useVirtualDirectory = true, CancellationToken cancellationToken = default, DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None)
        {
            var container = await this.GetContainerAsync().ConfigureAwait(false);

            return await container
                    .GetBlobClient(this.GetBlobName(name, useVirtualDirectory))
                    .DeleteIfExistsAsync(deleteSnapshotsOption, new BlobRequestConditions(), cancellationToken)
                    .ConfigureAwait(false)
                ;
        }

        public async Task<int> DeleteDirectoryAsync(string virtualDirectoryPath, bool useStoredVirtualDirectory = false, CancellationToken cancellationToken = default)
        {
            var container = await this.GetContainerAsync().ConfigureAwait(false);
            var blobsByHierarchy = container.GetBlobsByHierarchyAsync(prefix: this.GetBlobName(virtualDirectoryPath, useStoredVirtualDirectory), cancellationToken: cancellationToken);

            var count = 0;

            await foreach (var blobHierarchyItem in blobsByHierarchy)
            {
                if (!blobHierarchyItem.IsBlob)
                {
                    continue;
                }

                try
                {
                    if (await container
                        .GetBlobClient(blobHierarchyItem.Blob.Name)
                        .DeleteIfExistsAsync(DeleteSnapshotsOption.None, cancellationToken: cancellationToken)
                        .ConfigureAwait(false))
                    {
                        count++;
                    }
                }
                catch (Exception e)
                {
                    this.Logger?.LogWarning($"Unable to delete blob [{blobHierarchyItem.Blob.Name}]. [{e}]");
                }
            }

            return count;
        }

        public async Task<Uri[]> ListContent(string virtualPath = "", Func<BlobItemProperties, bool> predicate = null, CancellationToken cancellationToken = default)
        {
            var container = await this.GetContainerAsync().ConfigureAwait(false);
            var blobUriList = new List<Uri>();

            await foreach (var blob in container
                .GetBlobsAsync(prefix: this.GetBlobName(virtualPath, false), cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                if (blob.Deleted)
                {
                    continue;
                }

                try
                {
                    if (predicate != null)
                    {
                        if (predicate(blob.Properties))
                        {
                            blobUriList.Add(container.GetBlobClient(blob.Name).Uri);
                        }
                    }
                    else
                    {
                        blobUriList.Add(container.GetBlobClient(blob.Name).Uri);
                    }
                }
                catch (Exception e)
                {
                    this.Logger?.LogWarning($"Unable to delete blob [{blob.Name}]. [{e}]");
                }
            }

            return blobUriList.ToArray();
        }

        public async Task<Uri[]> ListContent(string virtualPath = "", CancellationToken cancellationToken = default)
        {
            var container = await this.GetContainerAsync().ConfigureAwait(false);
            var blobsByHierarchy = container.GetBlobsByHierarchyAsync(prefix: this.GetBlobName(virtualPath, false), cancellationToken: cancellationToken);

            var blobUriList = new List<Uri>();

            await foreach (var blobHierarchyItem in blobsByHierarchy)
            {
                if (!blobHierarchyItem.IsBlob)
                {
                    continue;
                }

                try
                {
                    blobUriList.Add(container.GetBlobClient(blobHierarchyItem.Blob.Name).Uri);
                }
                catch (Exception e)
                {
                    this.Logger?.LogWarning($"Unable to delete blob [{blobHierarchyItem.Blob.Name}]. [{e}]");
                }
            }

            return blobUriList.ToArray();
        }

        public async Task<(Stream Data, Dictionary<string, string> Properties)> GetAsync(
            string name
            , bool useVirtualDirectory = true
            , CancellationToken cancellationToken = default
            , params string[] properties
        )
        {
            var container = await this.GetContainerAsync().ConfigureAwait(false);

            var blob = container.GetBlobClient(this.GetBlobName(name, useVirtualDirectory));
            if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                return (null, null);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await AzureBlobRepository<TAzureBlobOptions>.GetAsync(blob, cancellationToken, properties).ConfigureAwait(false);
        }

        public async Task<(BlobClient blob, Dictionary<string, string> Properties)> GetBlobAsync(
            string name
            , bool useVirtualDirectory = true
            , CancellationToken cancellationToken = default
            , params string[] properties
        )
        {
            var container = await this.GetContainerAsync().ConfigureAwait(false);

            var blob = container.GetBlobClient(this.GetBlobName(name, useVirtualDirectory));
            if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                return (null, null);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var propertyList = new Dictionary<string, string>();

            if (properties == null)
            {
                return (blob, propertyList);
            }

            propertyList = await AzureBlobRepository<TAzureBlobOptions>
                .GetBlobProperties(blob, cancellationToken, properties)
                .ConfigureAwait(false);

            return (blob, propertyList);
        }

        /// <inheritdoc />
        public async Task<(Stream Data, Dictionary<string, string> Properties)> GetAsync(
            Uri uri,
            CancellationToken cancellationToken = default,
            params string[] properties
        )
        {
            var blobClient = this.Options.GetBlobClient(uri);
            if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                return (null, null);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await AzureBlobRepository<TAzureBlobOptions>.GetAsync(blobClient, cancellationToken, properties).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<(BlobClient Data, Dictionary<string, string> Properties)> GetBlobAsync(
            Uri uri,
            CancellationToken cancellationToken = default,
            params string[] properties
        )
        {
            var blobClient = this.Options.GetBlobClient(uri);
            if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                return (null, null);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var propertyList = new Dictionary<string, string>();

            if (properties == null)
            {
                return (blobClient, propertyList);
            }

            propertyList = await AzureBlobRepository<TAzureBlobOptions>
                .GetBlobProperties(blobClient, cancellationToken, properties)
                .ConfigureAwait(false);

            return (blobClient, propertyList);
        }

        private static async Task<(Stream Data, Dictionary<string, string> Properties)> GetAsync(
            BlobClient blob,
            CancellationToken cancellationToken = default,
            params string[] properties
        )
        {
            var response = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var stream = new MemoryStream();
            await response.Value.Content.CopyToAsync(stream, 81920, cancellationToken).ConfigureAwait(false);

            var propertyList = new Dictionary<string, string>();

            if (properties == null)
            {
                return (stream, propertyList);
            }

            propertyList = await AzureBlobRepository<TAzureBlobOptions>
                .GetBlobProperties(blob, cancellationToken, properties)
                .ConfigureAwait(false);

            return (stream, propertyList);
        }

        /// <summary>
        /// Moves a blob up or down the virtual directory
        /// </summary>
        /// <param name="name"></param>
        /// <param name="toVirtualDirectory"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public async Task MoveAsync
        (
            string name,
            bool toVirtualDirectory = false,
            CancellationToken cancellationToken = default,
            string[] properties = null
        )
        {
            var container = await this.GetContainerAsync().ConfigureAwait(false);

            var blobSource = container.GetBlobClient(this.GetBlobName(name, !toVirtualDirectory));
            var blobTarget = container.GetBlobClient(this.GetBlobName(name, toVirtualDirectory));

            if (!await blobSource.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                this.Logger?.LogWarning($"Unable to move blob file [{blobSource.Uri}] to [{blobTarget.Uri}]. Source does not exist");
                return;
            }

            var blobProperties = new Dictionary<string, string>();
            if (properties != null)
            {
                blobProperties = await GetBlobProperties(blobSource, cancellationToken, properties).ConfigureAwait(false);
            }

            var copyOperation = await blobTarget.StartCopyFromUriAsync(blobSource.Uri, blobProperties, cancellationToken: cancellationToken).ConfigureAwait(false);

            await copyOperation.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await blobSource.DeleteIfExistsAsync(DeleteSnapshotsOption.None, new BlobRequestConditions(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// move blob to another virtual directory
        /// </summary>
        /// <param name="name"></param>
        /// <param name="toVirtualDirectoryName"></param>
        /// <param name="toVirtualDirectory"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public async Task MoveAsync
        (
            string name,
            string toVirtualDirectoryName,
            bool toVirtualDirectory = false,
            CancellationToken cancellationToken = default,
            string[] properties = null
        )
        {
            var container = await this.GetContainerAsync().ConfigureAwait(false);

            var blobSource = container.GetBlobClient(this.GetBlobName(name, !toVirtualDirectory));
            var blobTarget = container.GetBlobClient($"{toVirtualDirectoryName}/{name}");

            if (!await blobSource.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                this.Logger?.LogWarning($"Unable to move blob file [{blobSource.Uri}] to [{blobTarget.Uri}]. Source does not exist");
                return;
            }

            var blobProperties = new Dictionary<string, string>();
            if (properties != null)
            {
                blobProperties = await GetBlobProperties(blobSource, cancellationToken, properties).ConfigureAwait(false);
            }

            var copyOperation = await blobTarget.StartCopyFromUriAsync(blobSource.Uri, blobProperties, cancellationToken: cancellationToken).ConfigureAwait(false);

            await copyOperation.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await blobSource.DeleteIfExistsAsync(DeleteSnapshotsOption.None, new BlobRequestConditions(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// move blob to from container to container
        /// </summary>
        /// <param name="sourceContainerName"></param>
        /// <param name="targetContainerName"></param>
        /// <param name="sourceName"></param>
        /// <param name="targetName"></param>
        /// <param name="sourceVirtualPath"></param>
        /// <param name="targetVirtualPath"></param>
        /// <param name="properties"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>uri as string</returns>
        public async Task<string> MoveAsync
        (
            string sourceContainerName
            , string targetContainerName
            , string sourceName
            , string targetName
            , string sourceVirtualPath = null
            , string targetVirtualPath = null
            , string[] properties = null
            , CancellationToken cancellationToken = default
        )
        {
            var sourceContainer = await this.GetContainerAsync(sourceContainerName).ConfigureAwait(false);
            var targetContainer = await this.GetContainerAsync(targetContainerName).ConfigureAwait(false);

            var blobSource = sourceContainer.GetBlobClient(string.IsNullOrWhiteSpace(sourceVirtualPath) ? sourceName : $"{sourceVirtualPath.TrimEnd('/')}/{sourceName}");
            var blobTarget = targetContainer.GetBlobClient(string.IsNullOrWhiteSpace(targetVirtualPath) ? targetName : $"{targetVirtualPath.TrimEnd('/')}/{targetName}");

            if (!await blobSource.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                this.Logger?.LogWarning($"Unable to move blob file [{blobSource.Uri}] to [{blobTarget.Uri}]. Source is not existing");
                return null;
            }

            var blobProperties = new Dictionary<string, string>();
            if (properties != null)
            {
                blobProperties = await GetBlobProperties(blobSource, cancellationToken, properties).ConfigureAwait(false);
            }

            var copyOperation = await blobTarget
                .StartCopyFromUriAsync(blobSource.Uri, blobProperties, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await copyOperation
                .WaitForCompletionAsync(cancellationToken)
                .ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            await blobSource.DeleteIfExistsAsync(DeleteSnapshotsOption.None, new BlobRequestConditions(), cancellationToken).ConfigureAwait(false);

            return blobTarget.Uri.ToString();
        }

        /// <summary>
        /// Copy blob to from container to container
        /// Note that the copying is not limited by storage account, however, in that case the source URI must have SAS token
        /// </summary>
        /// <param name="sourceUriWithSasToken">Source URI of the blob to copy</param>
        /// <param name="targetName">Target blob name</param>
        /// <param name="targetVirtualPath">Target blob virtual path</param>
        /// <param name="httpHeaders"></param>
        /// <param name="metadata">Properties to apply to target blob</param>
        /// <param name="cancellationToken"></param>
        /// <returns>uri as string</returns>
        public async Task<Uri> CopyAsync(
            Uri sourceUriWithSasToken
            , string targetName
            , string targetVirtualPath = null
            , Dictionary<string, object> httpHeaders = null
            , Dictionary<string, string> metadata = null
            , CancellationToken cancellationToken = default
        )
        {
            var targetContainer = await this.GetContainerAsync().ConfigureAwait(false);
            var blobTarget = targetContainer.GetBlobClient(this.GetBlobName(targetName, targetVirtualPath));

            var copyOperation = await blobTarget
                .StartCopyFromUriAsync(sourceUriWithSasToken, metadata, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await copyOperation
                .WaitForCompletionAsync(cancellationToken)
                .ConfigureAwait(false);

            await this
                .SetHttpHeaders(blobTarget, httpHeaders)
                .ConfigureAwait(false);

            return blobTarget.Uri;
        }

        /// <summary>
        /// Copies blob from source uri to destination blob.
        /// </summary>
        /// <param name="sourceUriWithSasToken"></param>
        /// <param name="targetBlobName"></param>
        /// <param name="targetVirtualPath"></param>
        /// <param name="httpHeadersRequest"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="propertiesRequest"></param>
        /// <returns></returns>
        public async Task<Uri> CopyAsync(
            Uri sourceUriWithSasToken
            , string targetBlobName
            , string targetVirtualPath = null
            , PropertiesRequest propertiesRequest = null
            , HttpHeadersRequest httpHeadersRequest = null
            , CancellationToken cancellationToken = default
        )
        {
            var sourceBlobClient = new BlobClient(sourceUriWithSasToken);
            if (!await sourceBlobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new ArgumentException($"Blob [{sourceUriWithSasToken}] is not existing", nameof(sourceUriWithSasToken));
            }

            var (properties, httpHeaders) = await this.GetPropertiesFromSourceAndCombineWithValuesWeAlreadyHave(
                sourceBlobClient
                , propertiesRequest
                , httpHeadersRequest
                , cancellationToken
            ).ConfigureAwait(false);

            return await this.CopyAsync(
                sourceUriWithSasToken
                , targetBlobName
                , targetVirtualPath
                , httpHeaders
                , properties
                , cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task<(Dictionary<string, string> Properties, Dictionary<string, object> HttpHeaders)> GetPropertiesFromSourceAndCombineWithValuesWeAlreadyHave(
            BlobClient sourceBlobClient
            , PropertiesRequest propertiesRequest = null
            , HttpHeadersRequest httpHeadersRequest = null
            , CancellationToken cancellationToken = default
        )
        {
            if (propertiesRequest == null && httpHeadersRequest == null)
            {
                return (null, null);
            }

            var sourceProperties = new Dictionary<string, string>();

            var hasAnyPropertiesToTake = propertiesRequest?.TakeFromSource?.Any();
            var hasAnyHttpHeadersToTake = httpHeadersRequest?.TakeFromSource?.Any();

            // combine the properties to take from source since the azure SDK returns all properties, http headers and other metadata via the same method
            var shouldTakeSourceProperties = hasAnyPropertiesToTake == true || hasAnyHttpHeadersToTake == true;
            if (shouldTakeSourceProperties)
            {
                string[] propertiesToTakeFromSource;
                if (hasAnyPropertiesToTake == true && hasAnyHttpHeadersToTake == true)
                {
                    propertiesToTakeFromSource = propertiesRequest.TakeFromSource.Union(httpHeadersRequest.TakeFromSource).ToArray();
                }
                else if (hasAnyPropertiesToTake == true)
                {
                    propertiesToTakeFromSource = propertiesRequest.TakeFromSource;
                }
                else
                {
                    propertiesToTakeFromSource = httpHeadersRequest.TakeFromSource;
                }
                sourceProperties = await AzureBlobRepository<TAzureBlobOptions>.GetBlobProperties(sourceBlobClient, cancellationToken, propertiesToTakeFromSource).ConfigureAwait(false);
            }

            var properties = new Dictionary<string, string>();
            if (propertiesRequest?.TakeFromSource != null)
            {
                foreach (var property in propertiesRequest.TakeFromSource.Where(p => sourceProperties.ContainsKey(p)))
                {
                    properties.Add(property, sourceProperties[property]);
                }
            }

            if (propertiesRequest?.ValuesToAdd != null)
            {
                foreach (var (key, value) in propertiesRequest.ValuesToAdd)
                {
                    properties[key] = value;
                }
            }

            var httpHeaders = new Dictionary<string, object>();
            if (httpHeadersRequest?.TakeFromSource != null)
            {
                foreach (var prop in httpHeadersRequest.TakeFromSource.Where(sourceProperty => sourceProperties.ContainsKey(sourceProperty)))
                {
                    httpHeaders.Add(prop, sourceProperties[prop]);
                }
            }

            if (httpHeadersRequest?.ValuesToAdd != null)
            {
                foreach (var (key, value) in httpHeadersRequest.ValuesToAdd)
                {
                    httpHeaders[key] = value;
                }
            }

            return (properties, httpHeaders);
        }

        /// <summary>
        /// Set HTTP headers to a blob
        /// </summary>
        /// <param name="blobClient">BlobClient pointing to blob</param>
        /// <param name="httpHeaders">Dictionary of HTTP headers to set to the blob</param>
        private async Task SetHttpHeaders(BlobBaseClient blobClient, IReadOnlyDictionary<string, object> httpHeaders = null)
        {
            if (httpHeaders == null)
            {
                return;
            }

            var blobHttpHeaders = new BlobHttpHeaders();

            if (httpHeaders.TryGetValue(nameof(BlobHttpHeaders.ContentType), out var contentType) && contentType is string contentTypeAsString)
            {
                blobHttpHeaders.ContentType = contentTypeAsString;
            }
            if (httpHeaders.TryGetValue(nameof(BlobHttpHeaders.CacheControl), out var cacheControl) && cacheControl is string cacheControlAsString)
            {
                blobHttpHeaders.CacheControl = cacheControlAsString;
            }
            if (httpHeaders.TryGetValue(nameof(BlobHttpHeaders.ContentEncoding), out var contentEncoding) && contentEncoding is string contentEncodingAsString)
            {
                blobHttpHeaders.ContentEncoding = contentEncodingAsString;
            }
            if (httpHeaders.TryGetValue(nameof(BlobHttpHeaders.ContentDisposition), out var contentDisposition) && contentDisposition is string contentDispositionAsString)
            {
                blobHttpHeaders.ContentDisposition = contentDispositionAsString;
            }
            if (httpHeaders.TryGetValue(nameof(BlobHttpHeaders.ContentHash), out var contentHash) && contentHash is byte[] contentHashAsByteArray)
            {
                blobHttpHeaders.ContentHash = contentHashAsByteArray;
            }
            if (httpHeaders.TryGetValue(nameof(BlobHttpHeaders.ContentLanguage), out var contentLanguage) && contentLanguage is string contentLanguageAsString)
            {
                blobHttpHeaders.ContentLanguage = contentLanguageAsString;
            }

            await blobClient.SetHttpHeadersAsync(blobHttpHeaders).ConfigureAwait(false);
        }

        private string GetBlobName(string name, bool useVirtualDirectory)
            => string.IsNullOrEmpty(this.VirtualPath) || !useVirtualDirectory
                ? name
                : $"{this.VirtualPath.TrimEnd('/')}/{name}";

        private string GetBlobName(string name, string virtualDirectory)
            => string.IsNullOrEmpty(virtualDirectory)
                ? name
                : $"{virtualDirectory.TrimEnd('/')}/{name}";

        public async Task<int> UploadZipSerial(string rootFolderName, Stream stream, CancellationToken cancellationToken = default)
        {
            var count = 0;

            try
            {
                using var zipRoot = new ZipArchive(stream);

                var container = await this.GetContainerAsync().ConfigureAwait(false);

                foreach (var entry in zipRoot.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (entry.FullName.EndsWith("/"))
                    {
                        continue;
                    }

                    var virtualPath = $"{rootFolderName}/{entry.FullName.TrimStart('/')}";

                    await using (var file = entry.Open())
                    {
                        var dotIndex = entry.Name.LastIndexOf('.');
                        var contentType = dotIndex == -1
                                ? MimeType.OctetStream
                                : MimeTypeMap.GetMimeType(entry.Name.Substring(dotIndex).ToLower())
                            ;

                        this.Logger.LogInformation($"Upload file [{virtualPath}]:[{contentType}]");
                        var blob = container.GetBlobClient(virtualPath);

                        await blob
                            .UploadAsync(
                                file,
                                new BlobHttpHeaders
                                {
                                    ContentType = contentType
                                },
                                cancellationToken: cancellationToken)
                            .ConfigureAwait(false);

                        count++;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                this.Logger.LogInformation($"count: {count}");
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Try delete [{rootFolderName}]", ex);
                await this.DeleteBlobAsync(rootFolderName, false, cancellationToken).ConfigureAwait(false);

                return 0;
            }

            return count;
        }

        public async Task<bool> BlobExists(string fullName, CancellationToken cancellationToken = default)
        {
            var container = await this.GetContainerAsync().ConfigureAwait(false);
            var blobReference = container.GetBlobClient(fullName);

            return await blobReference.ExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<(string Uri, string SasToken)> CreateContainerSharedAccessToken
        (
            CreateSharedAccessSignatureConfiguration configuration
            , AccountSasPermissions permissions
            , AccountSasResourceTypes resourceTypes = AccountSasResourceTypes.Container | AccountSasResourceTypes.Object
            , AccountSasServices services = AccountSasServices.Blobs
        )
        {
            var container = this.BlobServiceClient.GetBlobContainerClient(configuration.ContainerName);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var token = base.CreateSharedAccessToken(configuration, permissions, resourceTypes, services);

            return (container.Uri.ToString(), token);
        }

        /// <inheritdoc />
        public async Task<(string Uri, string SasToken)> CreateBlobSharedAccessToken(
            string blobName,
            CreateSharedAccessSignatureConfiguration configuration
            , AccountSasPermissions permissions
            , AccountSasResourceTypes resourceTypes = AccountSasResourceTypes.Object
            , AccountSasServices services = AccountSasServices.Blobs
            , string virtualPath = null
        )
        {
            var container = this.BlobServiceClient.GetBlobContainerClient(configuration.ContainerName);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = container.GetBlobClient(this.GetBlobName(blobName, virtualPath));
            if (!await blob.ExistsAsync().ConfigureAwait(false))
            {
                return (null, null);
            }

            var sas = new AccountSasBuilder
            {
                ResourceTypes = resourceTypes,
                Services = services,
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-configuration.ValidFromInMinutes),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(configuration.ValidForInMinutes + configuration.ClockScrewInMinutes)
            };

            sas.SetPermissions(permissions);

            //Generate the shared access signature on the container, setting the constraints directly on the signature.
            return (blob.Uri.ToString(), "?" + sas.ToSasQueryParameters(this.Options.CreateStorageSharedKeyCredential()));
        }

        #region Static methods

        public static async Task<Dictionary<string, string>> GetBlobProperties(
            BlobBaseClient blob,
            CancellationToken cancellationToken = default,
            params string[] properties
        )
        {
            var response = await blob.GetPropertiesAsync(new BlobRequestConditions(), cancellationToken).ConfigureAwait(false);
            var blobProperties = response.Value;

            var propertyList = new Dictionary<string, string>();

            foreach (var property in properties)
            {
                string value;
                switch (property)
                {
                    case nameof(BlobProperties.AcceptRanges):
                        value = blobProperties.AcceptRanges;
                        break;

                    case nameof(BlobProperties.AccessTier):
                        value = blobProperties.AccessTier;
                        break;

                    case nameof(BlobProperties.AccessTierChangedOn):
                        value = blobProperties.AccessTierChangedOn.ToString();
                        break;

                    case nameof(BlobProperties.AccessTierInferred):
                        value = blobProperties.AccessTierInferred.ToString();
                        break;

                    case nameof(BlobProperties.ArchiveStatus):
                        value = blobProperties.ArchiveStatus;
                        break;

                    case nameof(BlobProperties.BlobCommittedBlockCount):
                        value = blobProperties.BlobCommittedBlockCount.ToString();
                        break;

                    case nameof(BlobProperties.BlobSequenceNumber):
                        value = blobProperties.BlobSequenceNumber.ToString();
                        break;

                    case nameof(BlobProperties.BlobType):
                        value = blobProperties.BlobType.ToString();
                        break;

                    case nameof(BlobProperties.CacheControl):
                        value = blobProperties.CacheControl;
                        break;

                    case nameof(BlobProperties.ContentDisposition):
                        value = blobProperties.ContentDisposition;
                        break;

                    case nameof(BlobProperties.ContentEncoding):
                        value = blobProperties.ContentEncoding;
                        break;

                    case nameof(BlobProperties.ContentHash):
                        value = Convert.ToBase64String(blobProperties.ContentHash);
                        break;

                    case nameof(BlobProperties.ContentLanguage):
                        value = blobProperties.ContentLanguage;
                        break;

                    case nameof(BlobProperties.ContentType):
                        value = blobProperties.ContentType;
                        break;

                    case nameof(BlobProperties.ContentLength):
                        value = blobProperties.ContentLength.ToString();
                        break;

                    case nameof(BlobProperties.CopyCompletedOn):
                        value = blobProperties.CopyCompletedOn.ToString();
                        break;

                    case nameof(BlobProperties.CopyId):
                        value = blobProperties.CopyId;
                        break;

                    case nameof(BlobProperties.CopyProgress):
                        value = blobProperties.CopyProgress;
                        break;

                    case nameof(BlobProperties.CopySource):
                        value = blobProperties.CopySource.ToString();
                        break;

                    case nameof(BlobProperties.CopyStatus):
                        value = blobProperties.CopyStatus.ToString();
                        break;

                    case nameof(BlobProperties.CopyStatusDescription):
                        value = blobProperties.CopyStatusDescription;
                        break;

                    case nameof(BlobProperties.CreatedOn):
                        value = blobProperties.CreatedOn.ToString();
                        break;

                    case nameof(BlobProperties.DestinationSnapshot):
                        value = blobProperties.DestinationSnapshot;
                        break;

                    case nameof(BlobProperties.ETag):
                        value = blobProperties.ETag.ToString();
                        break;

                    case nameof(BlobProperties.EncryptionKeySha256):
                        value = blobProperties.EncryptionKeySha256;
                        break;

                    case nameof(BlobProperties.EncryptionScope):
                        value = blobProperties.EncryptionScope;
                        break;

                    case nameof(BlobProperties.IsIncrementalCopy):
                        value = blobProperties.IsIncrementalCopy.ToString();
                        break;

                    case nameof(BlobProperties.IsServerEncrypted):
                        value = blobProperties.IsServerEncrypted.ToString();
                        break;

                    case nameof(BlobProperties.LastModified):
                        value = blobProperties.LastModified.ToString();
                        break;

                    case nameof(BlobProperties.LeaseDuration):
                        value = blobProperties.LeaseDuration.ToString();
                        break;

                    case nameof(BlobProperties.LeaseState):
                        value = blobProperties.LeaseState.ToString();
                        break;

                    case nameof(BlobProperties.LeaseStatus):
                        value = blobProperties.LeaseStatus.ToString();
                        break;

                    default:
                        value = blobProperties.Metadata.ContainsKey(property) ? blobProperties.Metadata[property] : string.Empty;
                        break;
                }

                propertyList.Add(property, value);
            }

            return propertyList;
        }

        /// <summary>
        /// Moves blob from source uri to destination blob.
        /// Please note that the source uri must contain SAS token if the copy is being done between storage accounts.
        /// </summary>
        /// <typeparam name="TTargetOptions"></typeparam>
        /// <param name="targetBlobRepository"></param>
        /// <param name="sourceUriWithSaSToken"></param>
        /// <param name="targetBlobName"></param>
        /// <param name="targetVirtualPath"></param>
        /// <param name="propertiesToTakeFromSource"></param>
        /// <param name="propertiesWithValues"></param>
        /// <param name="httpHeaders"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task MoveAsync<TTargetOptions>(
            IAzureBlobRepository<TTargetOptions> targetBlobRepository
            , Uri sourceUriWithSaSToken
            , string targetBlobName
            , string targetVirtualPath = null
            , string[] propertiesToTakeFromSource = null
            , Dictionary<string, string> propertiesWithValues = null
            , Dictionary<string, object> httpHeaders = null
            , CancellationToken cancellationToken = default
        )
            where TTargetOptions : AzureBlobRepositoryOptions, new()
        {
            await AzureBlobRepository<TTargetOptions>.CopyAsync(
                targetBlobRepository
                , sourceUriWithSaSToken
                , targetBlobName
                , targetVirtualPath
                , propertiesToTakeFromSource
                , propertiesWithValues
                , httpHeaders
                , cancellationToken
            ).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                await Task.FromCanceled(cancellationToken);
            }

            var sourceBlobClient = new BlobClient(sourceUriWithSaSToken);
            await sourceBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Copies blob from source uri to destination blob.
        /// Please note that the source uri must contain SAS token if the copy is being done between storage accounts
        /// </summary>
        /// <typeparam name="TTargetOptions"></typeparam>
        /// <param name="targetBlobRepository"></param>
        /// <param name="sourceUriWithSaSToken"></param>
        /// <param name="targetBlobName"></param>
        /// <param name="targetVirtualPath"></param>
        /// <param name="propertiesToTakeFromSource"></param>
        /// <param name="propertiesWithValues"></param>
        /// <param name="httpHeaders"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task CopyAsync<TTargetOptions>(
            IAzureBlobRepository<TTargetOptions> targetBlobRepository
            , Uri sourceUriWithSaSToken
            , string targetBlobName
            , string targetVirtualPath = null
            , string[] propertiesToTakeFromSource = null
            , Dictionary<string, string> propertiesWithValues = null
            , Dictionary<string, object> httpHeaders = null
            , CancellationToken cancellationToken = default
        )
            where TTargetOptions : AzureBlobRepositoryOptions, new()
        {
            var sourceBlobClient = new BlobClient(sourceUriWithSaSToken);
            if (!await sourceBlobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new ArgumentException($"Blob [{sourceUriWithSaSToken}] is not existing", nameof(sourceUriWithSaSToken));
            }

            var properties = new Dictionary<string, string>();
            if (propertiesToTakeFromSource != null)
            {
                properties = await AzureBlobRepository<TTargetOptions>.GetBlobProperties(sourceBlobClient, cancellationToken, propertiesToTakeFromSource).ConfigureAwait(false);
            }

            if (propertiesWithValues != null)
            {
                foreach (var (key, value) in propertiesWithValues.Where(targetProperty => !properties.ContainsKey(targetProperty.Key)))
                {
                    properties.Add(key, value);
                }
            }

            await targetBlobRepository.CopyAsync(
                sourceUriWithSaSToken
                , targetBlobName
                , targetVirtualPath
                , httpHeaders
                , properties
                , cancellationToken
            ).ConfigureAwait(false);
        }

        #endregion
    }
}