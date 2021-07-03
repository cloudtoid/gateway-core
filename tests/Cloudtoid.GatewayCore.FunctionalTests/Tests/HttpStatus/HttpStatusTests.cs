using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Cloudtoid.GatewayCore.FunctionalTests.TestExecutor;
using Method = System.Net.Http.HttpMethod;

namespace Cloudtoid.GatewayCore.FunctionalTests
{
    [TestClass]
    public sealed class HttpStatusTests
    {
        [TestMethod("Basic HTTP Status plumbing test")]
        public async Task BasicPlumbingTestAsync()
        {
            await Task.Delay(10);
        }

        [TestMethod("Should not return success when route doesn't exist.")]
        public async Task NoRouteTestAsync()
        {
            var request = new HttpRequestMessage(Method.Get, "noRoute?message=test");
            await ExecuteAsync(
                "DefaultTestOptions.json",
                request,
                response =>
                {
                    response.IsSuccessStatusCode.Should().BeFalse();
                    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
                    return Task.CompletedTask;
                });
        }
    }
}
