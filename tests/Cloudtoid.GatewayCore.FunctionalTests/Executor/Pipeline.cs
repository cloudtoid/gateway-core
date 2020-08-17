using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    internal sealed class Pipeline : IAsyncDisposable
    {
        private readonly int proxyPort;
        private readonly int upstreamPort;
        private readonly IConfiguration proxyConfig;
        private IWebHost? upstreamService;
        private IWebHost? proxy;

        public Pipeline(int proxyPort, int upstreamPort, IConfiguration proxyConfig)
        {
            this.proxyPort = proxyPort;
            this.upstreamPort = upstreamPort;
            this.proxyConfig = proxyConfig;
        }

        public async ValueTask DisposeAsync() => await StopAsync();

        internal async Task StartAsync()
        {
            upstreamService = UpstreamStartup.BuildWebHost(upstreamPort);
            proxy = ProxyStartup.BuildWebHost(proxyPort, proxyConfig);

            await upstreamService.StartAsync();
            await proxy.StartAsync();
        }

        internal async Task StopAsync()
        {
            if (proxy != null)
            {
                await proxy.StopAsync();
                proxy = null;
            }

            if (upstreamService != null)
            {
                await upstreamService.StopAsync();
                upstreamService = null;
            }
        }
    }
}
