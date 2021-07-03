using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    internal static class TestExecutor
    {
        internal static async Task ExecuteAsync(
            string gatewayConfigFile,
            HttpRequestMessage request,
            Func<HttpResponseMessage, Task> responseValidator)
        {
            await using var pipeline = new Pipeline(gatewayConfigFile);
            await pipeline.StartAsync();
            using var httpClient = pipeline.CreateGatewayCoreClient();
            using var response = await httpClient.SendAsync(request);
            await responseValidator(response);
        }
    }
}
