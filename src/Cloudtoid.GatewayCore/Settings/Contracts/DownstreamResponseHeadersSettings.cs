namespace Cloudtoid.GatewayCore.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class DownstreamResponseHeadersSettings
    {
        internal DownstreamResponseHeadersSettings(
            bool allowHeadersWithEmptyValue,
            bool allowHeadersWithUnderscoreInName,
            bool ignoreAllUpstreamHeaders,
            bool ignoreVia,
            bool includeCorrelationId,
            bool includeCallId,
            IReadOnlyList<CookieSettings> cookies,
            IReadOnlyList<HeaderOverride> overrides)
        {
            AllowHeadersWithEmptyValue = allowHeadersWithEmptyValue;
            AllowHeadersWithUnderscoreInName = allowHeadersWithUnderscoreInName;
            IgnoreAllUpstreamHeaders = ignoreAllUpstreamHeaders;
            IgnoreVia = ignoreVia;
            IncludeCorrelationId = includeCorrelationId;
            IncludeCallId = includeCallId;
            Cookies = cookies;
            Overrides = overrides;
            OverrideNames = new HashSet<string>(
                overrides.Select(h => h.Name),
                StringComparer.OrdinalIgnoreCase);
        }

        public bool AllowHeadersWithEmptyValue { get; }

        public bool AllowHeadersWithUnderscoreInName { get; }

        public bool IgnoreAllUpstreamHeaders { get; }

        public bool IgnoreVia { get; }

        public bool IncludeCorrelationId { get; }

        public bool IncludeCallId { get; }

        public IReadOnlyList<CookieSettings> Cookies { get; }

        public IReadOnlyList<HeaderOverride> Overrides { get; }

        public ISet<string> OverrideNames { get; }
    }
}