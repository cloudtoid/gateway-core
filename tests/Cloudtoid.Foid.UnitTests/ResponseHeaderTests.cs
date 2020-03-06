namespace Cloudtoid.Foid.UnitTests
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Proxy;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    public class ResponseHeaderTests
    {
        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderWithUnderscore_HeaderRemovedAsync()
        {
            var provider = new ResponseHeaderValuesProvider();
            var setter = new ResponseHeaderSetter(provider, Substitute.For<ILogger<ResponseHeaderSetter>>());

            var message = new HttpResponseMessage();
            message.Headers.Add("X-Good-Header", "some-value");
            message.Headers.Add("X_Bad_Header", "some-value");

            var context = new DefaultHttpContext();
            await setter.SetHeadersAsync(context, message);

            context.Response.Headers.ContainsKey("X-Good-Header").Should().BeTrue();
            context.Response.Headers.ContainsKey("X_Bad_Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderWithEmptyValue_HeaderRemovedAsync()
        {
            var provider = new ResponseHeaderValuesProvider();
            var setter = new ResponseHeaderSetter(provider, Substitute.For<ILogger<ResponseHeaderSetter>>());

            var message = new HttpResponseMessage();
            message.Headers.Add("X-Empty-Header", string.Empty);

            var context = new DefaultHttpContext();
            await setter.SetHeadersAsync(context, message);

            context.Response.Headers.ContainsKey("X-Empty-Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderValuesProviderDropsHeaders_HeadersAreNotIncludedAsync()
        {
            var provider = Substitute.For<IResponseHeaderValuesProvider>();
            provider
                .TryGetHeaderValues(
                    Arg.Any<HttpContext>(),
                    Arg.Is("X-Keep-Header"),
                    Arg.Any<IList<string>>(),
                    out Arg.Any<IList<string>>())
                .Returns(x =>
                {
                    x[3] = new[] { "keep-value" };
                    return true;
                });

            provider
                .TryGetHeaderValues(
                    Arg.Any<HttpContext>(),
                    Arg.Is("X-Drop-Header"),
                    Arg.Any<IList<string>>(),
                    out Arg.Any<IList<string>>())
                .Returns(false);

            var setter = new ResponseHeaderSetter(provider, Substitute.For<ILogger<ResponseHeaderSetter>>());

            var message = new HttpResponseMessage();
            message.Headers.Add("X-Keep-Header", "keep-value");
            message.Headers.Add("X-Drop-Header", "drop-value");

            var context = new DefaultHttpContext();
            await setter.SetHeadersAsync(context, message);

            context.Response.Headers.ContainsKey("X-Keep-Header").Should().BeTrue();
            context.Response.Headers["X-Keep-Header"].Should().BeEquivalentTo(new[] { "keep-value" });
            context.Response.Headers.ContainsKey("X-Drop-Header").Should().BeFalse();
        }
    }
}