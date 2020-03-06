namespace Cloudtoid.Foid.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Proxy;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;

    [TestClass]
    public class RequestHeaderTests
    {
        [TestMethod]
        public void GetHostHeaderValue_WhenHostNameIncludesPortNumber_PortNumberIsRemoved()
        {
            var provider = new RequestHeaderValuesProvider(new ProxyConfig(Substitute.For<IConfiguration>()));
            provider.TryGetHeaderValues(new DefaultHttpContext(), HeaderNames.Host, new[] { "host:123", "random-value" }, out var values).Should().BeTrue();
            values.Should().HaveCount(1);
            values[0].Should().Be("host");
        }

        [TestMethod]
        public void GetHostHeaderValue_WhenHostHeaderNotSpecified_HostHeaderIsMachineName()
        {
            var provider = new RequestHeaderValuesProvider(new ProxyConfig(Substitute.For<IConfiguration>()));
            provider.TryGetHeaderValues(new DefaultHttpContext(), HeaderNames.Host, Array.Empty<string>(), out var values).Should().BeTrue();
            values.Should().HaveCount(1);
            values[0].Should().Be(Environment.MachineName);
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNoHostHeader_HostHeaderIsAddedAsync()
        {
            var provider = new RequestHeaderValuesProvider(new ProxyConfig(Substitute.For<IConfiguration>()));
            var setter = new RequestHeaderSetter(provider, GuidProvider.Instance, Substitute.For<ILogger<RequestHeaderSetter>>());

            var context = new DefaultHttpContext();
            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message);

            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
            message.Headers.GetValues(HeaderNames.Host).FirstOrDefault().Should().Be(provider.GetDefaultHostHeaderValue(context));
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderWithUnderscore_HeaderRemovedAsync()
        {
            var provider = new RequestHeaderValuesProvider(new ProxyConfig(Substitute.For<IConfiguration>()));
            var setter = new RequestHeaderSetter(provider, GuidProvider.Instance, Substitute.For<ILogger<RequestHeaderSetter>>());

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Good-Header", "some-value");
            context.Request.Headers.Add("X_Bad_Header", "some-value");

            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message);

            message.Headers.Contains("X-Good-Header").Should().BeTrue();
            message.Headers.Contains("X_Bad_Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderWithEmptyValue_HeaderRemovedAsync()
        {
            var provider = new RequestHeaderValuesProvider(new ProxyConfig(Substitute.For<IConfiguration>()));
            var setter = new RequestHeaderSetter(provider, GuidProvider.Instance, Substitute.For<ILogger<RequestHeaderSetter>>());

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Empty-Header", string.Empty);

            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message);

            message.Headers.Contains("X-Empty-Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderValuesProviderDropsHeaders_HeadersAreNotIncludedAsync()
        {
            var provider = Substitute.For<IRequestHeaderValuesProvider>();
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

            var setter = new RequestHeaderSetter(provider, GuidProvider.Instance, Substitute.For<ILogger<RequestHeaderSetter>>());

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Keep-Header", "keep-value");
            context.Request.Headers.Add("X-Drop-Header", "drop-value");

            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message);

            message.Headers.Contains("X-Keep-Header").Should().BeTrue();
            message.Headers.GetValues("X-Keep-Header").Should().BeEquivalentTo(new[] { "keep-value" });
            message.Headers.Contains("X-Drop-Header").Should().BeFalse();
        }
    }
}