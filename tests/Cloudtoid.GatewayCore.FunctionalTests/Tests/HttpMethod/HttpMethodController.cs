using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [ApiController]
    [Route("[controller]")]
    public class HttpMethodController : ControllerBase
    {
        private IHeaderDictionary RequestHeaders
            => HttpContext.Request.Headers;

        private bool IsGatewayCore
            => RequestHeaders.TryGetValue(HeaderNames.Via, out var values)
            && values.ToString().ContainsOrdinalIgnoreCase(GatewayCore.Constants.ServerName);

        [HttpGet("echo")]
        public string Echo(string message)
            => message;
    }
}
