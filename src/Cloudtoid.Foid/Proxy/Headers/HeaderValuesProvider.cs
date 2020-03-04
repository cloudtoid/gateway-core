namespace Cloudtoid.Foid.Proxy
{
    using System;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Net.Http.Headers;
    using static Contract;

    internal sealed class HeaderValuesProvider : IHeaderValuesProvider
    {
        public bool AllowHeadersWithEmptyValue => false;

        public bool AllowHeadersWithUnderscoreInName => false;

        public bool TryGetHeaderValues(string name, StringValues currentValues, out StringValues values)
        {
            CheckNonEmpty(name, nameof(name));

            if (name.EqualsOrdinalIgnoreCase(HeaderNames.Host))
            {
                values = GetHostHeaderValue(currentValues);
                return true;
            }

            values = currentValues;
            return true;
        }

        private static string GetHostHeaderValue(StringValues currentValues)
        {
            // No value, just return the machine name as the host name
            if (currentValues.Count == 0)
                return Environment.MachineName;

            // If the HOST header includes a PORT number, remove the port number
            var host = currentValues[0];
            var portIndex = host.LastIndexOf(':');

            return portIndex == -1 ? host : host.Substring(0, portIndex);
        }
    }
}
