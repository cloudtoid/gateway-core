namespace Cloudtoid.GatewayCore.Settings
{
    using System.Collections.Generic;

    public sealed class DownstreamResponseHeadersSettings
    {
        internal DownstreamResponseHeadersSettings(
            bool allowHeadersWithEmptyValue,
            bool allowHeadersWithUnderscoreInName,
            bool ignoreAllUpstreamHeaders,
            bool ignoreVia,
            bool includeCorrelationId,
            bool includeCallId,
            bool includeServer,
            IReadOnlyDictionary<string, CookieSettings> cookies,
            IReadOnlyDictionary<string, HeaderOverride> overrides)
        {
            AllowHeadersWithEmptyValue = allowHeadersWithEmptyValue;
            AllowHeadersWithUnderscoreInName = allowHeadersWithUnderscoreInName;
            IgnoreAllUpstreamHeaders = ignoreAllUpstreamHeaders;
            IgnoreVia = ignoreVia;
            IncludeCorrelationId = includeCorrelationId;
            IncludeCallId = includeCallId;
            IncludeServer = includeServer;
            Cookies = cookies;
            Overrides = overrides;
        }

        public bool AllowHeadersWithEmptyValue { get; }

        public bool AllowHeadersWithUnderscoreInName { get; }

        public bool IgnoreAllUpstreamHeaders { get; }

        public bool IgnoreVia { get; }

        public bool IncludeCorrelationId { get; }

        public bool IncludeCallId { get; }

        public bool IncludeServer { get; }

        public IReadOnlyDictionary<string, CookieSettings> Cookies { get; }

        public IReadOnlyDictionary<string, HeaderOverride> Overrides { get; }
    }
}