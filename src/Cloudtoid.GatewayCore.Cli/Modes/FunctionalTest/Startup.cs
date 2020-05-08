namespace Cloudtoid.GatewayCore.Cli.Modes.FunctionalTest
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;

    internal static class Startup
    {
        internal static async Task StartAsync(int proxyPort, int upstreamPort, IConfiguration proxyConfig)
        {
            var upstreamService = Upstream.UpstreamStartup.BuildWebHost(upstreamPort);
            var proxy = Proxy.ProxyStartup.BuildWebHost(proxyPort, proxyConfig);

            await upstreamService.StartAsync();
            await proxy.StartAsync();
        }
    }
}
