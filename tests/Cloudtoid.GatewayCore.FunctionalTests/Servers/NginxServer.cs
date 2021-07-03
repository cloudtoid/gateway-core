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
        private readonly DockerClient client;
        private string? containerId;

        public NginxServer()
        {
            var running = Process.GetProcesses().Any(
                p => p.ProcessName.EqualsOrdinalIgnoreCase("docker")
                || p.ProcessName.EqualsOrdinalIgnoreCase("dockerd"));

            if (!running)
                throw new InvalidOperationException("Docker is not running!");

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
                new AuthConfig(),
                new Progress<JSONMessage>());

            var response = await client.Containers.CreateContainerAsync(
                new CreateContainerParameters
                {
                    Image = Image,
                    Name = "nginx-gwcore",
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        ["8000"] = default
                    }
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
    }
}
