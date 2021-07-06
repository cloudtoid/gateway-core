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
        private readonly string containerName;
        private readonly int port;
        private readonly DockerClient client;

        internal NginxServer(string configFile, int port)
        {
            EnsureDockerIsRunning();
            this.configFile = configFile;
            this.port = port;
            client = new DockerClientConfiguration().CreateClient();
            containerName = "nginx-gwcore-" + port;
        }

        public ValueTask DisposeAsync() => StopAsync();

        internal async Task StartAsync()
        {
            await client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = Image },
                new(),
                new Progress<JSONMessage>());

            await RemoveContainerAsync();

            var portStr = port.ToStringInvariant();

            var response = await client.Containers.CreateContainerAsync(
                new CreateContainerParameters
                {
                    Image = Image,
                    Name = containerName,
                    HostConfig = new()
                    {
                        Mounts = new Mount[]
                        {
                            new()
                            {
                                Type = "bind",
                                Source = configFile,
                                Target = "/etc/nginx/nginx.conf",
                                ReadOnly = true
                            }
                        },
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            ["80/tcp"] = new PortBinding[]
                            {
                                new() { HostPort = portStr }
                            }
                        },
                    }
                });

            await client.Containers.StartContainerAsync(response.ID, null);
        }

        internal async ValueTask StopAsync()
        {
            await RemoveContainerAsync();
            client.Dispose();
        }

        private static void EnsureDockerIsRunning()
        {
            if (!Process.GetProcesses().Any(p => DockerProcessNames.Contains(p.ProcessName)))
                throw new InvalidOperationException("Docker desktop is not running!");
        }

        private async Task RemoveContainerAsync()
        {
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { [containerName] = true },
                    ["ancestor"] = new Dictionary<string, bool> { [Image] = true }
                }
            });

            if (containers.Count > 0)
            {
                var container = containers[0];
                await client.Containers.RemoveContainerAsync(container.ID, new() { Force = true });
            }
        }
    }
}
