using Microsoft.AspNetCore.Mvc;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [ApiController]
    [Route("[controller]")]
    public class HttpMethodController : ControllerBase
    {
        // TODO:
        // 1- Get with body - not common but it should just work. (both on request and response). We might want to make this configurable.
        // 2- Same applies to a successful PUT request

        [HttpGet("200")]
        [HttpDelete("200")]
        [HttpHead("200")]
        public string Echo200(string message)
            => message;

        [HttpGet("500")]
        [HttpDelete("500")]
        [HttpHead("500")]
        [HttpOptions("500")]
        public IActionResult Echo500(string message)
            => StatusCode(500, message);

        [HttpPost("b200")]
        [HttpPut("b200")]
        [HttpOptions("b200")]
        public string Post200([FromBody] string message)
            => message;

        [HttpPost("b500")]
        [HttpPut("b500")]
        [HttpOptions("b500")]
        public IActionResult Post500([FromBody] string message)
            => StatusCode(500, message);
    }
}
