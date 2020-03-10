namespace Cloudtoid.Foid
{
    using Microsoft.AspNetCore.Http;
    using static Contract;

    internal sealed class TraceIdProvider : ITraceIdProvider
    {
        private static readonly object CorrelationIdKey = new object();
        private static readonly object CallIdKey = new object();
        private readonly IGuidProvider guidProvider;
        private readonly OptionsProvider options;

        public TraceIdProvider(
            IGuidProvider guidProvider,
            OptionsProvider options)
        {
            this.guidProvider = CheckValue(guidProvider, nameof(guidProvider));
            this.options = CheckValue(options, nameof(options));
        }

        public string GetCorrelationId(HttpContext context)
        {
            if (context.Items.TryGetValue(CorrelationIdKey, out var existingId))
                return (string)existingId;

            var headersOptions = options.Proxy.Upstream.Request.Headers;
            if (headersOptions.IgnoreAllDownstreamRequestHeaders)
                return CreateCorrelationId(context);

            var correlationIdHeader = headersOptions.GetCorrelationIdHeader(context);
            if (!context.Request.Headers.TryGetValue(correlationIdHeader, out var values) || values.Count == 0)
                return CreateCorrelationId(context);

            context.Items.Add(CorrelationIdKey, values[0]);
            return values[0];
        }

        public string GetCallId(HttpContext context)
        {
            if (context.Items.TryGetValue(CallIdKey, out var id))
                return (string)id;

            var callId = guidProvider.NewGuid().ToStringInvariant("N");
            context.Items.Add(CallIdKey, callId);
            return callId;
        }

        private string CreateCorrelationId(HttpContext context)
        {
            var id = guidProvider.NewGuid().ToStringInvariant("N");
            context.Items.Add(CorrelationIdKey, id);
            return id;
        }
    }
}
