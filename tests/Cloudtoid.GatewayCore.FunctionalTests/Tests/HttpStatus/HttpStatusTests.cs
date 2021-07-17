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
        private const string Value = "test";
        private static Pipeline? pipeline;

        [TestMethod("Basic HTTP status plumbing test")]
        public async Task BasicPlumbingTestAsync()
        {
            using var activity = new Activity("BasicProxy").Start();
            activity.TraceStateString = "some-state";

            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, $"basic?message={Value}"),
                async (nginxResponse, response) =>
                {
                    await EnsureSuccessAsync(nginxResponse);
                    await EnsureSuccessAsync(response);

                    response.Content.Headers.ContentType
                        .Should().Be(nginxResponse.Content.Headers.ContentType);
                });
        }

        [TestMethod("Should not return success when route doesn't exist.")]
        public async Task NoRouteTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, $"noRoute?message={Value}"),
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
        }

        [TestMethod("Should redirect with 302-Found")]
        public async Task RedirectTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, $"redirect?message={Value}"),
                async (nginxResponse, response) =>
                {
                    await EnsureSuccessAsync(nginxResponse);
                    await EnsureSuccessAsync(response);
                });
        }

        [TestMethod("Should redirect with 301 - permanent")]
        public async Task RedirectPermanentTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, $"redirectPermanent?message={Value}"),
                async (nginxResponse, response) =>
                {
                    await EnsureSuccessAsync(nginxResponse);
                    await EnsureSuccessAsync(response);
                });
        }

        [TestMethod("Should redirect with 307 - temporary")]
        public async Task RedirectPreserveMethodTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, $"redirectPreserveMethod?message={Value}"),
                async (nginxResponse, response) =>
                {
                    await EnsureSuccessAsync(nginxResponse);
                    await EnsureSuccessAsync(response);
                });
        }

        [TestMethod("Should redirect with 308 - permanent and preserve method")]
        public async Task RedirectPermanentPreserveMethodTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, $"redirectPermanentPreserveMethod?message={Value}"),
                async (nginxResponse, response) =>
                {
                    await EnsureSuccessAsync(nginxResponse);
                    await EnsureSuccessAsync(response);
                });
        }

        [TestMethod("Should redirect with 300")]
        public async Task Redirect300TestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, $"redirect300?message={Value}"),
                (nginxResponse, response) =>
                {
                    nginxResponse.StatusCode.Should().Be(HttpStatusCode.Ambiguous);
                    response.StatusCode.Should().Be(HttpStatusCode.Ambiguous);
                    return Task.CompletedTask;
                });
        }

        [TestMethod("Should return not-modified HTTP status code (304)")]
        public async Task NotModifiedTestAsync()
        {
            await ExecuteAsync(
                () => new HttpRequestMessage(Method.Get, "notModified"),
                (nginxResponse, response) =>
                {
                    nginxResponse.StatusCode.Should().Be(HttpStatusCode.NotModified);
                    response.StatusCode.Should().Be(HttpStatusCode.NotModified);
                    return Task.CompletedTask;
                });
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Be(Value);
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
