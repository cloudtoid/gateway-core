namespace Cloudtoid.GatewayCore.FunctionalTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class FunctionalTests
    {
        private readonly TestExecutor executor = new TestExecutor();

        [TestMethod("Should reach upstream.")]
        public async Task EchoTestAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "echo?message=test");
            await executor.ExecuteAsync(
                request,
                async response =>
                {
                    response.IsSuccessStatusCode.Should().BeTrue();
                    var result = await response.Content.ReadAsStringAsync();
                    result.Should().Be("test");

                    var headers = response.Headers;
                    headers.GetValues(HeaderNames.Via).Should().BeEquivalentTo(new[] { "1.1 gwcore" });

                    var contentHeaders = response.Content.Headers;
                    contentHeaders.ContentType.MediaType.Should().Be("text/plain");
                    contentHeaders.ContentType.CharSet.Should().Be("utf-8");
                    contentHeaders.ContentLength.Should().Be(4);
                });
        }
    }
}
