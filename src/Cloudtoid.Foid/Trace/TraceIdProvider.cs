namespace Cloudtoid.Foid
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using static Contract;

    internal sealed class TraceIdProvider : ITraceIdProvider
    {
        private static readonly object RequestIdKey = new object();
        private static readonly object CallIdKey = new object();
        private readonly IGuidProvider guidProvider;
        private readonly IOptionsMonitor<FoidOptions> options;

        public TraceIdProvider(
            IGuidProvider guidProvider,
            IOptionsMonitor<FoidOptions> options)
        {
            this.guidProvider = CheckValue(guidProvider, nameof(guidProvider));
            this.options = CheckValue(options, nameof(options));
        }

        public string GetRequestId(HttpContext context)
        {
            if (context.Items.TryGetValue(RequestIdKey, out var id))
                return (string)id;

            string requestId;
            if (!options.CurrentValue.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamRequestHeaders
                && context.Request.Headers.TryGetValue(Headers.Names.RequestId, out var values)
                && values.Count > 0)
            {
                requestId = values[0];
            }
            else
            {
                requestId = guidProvider.NewGuid().ToStringInvariant("N");
            }

            context.Items.Add(RequestIdKey, requestId);
            return requestId;
        }

        public string GetCallId(HttpContext context)
        {
            if (context.Items.TryGetValue(CallIdKey, out var id))
                return (string)id;

            var callId = guidProvider.NewGuid().ToStringInvariant("N");
            context.Items.Add(CallIdKey, callId);
            return callId;
        }
    }
}
