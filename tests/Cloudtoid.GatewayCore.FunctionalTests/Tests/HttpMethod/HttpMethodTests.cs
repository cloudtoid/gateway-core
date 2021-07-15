using System;
using System.Net.Http;
using System.Net.Http.Json;
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

        [TestMethod("GET: Should return 200")]
        public async Task HttpGetTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, "get200?message=test"),
                async (nginxResponse, response) =>
                {
                    await EnsureSuccessAsync(nginxResponse);
                    await EnsureSuccessAsync(response);
                });
        }

        [TestMethod("GET: Should return 500 and content body")]
        public async Task HttpGet500TestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, "get500?message=test"),
                async (nginxResponse, response) =>
                {
                    nginxResponse.StatusCode.Should().Be(500);
                    var result = await nginxResponse.Content.ReadAsStringAsync();
                    result.Should().Be("test");

                    response.StatusCode.Should().Be(500);
                    result = await response.Content.ReadAsStringAsync();
                    result.Should().Be("test");
                });
        }

        [TestMethod("POST: Should return 200")]
        public async Task HttpPostTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Post, "post200") { Content = JsonContent.Create("test") },
                async (nginxResponse, response) =>
                {
                    await EnsureSuccessAsync(nginxResponse);
                    await EnsureSuccessAsync(response);
                });
        }

        [TestMethod("POST: Should return 500 and content body")]
        public async Task HttpPost500TestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Post, "post500") { Content = JsonContent.Create("test") },
                async (nginxResponse, response) =>
                {
                    nginxResponse.IsSuccessStatusCode.Should().BeFalse();
                    (await nginxResponse.Content.ReadAsStringAsync()).Should().Be("test");

                    response.IsSuccessStatusCode.Should().BeFalse();
                    (await response.Content.ReadAsStringAsync()).Should().Be("test");
                });
        }

        [TestMethod("DELETE: Should return 200")]
        public async Task HttpDeleteTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Delete, "delete200?message=test"),
                async (nginxResponse, response) =>
                {
                    await EnsureSuccessAsync(nginxResponse);
                    await EnsureSuccessAsync(response);
                });
        }

        [TestMethod("DELETE: Should return 500 and content body")]
        public async Task HttpDelete500TestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Delete, "delete500?message=test"),
                async (nginxResponse, response) =>
                {
                    nginxResponse.StatusCode.Should().Be(500);
                    var result = await nginxResponse.Content.ReadAsStringAsync();
                    result.Should().Be("test");

                    response.StatusCode.Should().Be(500);
                    result = await response.Content.ReadAsStringAsync();
                    result.Should().Be("test");
                });
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
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
