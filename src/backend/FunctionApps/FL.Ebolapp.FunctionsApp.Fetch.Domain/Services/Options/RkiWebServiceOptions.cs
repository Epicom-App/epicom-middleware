using System;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Services.Options
{
    public class RkiWebServiceOptions
    {
        public Uri BaseUrl { get; set; }

        public string DistrictsPath { get; set; }
        public string FederalStatesPath { get; set; }
    }
}