using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [ApiController]
    [Route("[controller]")]
    public class HttpStatusController : ControllerBase
    {
        private IHeaderDictionary RequestHeaders
            => HttpContext.Request.Headers;

        private bool IsGatewayCore
            => RequestHeaders.TryGetValue(HeaderNames.Via, out var values)
            && values.ToString().ContainsOrdinalIgnoreCase(GatewayCore.Constants.ServerName);

        [HttpGet("basic")]
        public string? BasicTest(string? message)
        {
            RequestHeaders.TryGetValue(HeaderNames.TraceParent, out var traceparents).Should().BeTrue();
            traceparents.Should().ContainSingle();

            RequestHeaders.TryGetValue(HeaderNames.TraceState, out var tracestate).Should().BeTrue();
            tracestate.Should().ContainSingle();

            if (IsGatewayCore)
            {
                RequestHeaders.TryGetValue(HeaderNames.Via, out var values).Should().BeTrue();
                values.Should().ContainSingle().And.ContainMatch("?.? " + GatewayCore.Constants.ServerName);
            }

            return message;
        }
    }
}
