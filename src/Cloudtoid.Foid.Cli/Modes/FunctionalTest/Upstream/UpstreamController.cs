namespace Cloudtoid.Foid.Cli.Modes.FunctionalTest.Upstream
{
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]")]
    public class UpstreamController : ControllerBase
    {
        [HttpGet("add")]
        public int Add(int a, int b)
        {
            return a + b;
        }
    }
}
