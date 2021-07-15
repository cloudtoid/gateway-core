using Microsoft.AspNetCore.Mvc;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [ApiController]
    [Route("[controller]")]
    public class HttpMethodController : ControllerBase
    {
        [HttpGet("get200")]
        public string Get200(string message)
            => message;

        [HttpGet("get500")]
        public IActionResult Get500(string message)
            => StatusCode(500, message);

        [HttpPost("post200")]
        public string Post200([FromBody] string message)
            => message;

        [HttpPost("post500")]
        public IActionResult Post500([FromBody] string message)
            => StatusCode(500, message);
    }
}
