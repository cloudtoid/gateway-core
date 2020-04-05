namespace Cloudtoid.GatewayCore.Trace
{
    using static Contract;

    public class TraceIdProvider : ITraceIdProvider
    {
        private readonly IGuidProvider guidProvider;

        public TraceIdProvider(IGuidProvider guidProvider)
        {
            this.guidProvider = CheckValue(guidProvider, nameof(guidProvider));
        }

        public virtual string GetCorrelationIdHeader(ProxyContext context)
            => context.ProxySettings.GetCorrelationIdHeader(context);

        public virtual string GetOrCreateCorrelationId(ProxyContext context)
        {
            if (context.ProxyUpstreamRequestHeadersSettings.IgnoreAllDownstreamHeaders)
                return CreateCorrelationId();

            var correlationIdHeader = GetCorrelationIdHeader(context);
            if (!context.Request.Headers.TryGetValue(correlationIdHeader, out var values) || values.Count == 0)
                return CreateCorrelationId();

            return values[0];
        }

        public virtual string CreateCallId(ProxyContext context)
            => guidProvider.NewGuid().ToStringInvariant("N");

        private string CreateCorrelationId()
            => guidProvider.NewGuid().ToStringInvariant("N");
    }
}
