using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [TestClass]
    public sealed class HttpStatusTests
    {
        [TestMethod("Basic HTTP Status plumbing test")]
        public async Task BasicPlumbingTestAsync()
        {
            await using var pipeline = new Pipeline(
                "Tests/HttpStatus/GatewayCoreOptions/default.json",
                "Tests/HttpStatus/NginxConfigs/default.conf");

            await pipeline.StartAsync();
        }
    }
}
