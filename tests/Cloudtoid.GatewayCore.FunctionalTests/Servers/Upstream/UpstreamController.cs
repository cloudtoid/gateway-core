namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using System.Linq;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Net.Http.Headers;

    [ApiController]
    [Route("[controller]")]
    public class UpstreamController : ControllerBase
    {
        [HttpGet("echo")]
        public string Echo(string message) => message;

        [HttpGet("trace")]
        public string TraceTest(string message)
        {
            HttpContext.Request.Headers.TryGetValue(Constants.CorrelationId, out var values).Should().BeTrue();
            values.Should().HaveCount(1);

            HttpContext.Request.Headers.TryGetValue(Constants.CallId, out values).Should().BeTrue();
            values.Should().HaveCount(1);

            return message;
        }

        [HttpGet("noTrace")]
        public string NoTraceTest(string message)
        {
            HttpContext.Request.Headers.ContainsKey(Constants.CorrelationId).Should().BeFalse();
            HttpContext.Request.Headers.ContainsKey(Constants.CallId).Should().BeFalse();
            return message;
        }

        [HttpGet("customCorrelationId")]
        public string CustomCorrelationIdTest(string message)
        {
            HttpContext.Request.Headers.TryGetValue("x-cor-custom", out var values).Should().BeTrue();
            values.Should().HaveCount(1);
            return message;
        }

        [HttpGet("originalCorrelationId")]
        public string OriginalCorrelationIdTest(string message)
        {
            HttpContext.Request.Headers.TryGetValue(Constants.CorrelationId, out var values).Should().BeTrue();
            values.Should().BeEquivalentTo(new[] { "corr-id-1" });

            return message;
        }

        [HttpGet("callId")]
        public string CallIdTest(string message)
        {
            HttpContext.Request.Headers.TryGetValue(Constants.CallId, out var values).Should().BeTrue();
            var callId = values.Single();
            callId.Should().NotBe("call-id-1");

            HttpContext.Response.Headers.Add("x-proxy-call-id", callId);
            HttpContext.Response.Headers.Add(Constants.CallId, "call-id-2");
            return message;
        }

        [HttpGet("via")]
        public string ViaTest(string message)
        {
            HttpContext.Request.Headers.TryGetValue(HeaderNames.Via, out var values).Should().BeTrue();
            values.Should().BeEquivalentTo(new[] { "1.1 gwcore" });
            return message;
        }

        [HttpGet("noVia")]
        public string NoViaTest(string message)
        {
            HttpContext.Request.Headers.ContainsKey(HeaderNames.Via).Should().BeFalse();
            return message;
        }

        [HttpGet("viaCustomProxyName")]
        public string ViaCustomProxyTest(string message)
        {
            HttpContext.Request.Headers.TryGetValue(HeaderNames.Via, out var values).Should().BeTrue();
            values.Should().BeEquivalentTo(new[] { "1.1 custom-proxy" });
            return message;
        }

        [HttpGet("viaTwoProxies")]
        public string ViaTwoProxiesTest(string message)
        {
            var values = HttpContext.Request.Headers.GetCommaSeparatedValues(HeaderNames.Via);
            values.Should().BeEquivalentTo(new[] { "2.0 first-leg", "1.1 gwcore" });

            HttpContext.Response.Headers.Add(HeaderNames.Via, "2.0 first-leg");
            return message;
        }
    }
}
