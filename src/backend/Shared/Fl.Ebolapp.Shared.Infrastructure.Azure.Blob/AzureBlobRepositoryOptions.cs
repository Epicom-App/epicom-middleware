using System;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace Fl.Ebolapp.Shared.Infrastructure.Azure.Blob
{
    public class AzureBlobRepositoryOptions : AzureRepositoryOptions
    {
        public string InitialContainerName { get; set; }
        public string VirtualPath { get; set; }

        public BlobClient GetBlobClient(Uri uri, BlobClientOptions options = null, bool useCredentials = true)
        {
            if (!useCredentials)
            {
                return new BlobClient(uri, options);
            }

            if (string.IsNullOrEmpty(this.AccountName))
            {
                throw new ArgumentException("No suitable configuration found to connect to storage account.");
            }

            if (string.IsNullOrEmpty(this.AccountKey))
            {
                return new BlobClient(uri, new DefaultAzureCredential(), options);
            }

            return new BlobClient(
                uri,
                base.CreateStorageSharedKeyCredential(),
                options
            );
        }

        public BlobServiceClient GetBlobServiceClient(BlobClientOptions options = null)
        {
            if (this.UseDevelopmentStorageAccount)
            {
                return new BlobServiceClient("UseDevelopmentStorage=true", options);
            }

            if (!string.IsNullOrEmpty(this.ConnectionString))
            {
                return new BlobServiceClient(base.ConnectionString, options);
            }

            if (string.IsNullOrEmpty(this.AccountName))
            {
                throw new ArgumentException("No suitable configuration found to connect to storage account.");
            }

            if (string.IsNullOrEmpty(this.AccountKey))
            {
                return new BlobServiceClient(base.CreateServiceUri(StorageType.Blob), new DefaultAzureCredential());
            }

            return new BlobServiceClient(
                base.CreateServiceUri(StorageType.Blob),
                base.CreateStorageSharedKeyCredential(),
                options
            );
        }
    }
}
