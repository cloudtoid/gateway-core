namespace Cloudtoid.Foid.Settings
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
            bool includeCorrelationId,
            bool includeCallId,
            IReadOnlyList<HeaderOverride> overrides)
        {
            AllowHeadersWithEmptyValue = allowHeadersWithEmptyValue;
            AllowHeadersWithUnderscoreInName = allowHeadersWithUnderscoreInName;
            IgnoreAllUpstreamHeaders = ignoreAllUpstreamHeaders;
            IncludeCorrelationId = includeCorrelationId;
            IncludeCallId = includeCallId;
            Overrides = overrides;
            OverrideNames = new HashSet<string>(
                overrides.Select(h => h.Name),
                StringComparer.OrdinalIgnoreCase);
        }

        public bool AllowHeadersWithEmptyValue { get; }

        public bool AllowHeadersWithUnderscoreInName { get; }

        public bool IgnoreAllUpstreamHeaders { get; }

        public bool IncludeCorrelationId { get; }

        public bool IncludeCallId { get; }

        public IReadOnlyList<HeaderOverride> Overrides { get; }

        public ISet<string> OverrideNames { get; }
    }
}