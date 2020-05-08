namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class TestExecutor : IDisposable
    {
        private const string ProxyFile = "cloudtoid.gatewaycore.cli.exe";
        private Process? proxyProcess;

        ~TestExecutor()
        {
            Dispose();
        }

        internal Task ExecuteAsync(string? proxyConfigFile = null)
        {
            proxyProcess = StartProxyServer(proxyConfigFile);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (proxyProcess == null)
                return;

            try
            {
                proxyProcess.Kill();
            }
            catch
            {
            }

            try
            {
                proxyProcess.Dispose();
            }
            catch
            {
            }

            proxyProcess = null;
        }

        private static Process StartProxyServer(string? proxyConfigFile)
        {
            var args = new StringBuilder("functional-test");

            if (!string.IsNullOrEmpty(proxyConfigFile))
                args.AppendSpace().Append("-c ").Append(proxyConfigFile);

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
                    if (e.Data.ContainsOrdinalIgnoreCase("CLI is running."))
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
