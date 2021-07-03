using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    internal sealed class NginxServer : IAsyncDisposable
    {
        private const string Image = "nginx:1.21";
        private static readonly IReadOnlySet<string> DockerProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "docker",
            "dockerd"
        };
        private readonly string configFile;
        private readonly int port;
        private readonly DockerClient client;
        private string? containerId;

        internal NginxServer(string configFile, int port)
        {
            EnsureDockerIsRunning();
            this.configFile = configFile;
            this.port = port;
            client = new DockerClientConfiguration().CreateClient();
        }

        public ValueTask DisposeAsync() => StopAsync();

        internal async Task StartAsync()
        {
            await client.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = Image,
                },
                null,
                null);

            var response = await client.Containers.CreateContainerAsync(
                new CreateContainerParameters
                {
                    Image = Image,
                    Name = "nginx-gwcore",
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        [port.ToStringInvariant() + ":80"] = default
                    },
                });

            containerId = response.ID;

            await client.Containers.StartContainerAsync(containerId, null);
        }

        internal async ValueTask StopAsync()
        {
            if (containerId is not null)
                await client.Containers.StopContainerAsync(containerId, null);

            client.Dispose();
        }

        private static void EnsureDockerIsRunning()
        {
            if (!Process.GetProcesses().Any(p => DockerProcessNames.Contains(p.ProcessName)))
                throw new InvalidOperationException("Docker desktop is not running!");
        }
    }
}
