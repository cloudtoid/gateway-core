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
                    headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "2.0 gwcore" });
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
                    headers.Contains("x-c-custom").Should().BeTrue();
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
                    headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "2.0 gwcore" });
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
                    headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "2.0 custom-proxy" });
                });
        }

        [TestMethod("Should have a via header with two values")]
        public async Task ViaTwoProxiesTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "viaTwoProxies?message=test");
            request.Headers.Via.Add(new ViaHeaderValue("1.1", "first-leg"));
            await executor.ExecuteAsync(
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "1.1 first-leg", "2.0 gwcore" });
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

        [TestMethod("Should not have forwarded or x-forwarded headers")]
        public async Task NoForwardedTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "noForwarded?message=test");
            request.Headers.Add(Constants.Forwarded, "for=192.0.2.60;proto=http;by=203.0.113.43;host=test");
            request.Headers.Add(Constants.XForwardedFor, "some-for");
            request.Headers.Add(Constants.XForwardedHost, "some-host");
            request.Headers.Add(Constants.XForwardedProto, "some-proto");

            await executor.ExecuteAsync(
                "NoXForwardedTestOptions.json",
                request,
                response => EnsureResponseSucceededAsync(response));

            request = new HttpRequestMessage(HttpMethod.Get, "noForwarded?message=test");
            request.Headers.Add(Constants.Forwarded, "for=192.0.2.60;proto=http;by=203.0.113.43;host=test");
            request.Headers.Add(Constants.XForwardedFor, "some-for");
            request.Headers.Add(Constants.XForwardedHost, "some-host");
            request.Headers.Add(Constants.XForwardedProto, "some-proto");

            await executor.ExecuteAsync(
                "NoForwardedTestOptions.json",
                request,
                response => EnsureResponseSucceededAsync(response));
        }

        [TestMethod("Should have a Forwarded header that includes earlier forwarded and x-forwarded header values")]
        public async Task ForwardedMultiProxiesTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "forwardedMultiProxies?message=test");
            request.Headers.Add(Constants.Forwarded, "for=192.0.2.60;proto=http;by=203.0.113.43;host=test, for=192.0.2.12;proto=https;by=203.0.113.43;host=test2");
            request.Headers.Add(Constants.XForwardedFor, "some-for");
            request.Headers.Add(Constants.XForwardedHost, "some-host");
            request.Headers.Add(Constants.XForwardedProto, "some-proto");

            await executor.ExecuteAsync(
                request,
                response => EnsureResponseSucceededAsync(response));
        }

        [TestMethod("Should have x-forwarded headers")]
        public async Task XForwardedTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "xForwarded?message=test");
            await executor.ExecuteAsync(
                "XForwardedTestOptions.json",
                request,
                response => EnsureResponseSucceededAsync(response));
        }

        [TestMethod("Should have x-forwarded headers that include earlier forwarded and x-forwarded header values")]
        public async Task XForwardedMultiProxiesTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "xForwardedMultiProxies?message=test");
            request.Headers.Add(Constants.Forwarded, "for=192.0.2.60;proto=http;by=203.0.113.43;host-test, for=[1020:3040:5060:7080:9010:1112:1314:1516]:10;proto=https;by=203.0.113.43;host=test2");
            request.Headers.Add(Constants.XForwardedFor, "some-for");
            request.Headers.Add(Constants.XForwardedHost, "some-host");
            request.Headers.Add(Constants.XForwardedProto, "some-proto");

            await executor.ExecuteAsync(
                "XForwardedTestOptions.json",
                request,
                response => EnsureResponseSucceededAsync(response));
        }

        [TestMethod("Should have a set-cookie header with modified values")]
        public async Task SetCookieTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "setCookie?message=test");
            await executor.ExecuteAsync(
                "SetCookieTestOptions.json",
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.GetValues(HeaderNames.SetCookie)
                        .Should()
                        .BeEquivalentTo(
                            new[]
                            {
                                "sessionId=1234; expires=Tue, 01 Jan 2030 01:01:01 GMT; domain=new.com; path=/; secure; samesite=lax; httponly",
                                "pxeId=exp12; domain=default.com; path=/; samesite=none",
                                "emptyOut=empty; path=/",
                            });
                });
        }

        private static async Task EnsureResponseSucceededAsync(HttpResponseMessage response)
        {
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Be("test");
        }

        // Tests
        // - Fix HTTPS so it also works on Mac and Linus!
        // - All HTTP methods (POST, DELETE, etc)
        // - HttpClientName
        // - Routing
        // - Failed HTTP requests with and without content/body
        // - Expression evaluations
        // - Timeout (both at httpClient to upstream and inside of proxy)
        // - Auto redirects
        // - ProxyException and exception handling
        // - When no route is found, do not return 200
        // - Extra (unknown) request and response headers are just forwarded
        // - Authentication
        // - Test all known headers and their behavior
    }
}
