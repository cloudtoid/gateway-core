namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Net.Http.Headers;
    using static Contract;

    public class RequestHeaderValuesProvider : IRequestHeaderValuesProvider
    {
        private readonly Config config;

        public RequestHeaderValuesProvider(Config config)
        {
            this.config = CheckValue(config, nameof(config));
        }

        public virtual bool AllowHeadersWithEmptyValue => config.Value.Upstream.Request.Headers.AllowHeadersWithEmptyValue;

        public virtual bool AllowHeadersWithUnderscoreInName => config.Value.Upstream.Request.Headers.AllowHeadersWithUnderscoreInName;

        public virtual bool IncludeExternalAddress => config.Value.Upstream.Request.Headers.IncludeExternalAddress;

        public virtual bool IgnoreClientAddress => config.Value.Upstream.Request.Headers.IgnoreClientAddress;

        public virtual bool IgnoreClientProtocol => config.Value.Upstream.Request.Headers.IgnoreClientProtocol;

        public virtual bool IgnoreRequestId => config.Value.Upstream.Request.Headers.IgnoreRequestId;

        public virtual bool IgnoreCallId => config.Value.Upstream.Request.Headers.IgnoreCallId;

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
        public virtual string GetDefaultHostHeaderValue(HttpContext context) => config.Value.Upstream.Request.Headers.DefaultHost;

        public virtual string? GetProxyNameHeaderValue(HttpContext context) => config.Value.Upstream.Request.Headers.ProxyName;

        public virtual IEnumerable<(string Key, IEnumerable<string> Values)> GetExtraHeaders(HttpContext context) => config.Value.Upstream.Request.Headers.ExtraHeaders;

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
