using System;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [ApiController]
    [Route("[controller]")]
    public class HeaderController : ControllerBase
    {
        private IHeaderDictionary RequestHeaders
            => HttpContext.Request.Headers;

        [HttpGet("echo")]
        public string Echo(string message)
            => message;

        [HttpGet("trace")]
        public string TraceTest(string message)
        {
            RequestHeaders.TryGetValue(Constants.CorrelationId, out var values).Should().BeTrue();
            values.Should().ContainSingle();

            RequestHeaders.TryGetValue(Constants.CallId, out values).Should().BeTrue();
            values.Should().ContainSingle();

            return message;
        }

        [HttpGet("noTrace")]
        public string NoTraceTest(string message)
        {
            RequestHeaders.ContainsKey(Constants.CorrelationId).Should().BeFalse();
            RequestHeaders.ContainsKey(Constants.CallId).Should().BeFalse();
            return message;
        }

        [HttpGet("customCorrelationId")]
        public string CustomCorrelationIdTest(string message)
        {
            RequestHeaders.TryGetValue("x-c-custom", out var values).Should().BeTrue();
            values.Should().ContainSingle();
            return message;
        }

        [HttpGet("originalCorrelationId")]
        public string OriginalCorrelationIdTest(string message)
        {
            RequestHeaders.TryGetValue(Constants.CorrelationId, out var values).Should().BeTrue();
            values.Should().BeEquivalentTo(new[] { "corr-id-1" });

            return message;
        }

        [HttpGet("callId")]
        public string CallIdTest(string message)
        {
            RequestHeaders.TryGetValue(Constants.CallId, out var values).Should().BeTrue();
            var callId = values.Single();
            callId.Should().NotBe("call-id-1");

            HttpContext.Response.Headers.Add("x-proxy-call-id", callId);
            HttpContext.Response.Headers.Add(Constants.CallId, "call-id-2");
            return message;
        }

        [HttpGet("server")]
        public string ServerTest(string message)
        {
            HttpContext.Response.Headers.Add(HeaderNames.Server, "some-server-name");
            return message;
        }

        [HttpGet("externalAddress")]
        public string ExternalAddressTest(string message)
        {
            RequestHeaders.TryGetValue(Constants.ExternalAddress, out var values).Should().BeTrue();
            values.Should().ContainSingle();
            return message;
        }

        [HttpGet("noExternalAddress")]
        public string NoExternalAddressTest(string message)
        {
            RequestHeaders.ContainsKey(Constants.ExternalAddress).Should().BeFalse();
            return message;
        }

        [HttpGet("via")]
        public string ViaTest(string message)
        {
            RequestHeaders.TryGetValue(HeaderNames.Via, out var values).Should().BeTrue();
            values.Should().ContainSingle().And.ContainMatch("?.? " + GatewayCore.Constants.ServerName);
            return message;
        }

        [HttpGet("noVia")]
        public string NoViaTest(string message)
        {
            RequestHeaders.ContainsKey(HeaderNames.Via).Should().BeFalse();
            return message;
        }

        [HttpGet("viaCustomProxyName")]
        public string ViaCustomProxyTest(string message)
        {
            RequestHeaders.TryGetValue(HeaderNames.Via, out var values).Should().BeTrue();
            values.Should().ContainSingle().And.ContainMatch("?.? custom-proxy");

            RequestHeaders.TryGetValue(Constants.ProxyName, out values).Should().BeTrue();
            values.Should().BeEquivalentTo(new[] { "custom-proxy" });

            return message;
        }

        [HttpGet("viaTwoProxies")]
        public string ViaTwoProxiesTest(string message)
        {
            var values = RequestHeaders.GetCommaSeparatedValues(HeaderNames.Via);
            values.Should().BeEquivalentTo(new[] { "1.1 first-leg", "1.1 " + GatewayCore.Constants.ServerName });

            HttpContext.Response.Headers.Add(HeaderNames.Via, "1.1 first-leg");
            return message;
        }

        [HttpGet("forwarded")]
        public string ForwardedTest(string message)
        {
            var values = RequestHeaders.GetCommaSeparatedValues(Constants.Forwarded);
            var forwarded = RemovePortFromHostInForwarded(values.Single());
            forwarded.Should().Be("by=\"[::1]\";for=\"[::1]\";host=localhost;proto=http");

            RequestHeaders.ContainsKey(Constants.XForwardedFor).Should().BeFalse();
            RequestHeaders.ContainsKey(Constants.XForwardedHost).Should().BeFalse();
            RequestHeaders.ContainsKey(Constants.XForwardedProto).Should().BeFalse();

            return message;
        }

        [HttpGet("noForwarded")]
        public string NoForwardedTest(string message)
        {
            RequestHeaders.ContainsKey(Constants.Forwarded).Should().BeFalse();
            RequestHeaders.ContainsKey(Constants.XForwardedFor).Should().BeFalse();
            RequestHeaders.ContainsKey(Constants.XForwardedHost).Should().BeFalse();
            RequestHeaders.ContainsKey(Constants.XForwardedProto).Should().BeFalse();

            return message;
        }

        [HttpGet("forwardedMultiProxies")]
        public string ForwardedMultiProxiesTest(string message)
        {
            var values = RequestHeaders.GetCommaSeparatedValues(Constants.Forwarded);
            values.Should().HaveCount(4);
            values[0].Should().Be("for=some-for;host=some-host;proto=some-proto");
            values[1].Should().Be("by=203.0.113.43;for=192.0.2.60;host=test;proto=http");
            values[2].Should().Be("by=203.0.113.43;for=192.0.2.12;host=test2;proto=https");
            RemovePortFromHostInForwarded(values[3]).Should().Be("by=\"[::1]\";for=\"[::1]\";host=localhost;proto=http");

            RequestHeaders.ContainsKey(Constants.XForwardedFor).Should().BeFalse();
            RequestHeaders.ContainsKey(Constants.XForwardedHost).Should().BeFalse();
            RequestHeaders.ContainsKey(Constants.XForwardedProto).Should().BeFalse();

            return message;
        }

        [HttpGet("xForwarded")]
        public string XForwardedTest(string message)
        {
            RequestHeaders.TryGetValue(Constants.XForwardedFor, out var values).Should().BeTrue();
            values.Single().Should().Be("::1");

            RequestHeaders.TryGetValue(Constants.XForwardedHost, out values).Should().BeTrue();
            values.Single().StartsWithOrdinalIgnoreCase("localhost").Should().BeTrue();

            RequestHeaders.TryGetValue(Constants.XForwardedProto, out values).Should().BeTrue();
            values.Single().Should().Be("http");

            RequestHeaders.ContainsKey(Constants.Forwarded).Should().BeFalse();

            return message;
        }

        [HttpGet("xForwardedMultiProxies")]
        public string XForwardedMultiProxiesTest(string message)
        {
            var values = RequestHeaders.GetCommaSeparatedValues(Constants.XForwardedFor);
            values.Should().BeEquivalentTo(new[] { "some-for", "192.0.2.60", "1020:3040:5060:7080:9010:1112:1314:1516", "::1" });

            values = RequestHeaders.GetCommaSeparatedValues(Constants.XForwardedHost);
            values.Should().BeEquivalentTo(new[] { "some-host" });

            values = RequestHeaders.GetCommaSeparatedValues(Constants.XForwardedProto);
            values.Should().BeEquivalentTo(new[] { "some-proto" });

            RequestHeaders.ContainsKey(Constants.Forwarded).Should().BeFalse();

            return message;
        }

        [HttpGet("setCookie")]
        public string SetCookieTest(string message)
        {
            HttpContext.Response.Cookies.Append(
                "sessionId",
                "1234",
                new CookieOptions
                {
                    Domain = "old.com",
                    Expires = new DateTimeOffset(2030, 1, 1, 1, 1, 1, TimeSpan.Zero),
                    HttpOnly = false,
                    Secure = false,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None
                });

            HttpContext.Response.Cookies.Append(
                "pxeId",
                "exp12",
                new CookieOptions
                {
                    Domain = "old.com",
                    HttpOnly = true,
                    Secure = true,
                });

            HttpContext.Response.Cookies.Append(
                "emptyOut",
                "empty",
                new CookieOptions
                {
                    Domain = "old.com",
                    HttpOnly = true,
                    Secure = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax
                });

            return message;
        }

        [HttpGet("append")]
        public string AppendHeaderTest(string message)
        {
            var values = RequestHeaders.GetCommaSeparatedValues(Constants.OneValue);
            values.Should().BeEquivalentTo(new[] { "one" });

            values = RequestHeaders.GetCommaSeparatedValues(Constants.TwoValues);
            values.Should().BeEquivalentTo(new[] { "one", "two" });

            var headers = HttpContext.Response.Headers;
            headers.Add(Constants.TwoValues, new[] { "one" });

            return message;
        }

        [HttpGet("addOverride")]
        public string AddOverrideTest(string message)
        {
            var values = RequestHeaders.GetCommaSeparatedValues(Constants.OneValue);
            values.Should().BeEquivalentTo(new[] { "one" });

            values = RequestHeaders.GetCommaSeparatedValues(Constants.TwoValues);
            values.Should().BeEquivalentTo(new[] { "one", "two" });

            values = RequestHeaders.GetCommaSeparatedValues(Constants.Expression);
            values.Should().BeEquivalentTo(new[] { Environment.MachineName + "/gwcore", "m:GET" });

            return message;
        }

        [HttpGet("updateOverride")]
        public string UpdateOverrideTest(string message)
        {
            var values = RequestHeaders.GetCommaSeparatedValues(Constants.OneValue);
            values.Should().BeEquivalentTo(new[] { "one-updated" });

            values = RequestHeaders.GetCommaSeparatedValues(Constants.TwoValues);
            values.Should().BeEquivalentTo(new[] { "one-updated", "two-updated" });

            values = RequestHeaders.GetCommaSeparatedValues(Constants.Expression);
            values.Should().BeEquivalentTo(new[] { Environment.MachineName + "/gwcore", "m:GET" });

            var headers = HttpContext.Response.Headers;
            headers.Add(Constants.OneValue, "one");
            headers.Add(Constants.TwoValues, new[] { "one", "two" });
            headers.Add(Constants.Expression, new[] { "one", "two" });

            return message;
        }

        [HttpGet("discard")]
        public string DiscardTest(string message)
        {
            var headers = RequestHeaders;
            headers.ContainsKey(Constants.OneValue).Should().BeFalse();
            headers.ContainsKey(Constants.TwoValues).Should().BeFalse();
            headers.ContainsKey(Constants.ThreeValues).Should().BeTrue();
            headers.ContainsKey(Constants.Underscore).Should().BeFalse();
            headers.ContainsKey(Constants.Expression).Should().BeFalse();

            headers = HttpContext.Response.Headers;
            headers.Add(Constants.OneValue, "one");
            headers.Add(Constants.TwoValues, new[] { "one", "two" });
            headers.Add(Constants.ThreeValues, new[] { "one", "two", "three" });
            headers.Add(Constants.Underscore, "one");
            headers.Add(Constants.Expression, string.Empty);
            return message;
        }

        [HttpGet("discardInbound")]
        public string DiscardInbondTest(string message)
        {
            var headers = RequestHeaders;
            headers.ContainsKey(Constants.OneValue).Should().BeFalse();
            headers.ContainsKey(Constants.TwoValues).Should().BeFalse();
            headers.ContainsKey(Constants.ThreeValues).Should().BeFalse();

            headers = HttpContext.Response.Headers;
            headers.Add(Constants.OneValue, "one");
            headers.Add(Constants.TwoValues, new[] { "one", "two" });
            headers.Add(Constants.ThreeValues, new[] { "one", "two", "three" });
            return message;
        }

        private static string RemovePortFromHostInForwarded(string forwarded)
        {
            var endIndex = forwarded.LastIndexOf(';');
            var startIndex = forwarded.LastIndexOf(':', endIndex);
            return forwarded.Substring(0, startIndex) + forwarded[endIndex..];
        }
    }
}
