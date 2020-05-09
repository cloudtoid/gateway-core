namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using FluentAssertions;
    using Microsoft.AspNetCore.Mvc;

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
    }
}
