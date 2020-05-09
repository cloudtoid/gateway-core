namespace Cloudtoid.GatewayCore.Cli
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;

    internal static class OptionDefaults
    {
        internal const int ProxyPort = 86;

        internal static IConfiguration GetDefaultOptions(int upstreamPort)
        {
            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["routes:/api/:proxy:to"] = $"http://localhost:{upstreamPort}/upstream/"
            };

            return new ConfigurationBuilder().AddInMemoryCollection(options).Build();
        }
    }
}
