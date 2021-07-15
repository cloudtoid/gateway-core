using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Method = System.Net.Http.HttpMethod;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [TestClass]
    public sealed class HttpMethodTests
    {
        private static Pipeline? pipeline;

        [TestMethod("Basic HTTP status plumbing test")]
        public async Task BasicPlumbingTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, "echo?message=test"),
                (nginxResponse, response) =>
                {
                    nginxResponse.IsSuccessStatusCode.Should().BeTrue();
                    response.IsSuccessStatusCode.Should().BeTrue();

                    response.Content.Headers.ContentType
                        .Should().Be(nginxResponse.Content.Headers.ContentType);

                    return Task.CompletedTask;
                });
        }

        private static async Task ExecuteAsync(
            Func<HttpRequestMessage> requestFactory,
            Func<HttpResponseMessage, HttpResponseMessage, Task> responseValidator)
        {
            using var nginexHttpClient = pipeline!.CreateNginxClient();
            using var gatewayCoreHttpClient = pipeline.CreateGatewayCoreClient();

            using var nginxRequest = requestFactory();
            using var gatewayCoreRequest = requestFactory();

            var nginxTask = nginexHttpClient.SendAsync(nginxRequest);
            var gatewayCoreTask = gatewayCoreHttpClient.SendAsync(gatewayCoreRequest);

            using var nginxResponse = await nginxTask;
            using var gatewayCoreResponse = await gatewayCoreTask;
            await responseValidator(nginxResponse, gatewayCoreResponse);
        }

        [ClassInitialize]
        public static async Task InitializeAsync(TestContext testContext)
        {
            if (testContext is null)
                throw new ArgumentNullException(nameof(testContext));

            pipeline = new Pipeline(
                "Tests/HttpMethod/GatewayCoreOptions/default.json",
                "Tests/HttpMethod/NginxConfigs/default.conf");

            await pipeline.StartAsync();

            // need to wait for nginx to get up and running in the docker container.
            await Task.Delay(1000);
        }

        [ClassCleanup]
        public static async Task CleanupAsync()
            => await pipeline!.DisposeAsync();
    }
}
