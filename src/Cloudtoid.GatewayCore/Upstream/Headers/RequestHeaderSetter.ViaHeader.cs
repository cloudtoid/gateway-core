namespace Cloudtoid.GatewayCore.Upstream
{
    using System.Net.Http;
    using System.Text;
    using Microsoft.Net.Http.Headers;

    public partial class RequestHeaderSetter
    {
        private const string HttpProtocolPrefix = "HTTP/";

        /// <summary>
        /// The Via general header is added by proxies, both forward and reverse proxies, and can appear in
        /// the request headers and the response headers. It is used for tracking message forwards, avoiding
        /// request loops, and identifying the protocol capabilities of senders along the request/response chain.
        /// See <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Via">here</a> for more information.
        /// </summary>
        protected virtual void AddViaHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var builder = new StringBuilder();

            if (!context.ProxyUpstreamRequestHeadersSettings.DiscardInboundHeaders)
            {
                if (context.Request.Headers.TryGetValue(HeaderNames.Via, out var values) && values.Count > 0)
                {
                    foreach (var value in values)
                        builder.Append(value).AppendComma().AppendSpace();
                }
            }

            if (context.Request.Protocol.StartsWithOrdinalIgnoreCase(HttpProtocolPrefix))
            {
                var version = context.RequestHttpVersion;
                builder.Append(version.Major).AppendDot().Append(version.Minor);
            }
            else
            {
                builder.Append(context.Request.Protocol);
            }

            builder.AppendSpace().Append(context.ProxyName);

            upstreamRequest.Headers.TryAddWithoutValidation(
                HeaderNames.Via,
                builder.ToString());
        }
    }
}
