namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Net.Http.Headers;
    using static Contract;

    public class RequestHeaderValuesProvider : IRequestHeaderValuesProvider
    {
        private readonly ProxyConfig config;

        public RequestHeaderValuesProvider(ProxyConfig config)
        {
            this.config = CheckValue(config, nameof(config));
        }

        public virtual bool AllowHeadersWithEmptyValue => config.Values.UpstreamRequest.Headers.AllowHeadersWithEmptyValue;

        public virtual bool AllowHeadersWithUnderscoreInName => config.Values.UpstreamRequest.Headers.AllowHeadersWithUnderscoreInName;

        public virtual bool IncludeExternalAddress => config.Values.UpstreamRequest.Headers.IncludeExternalAddress;

        public virtual bool IgnoreClientAddress => config.Values.UpstreamRequest.Headers.IgnoreClientAddress;

        public virtual bool IgnoreClientProtocol => config.Values.UpstreamRequest.Headers.IgnoreClientProtocol;

        public virtual bool IgnoreRequestId => config.Values.UpstreamRequest.Headers.IgnoreRequestId;

        public virtual bool IgnoreCallId => config.Values.UpstreamRequest.Headers.IgnoreCallId;

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
        public virtual string GetDefaultHostHeaderValue(HttpContext context) => config.Values.UpstreamRequest.Headers.DefaultHost;

        public virtual string? GetProxyNameHeaderValue(HttpContext context) => config.Values.UpstreamRequest.Headers.ProxyName;

        public virtual IEnumerable<(string Key, IEnumerable<string> Values)> GetExtraHeaders(HttpContext context) => config.Values.UpstreamRequest.Headers.ExtraHeaders;

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
