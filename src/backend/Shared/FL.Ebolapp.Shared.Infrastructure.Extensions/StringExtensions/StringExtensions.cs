using System;
using System.IO;
using System.Text;

namespace FL.Ebolapp.Shared.Infrastructure.Extensions.StringExtensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// converts string into a stream
        /// </summary>
        /// <param name="string">content</param>
        /// <param name="encoding">stream encoding; Encoding.UTF8 if null</param>
        /// <returns></returns>
        public static Stream ConvertToStream(this string @string, Encoding encoding = null)
        {
            if (@string == null)
            {
                throw new ArgumentNullException(nameof(@string));
            }

            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            return new MemoryStream(encoding.GetBytes(@string));
        }
    }
}
