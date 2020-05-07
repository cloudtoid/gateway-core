namespace Cloudtoid.GatewayCore.Cli.Modes.FunctionalTest
{
    using System.Threading.Tasks;

    internal static class Startup
    {
        internal static async Task StartAsync(int proxyPort, int upstreamPort, string proxyConfigFile)
        {
            var upstreamService = Upstream.UpstreamStartup.BuildWebHost(upstreamPort);
            var proxy = Proxy.ProxyStartup.BuildWebHost(proxyPort, proxyConfigFile);

            await upstreamService.StartAsync();
            await proxy.StartAsync();
        }
    }
}
