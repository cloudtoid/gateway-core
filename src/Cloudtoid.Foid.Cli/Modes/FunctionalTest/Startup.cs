namespace Cloudtoid.Foid.Cli.Modes.FunctionalTest
{
    using System.Threading.Tasks;

    internal static class Startup
    {
        internal static async Task StartAsync()
        {
            var upstreamService = Upstream.UpstreamStartup.BuildWebHost();
            var proxy = Proxy.ProxyStartup.BuildWebHost();

            await upstreamService.StartAsync();
            await proxy.StartAsync();
        }
    }
}
