namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class Tests
    {
        private readonly TestExecutor executor = new TestExecutor();

        [TestMethod("Simple end to end test.")]
        public async Task EchoTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:86/api/echo?message=test");
            await executor.ExecuteAsync(request);
        }
    }
}
