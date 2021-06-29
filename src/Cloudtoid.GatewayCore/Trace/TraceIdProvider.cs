using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Trace
{
    public class TraceIdProvider : ITraceIdProvider
    {
        private readonly IGuidProvider guidProvider;

        public TraceIdProvider(IGuidProvider guidProvider)
        {
            this.guidProvider = CheckValue(guidProvider, nameof(guidProvider));
        }

        public virtual string GetOrCreateCorrelationId(ProxyContext context)
        {
            if (context.ProxyUpstreamRequestHeadersSettings.DiscardInboundHeaders)
                return CreateCorrelationId();

            if (!context.Request.Headers.TryGetValue(context.CorrelationIdHeader, out var values) || values.Count == 0)
                return CreateCorrelationId();

            return values[0];
        }

        public virtual string CreateCallId(ProxyContext context)
            => guidProvider.NewGuid().ToStringInvariant("N");

        private string CreateCorrelationId()
            => guidProvider.NewGuid().ToStringInvariant("N");
    }
}
