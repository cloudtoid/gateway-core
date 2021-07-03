using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    internal static class TestExecutor
    {
        private static volatile int port = 5000;

        internal static async Task ExecuteAsync(
            string gatewayConfigFile,
            HttpRequestMessage request,
            Func<HttpResponseMessage, Task> responseValidator)
        {
            var proxyPort = Interlocked.Increment(ref port);
            var upstreamPort = Interlocked.Increment(ref port);

            var gatewayConfig = LoadGatewayConfig(gatewayConfigFile);
            gatewayConfig = UpdateUpstreamPort(gatewayConfig, upstreamPort);

            await using var pipeline = await StartPipelineAsync(proxyPort, upstreamPort, gatewayConfig);
            using var httpClient = CreateHttpClient(proxyPort);
            using var response = await httpClient.SendAsync(request);
            await responseValidator(response);
        }

        private static HttpClient CreateHttpClient(int port) => new()
        {
            BaseAddress = new Uri($"http://localhost:{port}/api/"),
            DefaultRequestVersion = new Version(2, 0),
        };

        private static async Task<Pipeline> StartPipelineAsync(int proxyPort, int upstreamPort, IConfiguration proxyConfig)
        {
            var pipeline = new Pipeline(proxyPort, upstreamPort, proxyConfig);
            await pipeline.StartAsync();
            return pipeline;
        }

        private static IConfiguration UpdateUpstreamPort(IConfiguration config, int upstreamPort)
        {
            var upstreamPortStr = upstreamPort.ToStringInvariant();
            foreach (var section in config.GetSection("routes").GetChildren())
            {
                var to = section.GetSection("proxy:to");
                if (to.Exists())
                    to.Value = to.Value.ReplaceOrdinal("$upstream-port", upstreamPortStr);
            }

            return config;
        }

        private static IConfiguration LoadGatewayConfig(string gatewayConfigFile)
        {
            var file = new FileInfo("Tests/Header/ProxyOptions/" + gatewayConfigFile);
            if (!file.Exists)
                throw new FileNotFoundException($"File '{gatewayConfigFile}' cannot be found.");

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(file.Directory!.FullName)
                .AddJsonFile(file.Name, optional: false, reloadOnChange: false);

            return configBuilder.Build();
        }
    }
}
