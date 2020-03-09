namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using static Contract;
    using static FoidOptions.ProxyOptions.UpstreamOptions.RequestOptions;

    public class RequestHeaderValuesProvider : IRequestHeaderValuesProvider
    {
        public RequestHeaderValuesProvider(IOptionsMonitor<FoidOptions> options)
        {
            Options = CheckValue(options, nameof(options));
        }

        public virtual bool AllowHeadersWithEmptyValue => HeaderOptions.AllowHeadersWithEmptyValue;

        public virtual bool AllowHeadersWithUnderscoreInName => HeaderOptions.AllowHeadersWithUnderscoreInName;

        public virtual bool IgnoreAllDownstreamRequestHeaders => HeaderOptions.IgnoreAllDownstreamRequestHeaders;

        public virtual bool IncludeExternalAddress => HeaderOptions.IncludeExternalAddress;

        public virtual bool IgnoreHost => HeaderOptions.IgnoreHost;

        public virtual bool IgnoreClientAddress => HeaderOptions.IgnoreClientAddress;

        public virtual bool IgnoreClientProtocol => HeaderOptions.IgnoreClientProtocol;

        public virtual bool IgnoreCorrelationId => HeaderOptions.IgnoreCorrelationId;

        public virtual bool IgnoreCallId => HeaderOptions.IgnoreCallId;

        public virtual string CorrelationIdHeader => HeaderOptions.CorrelationIdHeader;

        public virtual string ProxyNameHeaderValue => HeaderOptions.ProxyName;

        protected virtual IOptionsMonitor<FoidOptions> Options { get; }

        protected HeadersOptions HeaderOptions => Options.CurrentValue.Proxy.Upstream.Request.Headers;

        public virtual bool TryGetHeaderValues(
            HttpContext context,
            string name,
            IList<string> downstreamValues,
            out IList<string> upstreamValues)
        {
            CheckValue(context, nameof(context));
            CheckNonEmpty(name, nameof(name));

            upstreamValues = downstreamValues;
            return true;
        }

        public virtual IEnumerable<(string Key, string[] Values)> GetExtraHeaders(HttpContext context)
            => HeaderOptions.ExtraHeaders.Select(h => (h.Key, h.Values));
    }
}
