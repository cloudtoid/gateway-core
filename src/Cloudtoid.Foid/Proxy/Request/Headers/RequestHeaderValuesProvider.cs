namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Net.Http.Headers;
    using static Contract;

    internal sealed class RequestHeaderValuesProvider : IRequestHeaderValuesProvider
    {
        public bool AllowHeadersWithEmptyValue => false;

        public bool AllowHeadersWithUnderscoreInName => false;

        public bool TryGetHeaderValues(
            HttpContext context,
            string name,
            IList<string> downstreamValues,
            out IList<string> upstreamValues)
        {
            CheckValue(context, nameof(context));
            CheckNonEmpty(name, nameof(name));

            if (name.EqualsOrdinalIgnoreCase(HeaderNames.Host))
            {
                upstreamValues = new[] { GetHostHeaderValue(context, downstreamValues) };
                return true;
            }

            upstreamValues = downstreamValues;
            return true;
        }

        // TODO: Is this a correct implementation???
        public string GetDefaultHostHeaderValue(HttpContext context) => Environment.MachineName;

        private string GetHostHeaderValue(HttpContext context, IList<string> downstreamValues)
        {
            // No value, just return the machine name as the host name
            if (downstreamValues.Count == 0)
                return GetDefaultHostHeaderValue(context);

            // If the HOST header includes a PORT number, remove the port number
            var host = downstreamValues[0];
            var portIndex = host.LastIndexOf(':');

            return portIndex == -1 ? host : host.Substring(0, portIndex);
        }
    }
}
