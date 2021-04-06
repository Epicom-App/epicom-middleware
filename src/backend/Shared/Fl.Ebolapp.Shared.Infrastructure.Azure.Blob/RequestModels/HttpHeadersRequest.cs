using System.Collections.Generic;

namespace Fl.Ebolapp.Shared.Infrastructure.Azure.Blob.RequestModels
{
    public class HttpHeadersRequest
    {
        /// <summary>
        /// HttpHeaders to take from source
        /// </summary>
        public string[] TakeFromSource { get; set; }

        /// <summary>
        /// HttpHeaders to apply. Note that these values are overwriting the values we want to take from source.
        /// </summary>
        public Dictionary<string, object> ValuesToAdd { get; set; }
    }
}