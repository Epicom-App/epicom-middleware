using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;

using FL.Ebolapp.FunctionsApp.Fetch.Domain.Contracts;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Exceptions;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Extensions.BaseResponseModelExtensions;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Extensions.LegendConfigItemExtensions;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.ResponseModels;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Services;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Services.Options;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace FL.Ebolapp.FunctionApps.Fetch
{
    public class Fetch
    {
        private readonly IRkiWebService _rkiWebService;
        private readonly IStorageService<List<DistrictResponseModel>, DistrictStorageServiceOptions> _districtStorageService;
        private readonly IStorageService<List<FederalStateResponseModel>, FederalStateStorageServiceOptions> _federalStateStorageService;
        private readonly IStorageService<List<StateConfig>, StateConfigStorageServiceOptions> _statesConfigStorageService;
        private readonly IStorageService<List<AreaConfig>, AreaConfigStorageServiceOptions> _areasConfigStorageService;
        private readonly IStorageService<LegendConfig, LegendConfigStorageServiceOptions> _legendConfigStorageService;
        private readonly ILogger _logger;

        public Fetch(
            IRkiWebService rkiWebService, 
            IStorageService<List<DistrictResponseModel>, DistrictStorageServiceOptions> districtStorageService, 
            IStorageService<List<FederalStateResponseModel>, FederalStateStorageServiceOptions> federalStateStorageService,
            IStorageService<List<StateConfig>, StateConfigStorageServiceOptions> statesConfigStorageService,
            IStorageService<List<AreaConfig>, AreaConfigStorageServiceOptions> areasConfigStorageService,
            IStorageService<LegendConfig, LegendConfigStorageServiceOptions> legendConfigStorageService,
            ILoggerFactory loggerFactory)
        {
            _rkiWebService = rkiWebService;
            _districtStorageService = districtStorageService;
            _federalStateStorageService = federalStateStorageService;
            _statesConfigStorageService = statesConfigStorageService;
            _areasConfigStorageService = areasConfigStorageService;
            _legendConfigStorageService = legendConfigStorageService;
            _logger = loggerFactory?.CreateLogger(LogCategories.CreateFunctionCategory(nameof(Fetch))) 
                      ?? throw new Exception($"Failed to inject [{nameof(ILoggerFactory)}]");
        }

        [FunctionName(nameof(Fetch) + "-" + nameof(Fetch.HttpTrigger))]
        public async Task<IActionResult> HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest request,
            ExecutionContext executionContext,
            CancellationToken cancellationToken
        )
        {
            var queryParameter = request.GetQueryParameterDictionary();

            var awaitSave = true;
            var dateTime = DateTime.UtcNow;

            if (!queryParameter.TryGetValue("await", out var awaitSaveString) && !bool.TryParse(awaitSaveString, out awaitSave))
            {
                awaitSave = true;
            }

            if (queryParameter.TryGetValue("dtOffsetDays", out var dateTimeOffsetDaysString) && int.TryParse(dateTimeOffsetDaysString, out var offset))
            {
                dateTime = DateTime.UtcNow.AddDays(offset);
            }

            return new ObjectResult(await this.HandleRequest(executionContext, awaitSave, dateTime, cancellationToken))
            {
                StatusCode = (int) HttpStatusCode.Accepted
            };
        }

        [FunctionName(nameof(Fetch) + "-" + nameof(Fetch.TimerTriggerHourly))]
        public async Task TimerTriggerHourly(
            [TimerTrigger("%TimerTriggerHourly%", RunOnStartup = false)] TimerInfo timerInfo,
            ExecutionContext executionContext,
            CancellationToken cancellationToken)
            => await this.HandleRequest(executionContext, true, DateTime.UtcNow, cancellationToken);

        [FunctionName(nameof(Fetch) + "-" + nameof(Fetch.TimerTriggerDaily))]
        public async Task TimerTriggerDaily(
            [TimerTrigger("%TimerTriggerDaily%", RunOnStartup = false)] TimerInfo timerInfo,
            ExecutionContext executionContext,
            CancellationToken cancellationToken)
            => await this.HandleRequest(executionContext, true, DateTime.UtcNow.AddDays(-1), cancellationToken);

        private async Task<FetchResponseModel> HandleRequest(
            ExecutionContext _, 
            bool awaitSave, 
            DateTime dateTime,
            CancellationToken cancellationToken = default)
        {
            var legendConfig = await _legendConfigStorageService.GetEntry(cancellationToken);
            var severities = legendConfig.Items
                .Select(LegendConfigItemExtensions.ToSeverity)
                .OrderByDescending(x => x.Value)
                .ToList();

            var federalStatesTask = HandleFederalStates(severities, awaitSave, dateTime, cancellationToken);
            var districtsTask = HandleDistricts(severities, awaitSave, dateTime, cancellationToken);

            await Task.WhenAll(federalStatesTask, districtsTask);

            return new FetchResponseModel
            {
                FederalStates = federalStatesTask.Result,
                Districts = districtsTask.Result,
            };
        }

        private async Task<List<DistrictResponseModel>> HandleDistricts(
            IReadOnlyCollection<Severity> severities, 
            bool awaitSave, 
            DateTime dateTime,
            CancellationToken cancellationToken)
        {
            List<DistrictResponseModel> districts;

            try
            {
                districts = await _rkiWebService.GetDistricts(cancellationToken);
                var areasConfig = await _areasConfigStorageService.GetEntry(cancellationToken);

                districts.ForEach(model =>
                {
                    model.InformationUrl = areasConfig.FirstOrDefault(x => x.AreaCode == model.AreaId)?.InformationUrl;
                    model.Severity = model.CalculateSeverity(severities);
                });

                _logger.LogInformation($"Districts retrieved [{districts.Count}]");
            }
            catch (Exception ex)
            {
                throw new EbolappException("Error while retrieving districts.", ex);
            }

            var task = _districtStorageService.AddEntry("DEU", districts, dateTime, cancellationToken);

            if (awaitSave)
            {
                await task;
            }

            return districts;
        }

        private async Task<List<FederalStateResponseModel>> HandleFederalStates(
            IReadOnlyCollection<Severity> severities, 
            bool awaitSave, 
            DateTime dateTime,
            CancellationToken cancellationToken)
        {
            List<FederalStateResponseModel> federalStates;

            try
            {
                federalStates = await _rkiWebService.GetFederalStates(cancellationToken);
                var statesConfig = await _statesConfigStorageService.GetEntry(cancellationToken);

                federalStates.ForEach(model =>
                {
                    model.InformationUrl = statesConfig.FirstOrDefault(x => x.StateId.ToString() == model.StateId)?.InformationUrl;
                    model.Severity = model.CalculateSeverity(severities);
                });

                _logger.LogInformation($"FederalStates retrieved [{federalStates.Count}]");
            }
            catch (Exception ex)
            {
                throw new EbolappException("Error while retrieving federal state.", ex);
            }

            var task = _federalStateStorageService.AddEntry("DEU", federalStates, dateTime, cancellationToken);

            if (awaitSave)
            {
                await task;
            }

            return federalStates;
        }
    }
}
