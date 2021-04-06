using System.Collections.Generic;

namespace Fl.Ebolapp.Shared.Infrastructure.Azure.Blob.RequestModels
{
    public class PropertiesRequest
    {
        /// <summary>
        /// Properties to take from source
        /// </summary>
        public string[] TakeFromSource { get; set; }

        /// <summary>
        /// Properties to apply. Note that these values are overwriting the values we want to take from source.
        /// </summary>
        public Dictionary<string, string> ValuesToAdd { get; set; }
    }
}