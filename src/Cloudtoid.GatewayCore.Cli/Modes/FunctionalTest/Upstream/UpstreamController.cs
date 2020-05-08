namespace Cloudtoid.GatewayCore.Cli.Modes.FunctionalTest.Upstream
{
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]")]
    public class UpstreamController : ControllerBase
    {
        [HttpGet("echo")]
        public string Echo(string message) => message;

        [HttpGet("add")]
        public int Add(int a, int b) => a + b;
    }
}
