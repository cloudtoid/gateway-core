namespace Cloudtoid.Foid
{
    using Microsoft.AspNetCore.Http;
    using static Contract;

    internal sealed class TraceIdProvider : ITraceIdProvider
    {
        private static readonly object RequestIdKey = new object();
        private static readonly object CallIdKey = new object();
        private readonly IGuidProvider guidProvider;

        public TraceIdProvider(IGuidProvider guidProvider)
        {
            this.guidProvider = CheckValue(guidProvider, nameof(guidProvider));
        }

        public string GetRequestId(HttpContext context)
        {
            if (context.Items.TryGetValue(RequestIdKey, out var id))
                return (string)id;

            var requestId = guidProvider.NewGuid().ToStringInvariant("N");
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
