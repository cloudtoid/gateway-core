using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Method = System.Net.Http.HttpMethod;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [TestClass]
    public sealed class FunctionalTests
    {
        private readonly TestExecutor executor = new();

        [TestMethod("Basic plumbing")]
        public async Task BasicPlumbingTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "echo?message=test");
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
                    contentHeaders.ContentType.Should().NotBeNull();
                    contentHeaders.ContentType.MediaType.Should().Be("text/plain");
                    contentHeaders.ContentType.CharSet.Should().Be("utf-8");
                    contentHeaders.ContentLength.Should().Be(4);
                });
        }

        [TestMethod("Should have correlation id and call id headers on request but not response")]
        public async Task DefaultTraceTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "trace?message=test");
            await executor.ExecuteAsync(
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.Contains(Constants.CorrelationId).Should().BeFalse();
                    headers.Contains(Constants.CallId).Should().BeFalse();
                });
        }

        [TestMethod("Should have correlation id and call id headers on both request and response")]
        public async Task TraceTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "trace?message=test");
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
            var request = new HttpRequestMessage(Method.Get, "noTrace?message=test");
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
            var request = new HttpRequestMessage(Method.Get, "customCorrelationId?message=test");
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
            var request = new HttpRequestMessage(Method.Get, "originalCorrelationId?message=test");
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
            var request = new HttpRequestMessage(Method.Get, "callId?message=test");
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

        [TestMethod("Should have server header that ignores the upstream server header")]
        public async Task ServerTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "server?message=test");
            await executor.ExecuteAsync(
                "ServerTestOptions.json",
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.TryGetValues(HeaderNames.Server, out var values).Should().BeTrue();
                    values.Should().BeEquivalentTo(new[] { "gwcore" });
                });
        }

        [TestMethod("Should not have a server header")]
        public async Task NoServerTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "server?message=test");
            await executor.ExecuteAsync(
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.TryGetValues(HeaderNames.Server, out var values).Should().BeFalse();
                });
        }

        [TestMethod("Should have an external address header (x-gwcore-external-address)")]
        public async Task ExternalAddressTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "externalAddress?message=test");
            await executor.ExecuteAsync(
                "ExternalAddressTestOptions.json",
                request,
                response => EnsureResponseSucceededAsync(response));
        }

        [TestMethod("Should not have an external address header (x-gwcore-external-address)")]
        public async Task NoExternalAddressTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "noExternalAddress?message=test");
            await executor.ExecuteAsync(
                request,
                response => EnsureResponseSucceededAsync(response));
        }

        [TestMethod("Should have a via header on both request and response")]
        public async Task ViaTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "via?message=test");
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
            var request = new HttpRequestMessage(Method.Get, "noVia?message=test");
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
            var request = new HttpRequestMessage(Method.Get, "viaCustomProxyName?message=test");
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
            var request = new HttpRequestMessage(Method.Get, "viaTwoProxies?message=test");
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

        [TestMethod("Should have a forwarded header")]
        public async Task ForwardedTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "forwarded?message=test");
            await executor.ExecuteAsync(
                request,
                response => EnsureResponseSucceededAsync(response));
        }

        [TestMethod("Should not have forwarded or x-forwarded headers")]
        public async Task NoForwardedTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "noForwarded?message=test");
            request.Headers.Add(Constants.Forwarded, "for=192.0.2.60;proto=http;by=203.0.113.43;host=test");
            request.Headers.Add(Constants.XForwardedFor, "some-for");
            request.Headers.Add(Constants.XForwardedHost, "some-host");
            request.Headers.Add(Constants.XForwardedProto, "some-proto");

            await executor.ExecuteAsync(
                "NoXForwardedTestOptions.json",
                request,
                response => EnsureResponseSucceededAsync(response));

            request = new HttpRequestMessage(Method.Get, "noForwarded?message=test");
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
            var request = new HttpRequestMessage(Method.Get, "forwardedMultiProxies?message=test");
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
            var request = new HttpRequestMessage(Method.Get, "xForwarded?message=test");
            await executor.ExecuteAsync(
                "XForwardedTestOptions.json",
                request,
                response => EnsureResponseSucceededAsync(response));
        }

        [TestMethod("Should have x-forwarded headers that include earlier forwarded and x-forwarded header values")]
        public async Task XForwardedMultiProxiesTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "xForwardedMultiProxies?message=test");
            request.Headers.Add(Constants.Forwarded, "for=192.0.2.60;proto=http;by=203.0.113.43;host-test, for=[1020:3040:5060:7080:9010:1112:1314:1516]:10;proto=https;by=203.0.113.43;host=test2");
            request.Headers.Add(Constants.XForwardedFor, "some-for");
            request.Headers.Add(Constants.XForwardedHost, "some-host");
            request.Headers.Add(Constants.XForwardedProto, "some-proto");

            await executor.ExecuteAsync(
                "XForwardedTestOptions.json",
                request,
                response => EnsureResponseSucceededAsync(response));
        }

        [TestMethod("Should have set-cookie headers with modified values")]
        public async Task SetCookieTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "setCookie?message=test");
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

        [TestMethod("Should have added override headers")]
        public async Task AddOverrideTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "addOverride?message=test");
            await executor.ExecuteAsync(
                "AddOverridesTestOptions.json",
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.GetValues(Constants.OneValue).Should().BeEquivalentTo(new[] { "one" });
                    headers.GetValues(Constants.TwoValues).Should().BeEquivalentTo(new[] { "one", "two" });
                    headers.GetValues(Constants.Expression).Should().BeEquivalentTo(new[] { Environment.MachineName + "/gwcore", "m:GET" });
                });
        }

        [TestMethod("Should have modified override headers")]
        public async Task UpdateOverrideTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "updateOverride?message=test");
            request.Headers.Add(Constants.OneValue, "one");
            request.Headers.Add(Constants.TwoValues, new[] { "one", "two" });
            request.Headers.Add(Constants.Expression, new[] { "one", "two" });

            await executor.ExecuteAsync(
                "UpdateOverridesTestOptions.json",
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.GetValues(Constants.OneValue).Should().BeEquivalentTo(new[] { "one-updated" });
                    headers.GetValues(Constants.TwoValues).Should().BeEquivalentTo(new[] { "one-updated", "two-updated" });
                    headers.GetValues(Constants.Expression).Should().BeEquivalentTo(new[] { Environment.MachineName + "/gwcore", "m:GET" });
                });
        }

        [TestMethod("Should not have discarded, empty, or headers with underscore")]
        public async Task DiscardTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "discard?message=test");
            request.Headers.Add(Constants.OneValue, "one");
            request.Headers.Add(Constants.TwoValues, new[] { "one", "two" });
            request.Headers.Add(Constants.ThreeValues, new[] { "one", "two", "three" });
            request.Headers.Add(Constants.Underscore, "one");
            request.Headers.Add(Constants.Expression, string.Empty);

            await executor.ExecuteAsync(
                "DiscardTestOptions.json",
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.Contains(Constants.OneValue).Should().BeFalse();
                    headers.Contains(Constants.TwoValues).Should().BeFalse();
                    headers.Contains(Constants.ThreeValues).Should().BeTrue();
                    headers.Contains(Constants.Underscore).Should().BeFalse();
                    headers.Contains(Constants.Expression).Should().BeFalse();
                });
        }

        [TestMethod("Should not keep inbound headers")]
        public async Task DiscardInboundTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "discardInbound?message=test");
            request.Headers.Add(Constants.OneValue, "one");
            request.Headers.Add(Constants.TwoValues, new[] { "one", "two" });
            request.Headers.Add(Constants.ThreeValues, new[] { "one", "two", "three" });

            await executor.ExecuteAsync(
                "DiscardInboundTestOptions.json",
                request,
                async response =>
                {
                    await EnsureResponseSucceededAsync(response);

                    var headers = response.Headers;
                    headers.Contains(Constants.OneValue).Should().BeFalse();
                    headers.Contains(Constants.TwoValues).Should().BeFalse();
                    headers.Contains(Constants.ThreeValues).Should().BeFalse();
                });
        }

        private static async Task EnsureResponseSucceededAsync(HttpResponseMessage response)
        {
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Be("test");
        }
    }
}
