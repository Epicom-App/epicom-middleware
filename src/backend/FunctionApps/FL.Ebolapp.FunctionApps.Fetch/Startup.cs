using System;
using System.Net;
using System.Net.Http;
using FL.Ebolapp.FunctionApps.Fetch;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Contracts;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Services;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Services.Options;
using Fl.Ebolapp.Shared.Infrastructure.Azure.Blob;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

// https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
[assembly: FunctionsStartup(typeof(Startup))]
namespace FL.Ebolapp.FunctionApps.Fetch
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets(typeof(RkiWebServiceOptions).Assembly)
                .AddUserSecrets(typeof(DistrictStorageServiceOptions).Assembly)
                .AddUserSecrets(typeof(FederalStateStorageServiceOptions).Assembly)
                .AddUserSecrets(typeof(LegendConfigStorageServiceOptions).Assembly)
                .AddUserSecrets(typeof(StateConfigStorageServiceOptions).Assembly)

                .AddEnvironmentVariables()
                .Build()
                ;

            //var rkiWebServiceOptions = config.GetSection(nameof(AzureKeyVaultRepositoryOptions)).Get<AzureKeyVaultRepositoryOptions>();

            builder.Services.Configure<DistrictStorageServiceOptions>(config.GetSection(nameof(DistrictStorageServiceOptions)));
            builder.Services.Configure<FederalStateStorageServiceOptions>(config.GetSection(nameof(FederalStateStorageServiceOptions)));
            builder.Services.Configure<LegendConfigStorageServiceOptions>(config.GetSection(nameof(LegendConfigStorageServiceOptions)));
            builder.Services.Configure<StateConfigStorageServiceOptions>(config.GetSection(nameof(StateConfigStorageServiceOptions)));
            builder.Services.Configure<AreaConfigStorageServiceOptions>(config.GetSection(nameof(AreaConfigStorageServiceOptions)));
            builder.Services.Configure<RkiWebServiceOptions>(config.GetSection(nameof(RkiWebServiceOptions)));

            builder.Services
                .AddHttpClient<IRkiWebService, RkiWebService>(client =>
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.Timeout = TimeSpan.FromMinutes(5);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(10))
                .AddPolicyHandler(Startup.GetRetryPolicy());

            builder.Services.AddTransient(typeof(IAzureBlobRepository<>), typeof(AzureBlobRepository<>));
            builder.Services.AddTransient(typeof(IStorageService<,>), typeof(StorageService<,>));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
            => HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }
}