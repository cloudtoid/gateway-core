namespace Cloudtoid.Foid.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Proxy;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Net.Http.Headers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using static Cloudtoid.Foid.FoidOptions.ProxyOptions.UpstreamOptions.RequestOptions.HeadersOptions;

    [TestClass]
    public class RequestHeaderTests
    {
        [TestMethod]
        public void GetHostHeaderValue_WhenHostNameIncludesPortNumber_PortNumberIsRemoved()
        {
            var provider = GetProvider();
            provider.TryGetHeaderValues(new DefaultHttpContext(), HeaderNames.Host, new[] { "host:123", "random-value" }, out var values).Should().BeTrue();
            values.Should().HaveCount(1);
            values[0].Should().Be("host");
        }

        [TestMethod]
        public void GetHostHeaderValue_WhenHostHeaderNotSpecified_HostHeaderIsMachineName()
        {
            var provider = GetProvider();
            provider.TryGetHeaderValues(new DefaultHttpContext(), HeaderNames.Host, Array.Empty<string>(), out var values).Should().BeTrue();
            values.Should().HaveCount(1);
            values[0].Should().Be(Environment.MachineName);
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNoHostHeader_HostHeaderIsAddedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains(HeaderNames.Host).Should().BeTrue();
            message.Headers.GetValues(HeaderNames.Host).SingleOrDefault().Should().Be(Environment.MachineName);
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderWithUnderscore_HeaderRemovedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Good-Header", "some-value");
            context.Request.Headers.Add("X_Bad_Header", "some-value");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains("X-Good-Header").Should().BeTrue();
            message.Headers.Contains("X_Bad_Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHeaderWithEmptyValue_HeaderRemovedAsync()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Add("X-Empty-Header", string.Empty);

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.Contains("X-Empty-Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenCustomHeaderValuesProviderDropsHeaders_HeadersAreNotIncludedAsync()
        {
            // Arrange
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

            // Act
            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message);

            // Assert
            message.Headers.Contains("X-Keep-Header").Should().BeTrue();
            message.Headers.GetValues("X-Keep-Header").Should().BeEquivalentTo(new[] { "keep-value" });
            message.Headers.Contains("X-Drop-Header").Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIncludeExternalAddress_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-foid-external-address";

            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IncludeExternalAddress = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "3.2.1.0");
            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("0.1.2.3");
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIncludeExternalAddress_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-foid-external-address";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IncludeExternalAddress = false;

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIgnoreAllDownstreamHeaders_NoDownstreamHeaderIsIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-custom-test";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "abc");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIgnoreAllDownstreamHeadersAndRequestId_NewRequestIdIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-request-id";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders = true;
            options.Proxy.Upstream.Request.Headers.IgnoreRequestId = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "abc");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers
                .GetValues(HeaderName)
                .SingleOrDefault()
                .Should()
                .Be(GuidProvider.Value.ToStringInvariant("N"));
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIgnoreAllDownstreamHeadersAndRequestId_ExistingRequestIdIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-request-id";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders = false;
            options.Proxy.Upstream.Request.Headers.IgnoreRequestId = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "abc");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers
                .GetValues(HeaderName)
                .SingleOrDefault()
                .Should()
                .Be("abc");
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIgnoreAllDownstreamHeaders_DownstreamHeadersAreIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-custom-test";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreAllDownstreamHeaders = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, new[] { "abc", "efg" });

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).Should().BeEquivalentTo(new[] { "abc", "efg" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIgnoreClientAddress_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-forwarded-for";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreClientAddress = true;

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIgnoreClientAddress_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-forwarded-for";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreClientAddress = false;

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("0.1.2.3");
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenHasClientAddress_NewValueIsAppenededAsync()
        {
            // Arrange
            const string HeaderName = "x-forwarded-for";

            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreClientAddress = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "3.2.1.0");
            context.Connection.RemoteIpAddress = new IPAddress(new byte[] { 0, 1, 2, 3 });

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).Should().BeEquivalentTo(new[] { "3.2.1.0", "0.1.2.3" });
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIgnoreClientProtocol_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-forwarded-proto";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreClientProtocol = true;

            var context = new DefaultHttpContext();
            context.Request.Scheme = "HTTPS";

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIgnoreClientProtocol_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-forwarded-proto";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreClientProtocol = false;

            var context = new DefaultHttpContext();
            context.Request.Scheme = "HTTPS";

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("HTTPS");
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIgnoreRequestId_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-request-id";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreRequestId = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "abc");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIgnoreRequestId_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-request-id";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreRequestId = false;

            var context = new DefaultHttpContext();

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers
                .GetValues(HeaderName)
                .SingleOrDefault()
                .Should()
                .Be(GuidProvider.Value.ToStringInvariant("N"));
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIgnoreRequestIdWithExistingId_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-request-id";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreRequestId = false;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "abc");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("abc");
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenIgnoreCallId_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-call-id";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreCallId = true;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "abc");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenNotIgnoreCallId_HeaderIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-call-id";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.IgnoreCallId = false;

            var context = new DefaultHttpContext();

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers
                .GetValues(HeaderName)
                .SingleOrDefault()
                .Should()
                .Be(GuidProvider.Value.ToStringInvariant("N"));
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenProxyNameNull_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-foid-proxy-name";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.ProxyName = null;

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "abc");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.Contains(HeaderName).Should().BeFalse();
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenProxyNameSpecified_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-foid-proxy-name";
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.ProxyName = "edge";

            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "abc");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("edge");
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenProxyNameDefault_HeaderNotIncludedAsync()
        {
            // Arrange
            const string HeaderName = "x-foid-proxy-name";
            var context = new DefaultHttpContext();
            context.Request.Headers.Add(HeaderName, "abc");

            // Act
            var message = await SetHeadersAsync(context);

            // Assert
            message.Headers.GetValues(HeaderName).SingleOrDefault().Should().Be("foid");
        }

        [TestMethod]
        public async Task SetHeadersAsync_WhenExtraHeaders_HeadersIncludedAsync()
        {
            // Arrange
            var options = new FoidOptions();
            options.Proxy.Upstream.Request.Headers.ExtraHeaders = new[]
            {
                new ExtraHeader
                {
                    Key = "x-xtra-1",
                    Values = new[] { "value1_1", "value1_2" }
                },
                new ExtraHeader
                {
                    Key = "x-xtra-2",
                    Values = new[] { "value2_1", "value2_2" }
                },
            };

            var context = new DefaultHttpContext();
            context.Request.Headers.Add("x-xtra-2", "value2_0");

            // Act
            var message = await SetHeadersAsync(context, options);

            // Assert
            message.Headers.GetValues("x-xtra-1").Should().BeEquivalentTo(new[] { "value1_1", "value1_2" });
            message.Headers.GetValues("x-xtra-2").Should().BeEquivalentTo(new[] { "value2_0", "value2_1", "value2_2" });
        }

        private static async Task<HttpRequestMessage> SetHeadersAsync(HttpContext context, FoidOptions? options = null)
        {
            var provider = GetProvider(options);
            var setter = new RequestHeaderSetter(provider, GuidProvider.Instance, Substitute.For<ILogger<RequestHeaderSetter>>());
            var message = new HttpRequestMessage();
            await setter.SetHeadersAsync(context, message);
            return message;
        }

        private static RequestHeaderValuesProvider GetProvider(FoidOptions? options = null)
        {
            var mo = Substitute.For<IOptionsMonitor<FoidOptions>>();
            mo.CurrentValue.Returns(options ?? new FoidOptions());
            return new RequestHeaderValuesProvider(mo);
        }
    }
}