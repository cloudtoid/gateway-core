using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    internal sealed class Pipeline : IAsyncDisposable
    {
        private static volatile int port = 8080;
        private readonly int gatewayCorePort;
        private readonly int upstreamPort;
        private readonly int nginxPort;
        private readonly IConfiguration gatewayConfig;
        private readonly NginxServer? nginx;
        private IWebHost? upstream;
        private IWebHost? gatewayCore;

        internal Pipeline(
            string gatewayConfigFile,
            string? nginxConfigFile = null)
        {
            gatewayCorePort = GetAvailablePort();
            upstreamPort = GetAvailablePort();
            gatewayConfig = LoadGatewayConfig(gatewayConfigFile, upstreamPort);

            if (nginxConfigFile is not null)
            {
                nginxConfigFile = GetNginxConfigFile(nginxConfigFile, upstreamPort);
                nginxPort = GetAvailablePort();
                nginx = new NginxServer(nginxConfigFile, nginxPort);
            }
        }

        public async ValueTask DisposeAsync() => await StopAsync();

        internal async Task StartAsync()
        {
            upstream = UpstreamStartup.BuildWebHost(upstreamPort);
            gatewayCore = GatewayCoreStartup.BuildWebHost(gatewayCorePort, gatewayConfig);

            await upstream.StartAsync();
            await gatewayCore.StartAsync();

            if (nginx is not null)
                await nginx.StartAsync();
        }

        internal async Task StopAsync()
        {
            if (nginx is not null)
                await nginx.StopAsync();

            if (gatewayCore is not null)
            {
                await gatewayCore.StopAsync();
                gatewayCore = null;
            }

            if (upstream is not null)
            {
                await upstream.StopAsync();
                upstream = null;
            }
        }

        internal HttpClient CreateGatewayCoreClient() => new()
        {
            BaseAddress = new Uri($"https://localhost:{gatewayCorePort}/api/")
        };

        internal HttpClient CreateNginxClient() => new()
        {
            BaseAddress = new Uri($"http://localhost:{nginxPort}/api/")
        };

        private static IConfiguration LoadGatewayConfig(string gatewayConfigFile, int upstreamPort)
        {
            var file = new FileInfo(gatewayConfigFile);
            if (!file.Exists)
                throw new FileNotFoundException($"File '{gatewayConfigFile}' cannot be found.");

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(file.Directory!.FullName)
                .AddJsonFile(file.Name, optional: false, reloadOnChange: false);

            var config = configBuilder.Build();

            // update the upstream port
            var upstreamPortStr = upstreamPort.ToStringInvariant();
            foreach (var section in config.GetSection("routes").GetChildren())
            {
                var to = section.GetSection("proxy:to");
                if (to.Exists())
                    to.Value = to.Value.ReplaceOrdinal("$upstream-port", upstreamPortStr);
            }

            return config;
        }

        private static string GetNginxConfigFile(string nginxConfigFile, int upstreamPort)
        {
            var config = File.ReadAllText(nginxConfigFile)
                .ReplaceOrdinal("$upstream-port", upstreamPort.ToStringInvariant())
                .ReplaceOrdinal("\r\n", "\n");

            var path = Path.GetTempFileName();
            File.WriteAllText(path, config, Encoding.Latin1);
            return path;
        }

        private static int GetAvailablePort()
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

            var p = port;
            do { p = Interlocked.Increment(ref port); }
            while (listeners.Any(ip => ip.Port == p));

            return p;
        }
    }
}
