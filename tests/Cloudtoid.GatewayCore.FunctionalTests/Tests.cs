namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class Tests
    {
        [TestMethod("Simple end to end test.")]
        public async Task EchoTestAsync()
        {
            using (var executor = new TestExecutor())
            {
                await executor.ExecuteAsync();
            }
        }
    }
}
