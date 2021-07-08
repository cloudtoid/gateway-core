using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Method = System.Net.Http.HttpMethod;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [TestClass]
    public sealed class HttpStatusTests
    {
        private static Pipeline? pipeline;

        [TestMethod("Basic HTTP status plumbing test")]
        public async Task BasicPlumbingTestAsync()
        {
            var activity = new Activity("BasicProxy").Start();

            try
            {
                await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, "basic?message=test"),
                async (nginxResponse, response) =>
                {
                    await EnsureResponseSucceededAsync(nginxResponse);
                    await EnsureResponseSucceededAsync(response);

                    response.Content.Headers.ContentType
                        .Should().Be(nginxResponse.Content.Headers.ContentType);
                });
            }
            finally
            {
                activity.Stop();
            }
        }

        [TestMethod("Should not return success when route doesn't exist.")]
        public async Task NoRouteTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, "noRoute?message=test"),
                (nginxResponse, response) =>
                {
                    response.IsSuccessStatusCode
                        .Should().BeFalse();

                    response.StatusCode
                        .Should().Be(nginxResponse.StatusCode)
                        .And.Be(HttpStatusCode.NotFound);

                    response.Content.Headers.ContentLength
                        .Should().Be(nginxResponse.Content.Headers.ContentLength);

                    return Task.CompletedTask;
                });

            ////while (true)
            ////    await Task.Delay(1000);
        }

        private static async Task EnsureResponseSucceededAsync(HttpResponseMessage response)
        {
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Be("test");
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
                "Tests/HttpStatus/GatewayCoreOptions/default.json",
                "Tests/HttpStatus/NginxConfigs/default.conf");

            await pipeline.StartAsync();

            // need to wait for nginx to get up and running in the docker container.
            await Task.Delay(1000);
        }

        [ClassCleanup]
        public static async Task CleanupAsync()
            => await pipeline!.DisposeAsync();
    }
}
