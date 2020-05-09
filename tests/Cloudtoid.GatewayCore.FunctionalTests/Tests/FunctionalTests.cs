namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class FunctionalTests
    {
        private readonly TestExecutor executor = new TestExecutor();

        [TestMethod("Basic plumbing")]
        public async Task BasicPlumbingTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "echo?message=test");
            await executor.ExecuteAsync(
                request,
                async response =>
                {
                    response.IsSuccessStatusCode.Should().BeTrue();
                    var result = await response.Content.ReadAsStringAsync();
                    result.Should().Be("test");

                    var headers = response.Headers;
                    headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "1.1 gwcore" });
                    headers.Contains(Constants.CorrelationId).Should().BeFalse();
                    headers.Contains(Constants.CallId).Should().BeFalse();

                    var contentHeaders = response.Content.Headers;
                    contentHeaders.ContentType.MediaType.Should().Be("text/plain");
                    contentHeaders.ContentType.CharSet.Should().Be("utf-8");
                    contentHeaders.ContentLength.Should().Be(4);
                });
        }

        [TestMethod("Should have correlation id and call id headers on both request and response")]
        public async Task TraceTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "trace?message=test");
            await executor.ExecuteAsync(
                "TraceTestOptions.json",
                request,
                async response =>
                {
                    response.IsSuccessStatusCode.Should().BeTrue();
                    var result = await response.Content.ReadAsStringAsync();
                    result.Should().Be("test");

                    var headers = response.Headers;
                    headers.Contains(Constants.CorrelationId).Should().BeTrue();
                    headers.Contains(Constants.CallId).Should().BeTrue();
                });
        }

        [TestMethod("Should have correlation id and call id headers on response but not request")]
        public async Task NoTraceTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "noTrace?message=test");
            await executor.ExecuteAsync(
                "NoTraceTestOptions.json",
                request,
                async response =>
                {
                    response.IsSuccessStatusCode.Should().BeTrue();
                    var result = await response.Content.ReadAsStringAsync();
                    result.Should().Be("test");

                    var headers = response.Headers;
                    headers.Contains(Constants.CorrelationId).Should().BeTrue();
                    headers.Contains(Constants.CallId).Should().BeTrue();
                });
        }

        [TestMethod("Should have a custom correlation id header on both request and response")]
        public async Task CorrelationIdTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "customCorrelationId?message=test");
            await executor.ExecuteAsync(
                "CustomCorrelationIdTestOptions.json",
                request,
                async response =>
                {
                    response.IsSuccessStatusCode.Should().BeTrue();
                    var result = await response.Content.ReadAsStringAsync();
                    result.Should().Be("test");

                    var headers = response.Headers;
                    headers.Contains("x-cor-custom").Should().BeTrue();
                });
        }

        // Tests
        // - All HTTP methods (POST, DELETE, etc)
        // - "Forwarded" headers
        // - Routing
        // - Failed HTTP requests with and without content/body
        // - Expression evaluations
        // - Timeout (both at httpClient to upstream and inside of proxy)
        // - Auto redirects
        // - ProxyException and exception handling
        // - When no route is found, do not return 200
        // - End to end tracing
        // - Extra (unknown) request and response headers are just forwarded
        // - Cookies (domain/host specific ones too)
        // - Authentication
        // - Test all known headers and their behavior
    }
}
