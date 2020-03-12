﻿namespace Cloudtoid.Foid.Options
{
    using System;

    internal static class Defaults
    {
        internal static class Proxy
        {
            internal static class Upstream
            {
                internal static class Request
                {
                    internal static Version HttpVersion { get; } = Cloudtoid.HttpVersion.Version20;

                    internal static TimeSpan Timeout { get; } = TimeSpan.FromMinutes(4);

                    internal static class Headers
                    {
                        internal const string CorrelationIdHeader = ProxyHeaderNames.CorrelationId;

                        internal const string ProxyName = "foid";

                        internal static string Host { get; } = Environment.MachineName;
                    }
                }
            }
        }
    }
}