using Microsoft.AspNetCore.Mvc;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [ApiController]
    [Route("[controller]")]
    public class HttpMethodController : ControllerBase
    {
        [HttpGet("200")]
        [HttpDelete("200")]
        [HttpHead("200")]
        [HttpOptions("200")]
        public string Echo200(string message)
            => message;

        [HttpGet("500")]
        [HttpDelete("500")]
        [HttpHead("500")]
        [HttpOptions("500")]
        public IActionResult Echo500(string message)
            => StatusCode(500, message);

        [HttpPost("post200")]
        public string Post200([FromBody] string message)
            => message;

        [HttpPost("post500")]
        public IActionResult Post500([FromBody] string message)
            => StatusCode(500, message);
    }
}
