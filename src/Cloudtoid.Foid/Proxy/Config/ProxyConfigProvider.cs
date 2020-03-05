namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Threading;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using static Contract;

    internal sealed class ProxyConfigProvider : IProxyConfigProvider
    {
        private readonly IConfiguration config;
        private Values values;

        public ProxyConfigProvider(IConfiguration config)
        {
            this.config = CheckValue(config, nameof(config));
            values = new Values(config);
            RegisterChangeCallback();
        }

        internal AutoResetEvent ChangeEvent { get; } = new AutoResetEvent(false);

        public TimeSpan GetTotalTimeout(HttpContext context) => values.TotalTimeout;

        private void RegisterChangeCallback()
            => config.GetReloadToken().RegisterChangeCallback(_ => OnConfigChanged(), default);

        private void OnConfigChanged()
        {
            values = new Values(config);
            RegisterChangeCallback();
            ChangeEvent.Set();
        }

        private sealed class Values
        {
            private const long TotalTimeoutDefault = 240000;

            internal Values(IConfiguration config)
            {
                TotalTimeout = TimeSpan.FromMilliseconds(config.GetValue<long>(ConfigConstants.Proxy.TotalTimeoutInMillisecondsSection, TotalTimeoutDefault));
            }

            internal TimeSpan TotalTimeout { get; }
        }
    }
}
