namespace Cloudtoid.Foid.Proxy
{
    using System;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using static Contract;

    internal sealed class ProxyConfigProvider : IProxyConfigProvider
    {
        private Values values;

        public ProxyConfigProvider(IConfiguration config)
        {
            CheckValue(config, nameof(config));
            values = new Values(config);
            config.GetReloadToken().RegisterChangeCallback(config => values = new Values((IConfiguration)config), config);
        }

        public TimeSpan GetTotalTimeout(HttpContext context) => values.TotalTimeout;

        private sealed class Values
        {
            internal Values(IConfiguration config)
            {
                TotalTimeout = TimeSpan.FromMilliseconds(config.GetValue<long>(ConfigConstants.Proxy.TotalTimeoutInMillisecondsSection, 10000));
            }

            internal TimeSpan TotalTimeout { get; }
        }
    }
}
