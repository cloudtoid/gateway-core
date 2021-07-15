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
        public Task HttpGetTestAsync()
            => NoRequestBodySuccessTestAsync(Method.Get);

        [TestMethod("GET: Should return 500 and content body")]
        public Task HttpGet500TestAsync()
            => NoRequestBodyFailTestAsync(Method.Get);

        [TestMethod("DELETE: Should return 200")]
        public Task HttpDeleteTestAsync()
            => NoRequestBodySuccessTestAsync(Method.Delete);

        [TestMethod("DELETE: Should return 500 and content body")]
        public Task HttpDelete500TestAsync()
            => NoRequestBodyFailTestAsync(Method.Delete);

        [TestMethod("HEAD: Should return 200")]
        public Task HttpHeadTestAsync()
            => NoRequestBodySuccessTestAsync(Method.Head);

        [TestMethod("HEAD: Should return 500 and content body")]
        public Task HttpHead500TestAsync()
            => NoRequestBodyFailTestAsync(Method.Head);

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

        private static Task NoRequestBodySuccessTestAsync(Method method)
            => ExecuteAsync(
                () => new HttpRequestMessage(method, "200?message=test"),
                async (nginxResponse, response) =>
                {
                    var expectedContent = method == Method.Head ? string.Empty : "test";

                    await EnsureSuccessAsync(nginxResponse, expectedContent);
                    await EnsureSuccessAsync(response, expectedContent);
                });

        private static Task NoRequestBodyFailTestAsync(Method method)
            => ExecuteAsync(
                () => new HttpRequestMessage(method, "500?message=test"),
                async (nginxResponse, response) =>
                {
                    var expectedContent = method == Method.Head ? string.Empty : "test";

                    nginxResponse.IsSuccessStatusCode.Should().BeFalse();
                    (await nginxResponse.Content.ReadAsStringAsync()).Should().Be(expectedContent);

                    response.IsSuccessStatusCode.Should().BeFalse();
                    (await response.Content.ReadAsStringAsync()).Should().Be(expectedContent);
                });

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string expectedContent = "test")
        {
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Be(expectedContent);
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
