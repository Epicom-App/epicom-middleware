using System;
using Azure.Storage;
using Microsoft.Identity.Client;

namespace Fl.Ebolapp.Shared.Infrastructure.Azure
{
    public class AzureRepositoryOptions
    {
        protected const string StorageEmulatorAccountName = "devstoreaccount1";

        // this key is publicly available and known to it's ok to have it in git
        protected const string StorageEmulatorAccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

        public string ConnectionString { get; set; }
        public string AccountName { get; set; }
        public string AccountKey { get; set; }
        public bool UseDevelopmentStorageAccount { get; set; }
        public AzureCloudInstance AzureCloudInstance { get; set; } = AzureCloudInstance.AzurePublic;

        /// <summary>
        /// parses the connection string and overrides properties
        /// </summary>
        protected void ParseConnectionString()
        {
            if (string.IsNullOrWhiteSpace(this.ConnectionString))
            {
                return;
            }

            foreach (var part in this.ConnectionString.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = part.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length != 2)
                {
                    continue;
                }

                switch (keyValue[0])
                {
                    // not using nameof(this.AccountName) because the connection string substring will not change if the options property does
                    case "AccountName":
                    this.AccountName = keyValue[1];
                    break;

                    // not using nameof(this.AccountKey) because the connection string substring will not change if the options property does
                    case "AccountKey":
                    this.AccountKey = keyValue[1];
                    break;

                    case "EndpointSuffix":
                    // TODO: handle and set 'AzureCloudInstance'
                    break;
                }
            }
        }

        protected Uri CreateServiceUri(StorageType storageType, string accountName = null)
        {
            if (string.IsNullOrWhiteSpace(accountName)
                && string.IsNullOrWhiteSpace(this.AccountName)
                && !string.IsNullOrWhiteSpace(this.ConnectionString))
            {
                this.ParseConnectionString();
            }

            string storageTypeValue;

            switch (storageType)
            {
                case StorageType.None:
                return new Uri("https://storage.azure.com");

                case StorageType.Blob:
                storageTypeValue = "blob";
                break;

                case StorageType.Queue:
                storageTypeValue = "queue";
                break;

                default:
                throw new ArgumentOutOfRangeException(nameof(storageType), storageType, null);
            }

            var azureCloudInstanceType = this.AzureCloudInstance switch
            {
                AzureCloudInstance.None => throw new ArgumentException($"Unable to create service uri with [{nameof(AzureRepositoryOptions.AzureCloudInstance)}] of value [{nameof(AzureCloudInstance.None)}]", nameof(AzureRepositoryOptions.AzureCloudInstance)),
                AzureCloudInstance.AzurePublic => "windows",
                AzureCloudInstance.AzureChina => "chinacloudapi",
                AzureCloudInstance.AzureGermany => "cloudapi",
                AzureCloudInstance.AzureUsGovernment => "usgovcloudapi",
                _ => throw new ArgumentOutOfRangeException(nameof(this.AzureCloudInstance), this.AzureCloudInstance, null)
            };

            return new Uri($"https://{accountName ?? this.AccountName}.{storageTypeValue}.core.{azureCloudInstanceType}.net");
        }

        public StorageSharedKeyCredential CreateStorageSharedKeyCredential(string accountName = null, string accountKey = null)
        {
            if (string.IsNullOrWhiteSpace(accountName)
                && string.IsNullOrWhiteSpace(this.AccountName)
                && string.IsNullOrWhiteSpace(accountKey)
                && string.IsNullOrWhiteSpace(this.AccountKey)
                && !string.IsNullOrWhiteSpace(this.ConnectionString))
            {
                this.ParseConnectionString();
            }

            return this.UseDevelopmentStorageAccount
                ? new StorageSharedKeyCredential(AzureRepositoryOptions.StorageEmulatorAccountName, AzureRepositoryOptions.StorageEmulatorAccountKey)
                : new StorageSharedKeyCredential(accountName ?? this.AccountName, accountKey ?? this.AccountKey)
                ;
        }
    }
}
