namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;

    internal sealed class TestExecutor
    {
        private static readonly Range ProxyPortRange = new Range(85, 185);
        private static readonly int UpstreamPortStartIndex = ProxyPortRange.End.Value + 1;
        private static readonly ConcurrentStack<HttpClient> HttpClients = new ConcurrentStack<HttpClient>();

        static TestExecutor()
        {
            for (int i = ProxyPortRange.Start.Value; i < ProxyPortRange.End.Value; i++)
                HttpClients.Push(CreateHttpClient(i));
        }

        internal Task ExecuteAsync(
            string gatewayConfigFile,
            HttpRequestMessage request,
            Func<HttpResponseMessage, Task> responseValidator)
        {
            return ExecuteAsync(
                request,
                responseValidator,
                LoadGatewayConfig(gatewayConfigFile));
        }

        internal async Task ExecuteAsync(
            HttpRequestMessage request,
            Func<HttpResponseMessage, Task> responseValidator,
            IConfiguration? gatewayConfig = null)
        {
            var httpClient = await GetHttpClientAsync();
            try
            {
                var proxyPort = httpClient.BaseAddress.Port;
                var upstreamPort = UpstreamPortStartIndex + proxyPort - ProxyPortRange.Start.Value;

                gatewayConfig = gatewayConfig is null
                    ? GetDefaultOptions(upstreamPort)
                    : UpdateUpstreamPort(gatewayConfig, upstreamPort);

                await using (var pipeline = await StartPipelineAsync(proxyPort, upstreamPort, gatewayConfig))
                {
                    HttpResponseMessage response;
                    try
                    {
                        response = await httpClient.SendAsync(request);
                    }
                    catch
                    {
                        using (var temp = httpClient)
                            httpClient = CreateHttpClient(httpClient.BaseAddress.Port);

                        throw;
                    }

                    using (response)
                        await responseValidator(response);
                }
            }
            finally
            {
                HttpClients.Push(httpClient);
            }
        }

        private static async Task<HttpClient> GetHttpClientAsync()
        {
            if (!HttpClients.TryPop(out var client) || client is null)
                await Task.Delay(10);

            return client!;
        }

        private static HttpClient CreateHttpClient(int port)
        {
            return new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{port}/api/"),
                DefaultRequestVersion = new Version(2, 0),
            };
        }

        private static async Task<Pipeline> StartPipelineAsync(int proxyPort, int upstreamPort, IConfiguration proxyConfig)
        {
            var pipeline = new Pipeline(proxyPort, upstreamPort, proxyConfig);
            await pipeline.StartAsync();
            return pipeline;
        }

        private static IConfiguration GetDefaultOptions(int upstreamPort)
        {
            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["routes:/api/:proxy:to"] = $"http://localhost:{upstreamPort}/upstream/"
            };

            return new ConfigurationBuilder().AddInMemoryCollection(options).Build();
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
            var file = new FileInfo("Tests/Options/" + gatewayConfigFile);
            if (!file.Exists)
                throw new FileNotFoundException($"File '{gatewayConfigFile}' cannot be found.");

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(file.Directory.FullName)
                .AddJsonFile(file.Name, optional: false, reloadOnChange: false);

            return configBuilder.Build();
        }
    }
}
