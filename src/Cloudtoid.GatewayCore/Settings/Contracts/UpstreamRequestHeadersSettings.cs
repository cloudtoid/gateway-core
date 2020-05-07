namespace Cloudtoid.GatewayCore.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class UpstreamRequestHeadersSettings
    {
        internal UpstreamRequestHeadersSettings(
            bool allowHeadersWithEmptyValue,
            bool allowHeadersWithUnderscoreInName,
            bool includeExternalAddress,
            bool includeProxyName,
            bool ignoreAllDownstreamHeaders,
            bool ignoreVia,
            bool ignoreCorrelationId,
            bool ignoreCallId,
            bool ignoreForwarded,
            bool useXForwarded,
            IReadOnlyList<HeaderOverride> overrides)
        {
            AllowHeadersWithEmptyValue = allowHeadersWithEmptyValue;
            AllowHeadersWithUnderscoreInName = allowHeadersWithUnderscoreInName;
            IncludeExternalAddress = includeExternalAddress;
            IncludeProxyName = includeProxyName;
            IgnoreAllDownstreamHeaders = ignoreAllDownstreamHeaders;
            IgnoreVia = ignoreVia;
            IgnoreCorrelationId = ignoreCorrelationId;
            IgnoreCallId = ignoreCallId;
            IgnoreForwarded = ignoreForwarded;
            UseXForwarded = useXForwarded;
            Overrides = overrides;
            OverrideNames = new HashSet<string>(
                overrides.Select(h => h.Name),
                StringComparer.OrdinalIgnoreCase);
        }

        public bool AllowHeadersWithEmptyValue { get; }

        public bool AllowHeadersWithUnderscoreInName { get; }

        public bool IncludeExternalAddress { get; }

        public bool IncludeProxyName { get; }

        public bool IgnoreAllDownstreamHeaders { get; }

        public bool IgnoreVia { get; }

        public bool IgnoreCorrelationId { get; }

        public bool IgnoreCallId { get; }

        public bool IgnoreForwarded { get; }

        public bool UseXForwarded { get; }

        public IReadOnlyList<HeaderOverride> Overrides { get; }

        public ISet<string> OverrideNames { get; }
    }
}