namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class TestExecutor
    {
        private const string ProxyProcessName = "cloudtoid.gatewaycore.cli";
        private static readonly string ProxyFile = ProxyProcessName + ".exe";
        private static readonly Range ProxyPortRange = new Range(85, 185);
        private static readonly int UpstreamPortStartIndex = ProxyPortRange.End.Value + 1;
        private static readonly ConcurrentStack<HttpClient> HttpClients = new ConcurrentStack<HttpClient>();

        static TestExecutor()
        {
            for (int i = ProxyPortRange.Start.Value; i < ProxyPortRange.End.Value; i++)
                HttpClients.Push(CreateHttpClient(i));

            var processes = Process.GetProcessesByName(ProxyProcessName);
            foreach (var process in processes)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                }
            }
        }

        internal async Task ExecuteAsync(
            HttpRequestMessage request,
            Func<HttpResponseMessage, Task> responseValidator,
            string? proxyConfigFile = null)
        {
            var httpClient = await GetHttpClientAsync();
            try
            {
                var proxyPort = httpClient.BaseAddress.Port;
                var upstreamPort = UpstreamPortStartIndex + proxyPort - ProxyPortRange.Start.Value;
                using (var proxyProcess = StartProxyServer(proxyPort, upstreamPort, proxyConfigFile))
                {
                    try
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
                    finally
                    {
                        try
                        {
                            proxyProcess.Kill(true);
                        }
                        catch
                        {
                        }
                    }
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

        private static Process StartProxyServer(int proxyPort, int upstreamPort, string? proxyConfigFile)
        {
            var args = new StringBuilder("functional-test")
                .Append(" -pp ").Append(proxyPort)
                .Append(" -up ").Append(upstreamPort);

            if (!string.IsNullOrEmpty(proxyConfigFile))
                args.Append(" -c ").Append(proxyConfigFile);

            var startInfo = new ProcessStartInfo(ProxyFile, args.ToString())
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            using (var signal = new ManualResetEventSlim(false))
            {
                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null && e.Data.ContainsOrdinalIgnoreCase("CLI is running."))
                        signal.Set();
                };

                if (!process.Start())
                    throw new InvalidOperationException($"{ProxyFile} failed to start.");

                process.BeginOutputReadLine();

                // Wait 3 seconds!
                for (int i = 0; i < 30; i++)
                {
                    if (signal.Wait(100))
                        break;

                    if (process.HasExited)
                        throw new InvalidOperationException($"{ProxyFile} exited with code {process.ExitCode}.");
                }

                if (!signal.IsSet)
                    throw new InvalidOperationException($"{ProxyFile} took much longer than expected to start.");
            }

            return process;
        }
    }
}
