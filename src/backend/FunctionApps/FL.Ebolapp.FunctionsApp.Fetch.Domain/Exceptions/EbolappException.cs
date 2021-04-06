using System;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Exceptions
{
    public class EbolappException : Exception
    {
        public EbolappException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public EbolappException(string message) : base(message)
        {
        }
    }
}