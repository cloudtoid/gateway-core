namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
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
                    await EnsureResponseSucceededAsync(response);

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
                    await EnsureResponseSucceededAsync(response);

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
                    await EnsureResponseSucceededAsync(response);

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
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.Contains("x-cor-custom").Should().BeTrue();
                });
        }

        [TestMethod("Should boomerang the client's correlation id")]
        public async Task OriginalCorrelationIdTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "originalCorrelationId?message=test");
            request.Headers.Add(Constants.CorrelationId, "corr-id-1");
            await executor.ExecuteAsync(
                "OriginalCorrelationIdTestOptions.json",
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.TryGetValues(Constants.CorrelationId, out var values).Should().BeTrue();
                    values.Should().BeEquivalentTo(new[] { "corr-id-1" });
                });
        }

        [TestMethod("Should boomerang gateway's newly generated call id and drop the one provided on both request and response")]
        public async Task CallIdTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "callId?message=test");
            request.Headers.Add(Constants.CallId, "call-id-1");
            await executor.ExecuteAsync(
                "CallIdTestOptions.json",
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.TryGetValues("x-proxy-call-id", out var values).Should().BeTrue();
                    var callId = values.Single();

                    headers.TryGetValues(Constants.CallId, out values).Should().BeTrue();
                    values.Should().BeEquivalentTo(new[] { callId });
                });
        }

        [TestMethod("Should have a via header on both request and response")]
        public async Task ViaTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "via?message=test");
            await executor.ExecuteAsync(
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "1.1 gwcore" });
                });
        }

        [TestMethod("Should not have a via header on both request and response")]
        public async Task NoViaTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "noVia?message=test");
            await executor.ExecuteAsync(
                "NoViaTestOptions.json",
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.Contains(HeaderNames.Via).Should().BeFalse();
                });
        }

        [TestMethod("Should have a via header on both request and response with custom proxy name")]
        public async Task ViaCustomProxyNameTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "viaCustomProxyName?message=test");
            await executor.ExecuteAsync(
                "ViaCustomProxyNameTestOptions.json",
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "1.1 custom-proxy" });
                });
        }

        [TestMethod("Should have a via header with two values")]
        public async Task ViaTwoProxiesTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "viaTwoProxies?message=test");
            request.Headers.Via.Add(new ViaHeaderValue("2.0", "first-leg"));
            await executor.ExecuteAsync(
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "2.0 first-leg", "1.1 gwcore" });
                });
        }

        [TestMethod("Should have a Forwarded header")]
        public async Task ForwardedTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "forwarded?message=test");
            await executor.ExecuteAsync(
                request,
                response => EnsureResponseSucceededAsync(response));
        }

        // Forwarded Tests
        // - Forwarded with inbound X-Forwarded-*
        // - X-Forwarded-* with inbound X-Forwarded-*
        // - X-Forwarded-* with inbound Forwarded

        private static async Task EnsureResponseSucceededAsync(HttpResponseMessage response)
        {
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Be("test");
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
        // - Test a via header and HTTPS
    }
}
