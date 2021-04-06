using System;
using System.Threading;
using System.Threading.Tasks;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Services;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Services.Options;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Contracts
{
    public interface IStorageService<TBlobType, in TOptions> where TOptions : StorageServiceOptions, new()
    {
        Task AddEntry(string countryCode, TBlobType entry, DateTime? dateTime = null, CancellationToken cancellationToken = default);
        Task<TBlobType> GetEntry(CancellationToken cancellationToken = default);
    }
}