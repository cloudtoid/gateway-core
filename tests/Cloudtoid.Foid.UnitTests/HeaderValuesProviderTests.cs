namespace Cloudtoid.Foid.UnitTests
{
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Proxy;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    public class HeaderValuesProviderTests
    {
        [TestMethod]
        public async Task SetHeadersAsync_WhenNoHostHeader_HostHeaderIsAddedAsync()
        {
            var provider = new RequestHeaderValuesProvider();
            var setter = new RequestHeaderSetter(provider, Substitute.For<ILogger<RequestHeaderSetter>>());

            var context = new DefaultHttpContext();
            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message);

            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
            message.Headers.GetValues(HeaderNames.Host).FirstOrDefault().Should().Be(provider.GetDefaultHostHeaderValue(context));
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderWithUnderscore_HeaderRemovedAsync()
        {
            var provider = new RequestHeaderValuesProvider();
            var setter = new RequestHeaderSetter(provider, Substitute.For<ILogger<RequestHeaderSetter>>());

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Good-Header", "some-value");
            context.Request.Headers.Add("X_Bad_Header", "some-value");

            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message);

            message.Headers.Should().NotContain("X_Bad_Header");
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderWithEmptyValue_HeaderRemovedAsync()
        {
            var provider = new RequestHeaderValuesProvider();
            var setter = new RequestHeaderSetter(provider, Substitute.For<ILogger<RequestHeaderSetter>>());

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Empty-Header", string.Empty);

            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message);

            message.Headers.Should().NotContain("X-Empty-Header");
        }
    }
}