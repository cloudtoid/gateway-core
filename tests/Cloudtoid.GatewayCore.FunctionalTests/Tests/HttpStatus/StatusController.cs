using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        private IHeaderDictionary RequestHeaders
            => HttpContext.Request.Headers;

        private bool IsGatewayCore
            => RequestHeaders.TryGetValue(HeaderNames.Via, out var values)
            && values.ToString().ContainsOrdinalIgnoreCase(GatewayCore.Constants.ServerName);

        [HttpGet("echo")]
        public string Echo(string message)
            => message;

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

        [HttpGet("redirect")]
        public IActionResult RedirectTest(string message)
            => Redirect($"~/api/echo?message={message}"); // HTTP Status Code = 302

        [HttpGet("redirectPermanent")]
        public IActionResult RedirectPermanentTest(string message)
            => RedirectPermanent($"~/api/echo?message={message}"); // HTTP Status Code = 301

        [HttpGet("redirectPreserveMethod")]
        public IActionResult RedirectPreserveMethodTest(string message)
            => RedirectPreserveMethod($"~/api/echo?message={message}"); // HTTP Status Code = 307

        [HttpGet("redirectPermanentPreserveMethod")]
        public IActionResult RedirectPermanentPreserveMethodTest(string message)
            => RedirectPermanentPreserveMethod($"~/api/echo?message={message}"); // HTTP Status Code = 308

        [HttpGet("redirect300")]
        public IActionResult Redirect300Test(string message)
            => StatusCode(300, $"~/api/echo?message={message}");

        [HttpGet("notModified")]
        public IActionResult NotModifiedTest()
            => StatusCode(304);
    }
}
