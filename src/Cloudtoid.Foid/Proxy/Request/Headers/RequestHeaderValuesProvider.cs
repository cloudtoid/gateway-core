namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Net.Http.Headers;
    using static Contract;

    public class RequestHeaderValuesProvider : IRequestHeaderValuesProvider
    {
        private const string FoidProxyName = "cloudtoid-foid";

        public virtual bool AllowHeadersWithEmptyValue => false;

        public virtual bool AllowHeadersWithUnderscoreInName => false;

        public virtual bool IncludeExternalAddress => false;

        public virtual bool IgnoreClientAddress => false;

        public virtual bool IgnoreClientProtocol => false;

        public virtual bool IgnoreRequestId => false;

        public virtual bool IgnoreCallId => false;

        public virtual bool TryGetHeaderValues(
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
        public virtual string GetDefaultHostHeaderValue(HttpContext context) => Environment.MachineName;

        public virtual string? GetProxyNameHeaderValue(HttpContext context) => FoidProxyName;

        public virtual IEnumerable<(string Key, IEnumerable<string> Values)> GetExtraHeaders(HttpContext context)
            => Enumerable.Empty<(string Key, IEnumerable<string> Values)>();

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
