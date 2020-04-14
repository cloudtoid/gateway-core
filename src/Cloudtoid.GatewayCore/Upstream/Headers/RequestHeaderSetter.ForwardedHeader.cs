namespace Cloudtoid.GatewayCore.Upstream
{
    using System.Net.Http;
    using System.Text;
    using Cloudtoid.GatewayCore.Headers;

    public partial class RequestHeaderSetter
    {
        protected virtual void AddForwardedHeaders(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            if (context.ProxyUpstreamRequestHeadersSettings.IgnoreForwarded)
                return;

            if (context.ProxyUpstreamRequestHeadersSettings.UseXForwarded)
            {
                AddXForwardedHeaders(context, upstreamRequest);
                return;
            }

            AddForwardedHeader(context, upstreamRequest);
        }

        private void AddXForwardedHeaders(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            AddXForwardedForHeader(context, upstreamRequest);
            AddXForwardedProtocolHeader(context, upstreamRequest);
            AddXForwardedHostHeader(context, upstreamRequest);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded
        protected virtual void AddForwardedHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var builder = new StringBuilder();

            var forAddress = GetRemoteIpAddressOrDefault(context, wrapIpV6: true);
            if (!string.IsNullOrEmpty(forAddress))
            {
                builder.Append(ForwardedFor);
                builder.Append(forAddress);
            }

            var host = context.Request.Host;
            if (host.HasValue)
            {
                builder.AppendIfNotEmpty(Semicolon);
                builder.Append(ForwardedHost);
                builder.Append(host.Value);
            }

            var proto = context.Request.Scheme;
            if (!string.IsNullOrEmpty(proto))
            {
                builder.AppendIfNotEmpty(Semicolon);
                builder.Append(ForwardedProto);
                builder.Append(proto);
            }

            if (builder.Length == 0)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.Forwarded,
                builder.ToString());
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-For
        protected virtual void AddXForwardedForHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var forAddress = GetRemoteIpAddressOrDefault(context);
            if (string.IsNullOrEmpty(forAddress))
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.ForwardedFor,
                forAddress);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-Proto
        protected virtual void AddXForwardedProtocolHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            if (string.IsNullOrEmpty(context.Request.Scheme))
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.ForwardedProtocol,
                context.Request.Scheme);
        }

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-Host
        protected virtual void AddXForwardedHostHeader(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            var host = context.Request.Host;
            if (!host.HasValue)
                return;

            AddHeaderValues(
                context,
                upstreamRequest,
                Names.ForwardedHost,
                host.Value);
        }
    }
}
