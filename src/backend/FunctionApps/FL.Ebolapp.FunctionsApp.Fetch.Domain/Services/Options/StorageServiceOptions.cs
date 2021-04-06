using Fl.Ebolapp.Shared.Infrastructure.Azure.Blob;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Services.Options
{
    public class StorageServiceOptions : AzureBlobRepositoryOptions
    {
        public int CacheControlInSeconds { get; set; }
        public string TargetFileName { get; set; }
    }
}