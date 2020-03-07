namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using static Contract;
    using static FoidOptions.ProxyOptions.DownstreamOptions.ResponseOptions;

    public class ResponseHeaderValuesProvider : IResponseHeaderValuesProvider
    {
        public ResponseHeaderValuesProvider(IOptionsMonitor<FoidOptions> options)
        {
            Options = CheckValue(options, nameof(options));
        }

        public virtual bool AllowHeadersWithEmptyValue => HeaderOptions.AllowHeadersWithEmptyValue;

        public virtual bool AllowHeadersWithUnderscoreInName => HeaderOptions.AllowHeadersWithUnderscoreInName;

        public virtual bool IgnoreAllUpstreamResponseHeaders => HeaderOptions.IgnoreAllUpstreamResponseHeaders;

        protected virtual IOptionsMonitor<FoidOptions> Options { get; }

        protected HeadersOptions HeaderOptions => Options.CurrentValue.Proxy.Downstream.Response.Headers;

        public virtual bool TryGetHeaderValues(
            HttpContext context,
            string name,
            string[] upstreamHeaders,
            out string[] downstreamHeaders)
        {
            CheckValue(context, nameof(context));
            CheckNonEmpty(name, nameof(name));

            downstreamHeaders = upstreamHeaders;
            return true;
        }

        public virtual IEnumerable<(string Key, string[] Values)> GetExtraHeaders(HttpContext context)
            => HeaderOptions.ExtraHeaders.Select(h => (h.Key, h.Values));
    }
}
