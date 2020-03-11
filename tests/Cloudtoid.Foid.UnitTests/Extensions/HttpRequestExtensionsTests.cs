namespace Cloudtoid.Foid.UnitTests
{
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpRequestExtensionsTests
    {
        [TestMethod]
        public void GetHttpVersion_AllHttpVersions_CorrectResults()
        {
            var context = new DefaultHttpContext();
            var request = context.Request;

            request.Protocol = "HTTP/1.0";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http10);

            request.Protocol = "HTTP/1.1";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http11);

            request.Protocol = "HTTP/2.0";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http20);

            request.Protocol = "HTTP/3.0";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http30);

            request.Protocol = "HTTP/4.0";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http40);

            request.Protocol = "HTTP/1";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http10);

            request.Protocol = "HTTP/2";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http20);

            request.Protocol = "HTTP/3";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http30);

            request.Protocol = "HTTP/4";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http40);

            request.Protocol = "/1";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http10);

            request.Protocol = "/1.1";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http11);

            request.Protocol = "/2";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http20);

            request.Protocol = "/3";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http30);

            request.Protocol = "/4";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http40);

            request.Protocol = "default";
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http11);

            request.Protocol = null;
            request.GetHttpVersion().Should().Be(HttpProtocolVersion.Http11);
        }
    }
}
