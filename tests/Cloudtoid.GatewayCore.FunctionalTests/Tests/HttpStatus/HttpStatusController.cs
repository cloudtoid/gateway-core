using Microsoft.AspNetCore.Mvc;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [ApiController]
    [Route("[controller]")]
    public class HttpStatusController : ControllerBase
    {
        [HttpGet("echo")]
        public string? Echo(string? message) => message;
    }
}
