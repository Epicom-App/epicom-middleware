using System;
using System.Collections.Generic;
using System.Linq;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Services
{
    public class RkiWebServiceUrlBuilder
    {
        private readonly Uri _baseUrl;
        private readonly IList<string> _pathParts;
        private readonly IDictionary<string, string> _parameters;

        public RkiWebServiceUrlBuilder(Uri baseUrl)
        {
            _baseUrl = baseUrl;
            _pathParts = new List<string>();
            _parameters = new Dictionary<string, string>();
        }

        public RkiWebServiceUrlBuilder AddParameter(string key, string value)
        {
            _parameters.Add(key, value);

            return this;
        }

        public RkiWebServiceUrlBuilder AddPathPart(string part)
        {
            _pathParts.Add(part);

            return this;
        }

        public Uri Build()
        {
            var parameters = string.Join("&", _parameters.Select(entry => $"{entry.Key}={entry.Value}"));

            var path = string.Join("/", _pathParts);

            return new Uri(_baseUrl + path + (string.IsNullOrEmpty(parameters) ? string.Empty : ("?" + parameters)));
        }
    }
}